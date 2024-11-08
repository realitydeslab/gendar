// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

#if UNITY_IOS
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using HoloKit.iOS;

namespace HoloKit.ImageTrackingRelocalization.iOS
{
    public struct TimedCameraPose
    {
        public double Timestamp;
        public Matrix4x4 PoseMatrix;
    }

    /// <summary>
    /// Partial class separating host and client code; this file contains host-specific code.
    /// </summary>
    public partial class NetworkImageTrackingStablizer : NetworkBehaviour
    {
        public bool IsDisplayingMarker => m_IsDisplayingMarker;

        [Header("Host Side Calibration Parameters")]
        [Tooltip("The maximum timestamp gap between the current time and a stored camera pose. Host does not store poses with timestamp earlier than this threshold.")]
        [SerializeField] private double m_TimedCameraPoseTimestampThreshold = 0.6;

        [Tooltip("The maximum acceptable timestamp deviation. We only process camera poses with timestamp deviation smaller than this threshold.")]
        [SerializeField] private double m_TimestampDeviationThreshold = 0.034;

        /// <summary>
        /// Stores a queue of camera poses with timestamps.
        /// </summary>
        private readonly Queue<TimedCameraPose> m_TimedCameraPoseQueue = new();

        private MarkerRenderer m_MarkerRenderer;

        private bool m_IsDisplayingMarker;

        /// <summary>
        /// The offset from the host's camera to the center of the displayed marker.
        /// </summary>
        private NetworkVariable<Vector3> m_CameraToMarkerOffset = new(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>
        /// The offset from the host's camera to its screen center. This is used by the client to correctly render the alignment marker.
        /// </summary>
        private NetworkVariable<Vector3> m_CameraToScreenCenterOffset = new(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private void Start_Host()
        {
            m_MarkerRenderer = GetComponent<MarkerRenderer>();
        }

        private void OnNetworkSpawn_Host()
        {

        }

        public void StartDisplayingMarker()
        {
#if !UNITY_EDITOR
            HoloKitARKitManager.Instance.ARKitNativeProvider.OnARSessionUpdatedFrame += OnARSessionUpdatedFrame_Host;
            HoloKitARKitManager.Instance.ARKitNativeProvider.InterceptUnityARSessionDelegate();
#endif

            (m_CameraToMarkerOffset.Value, m_CameraToScreenCenterOffset.Value) = m_MarkerRenderer.SpawnMarker();
            m_IsDisplayingMarker = true;
        }

        public void StopDisplayingMarker()
        {
#if !UNITY_EDITOR
            HoloKitARKitManager.Instance.ARKitNativeProvider.OnARSessionUpdatedFrame -= OnARSessionUpdatedFrame_Host;
            HoloKitARKitManager.Instance.ARKitNativeProvider.RestoreUnityARSessionDelegate();
#endif

            m_MarkerRenderer.DestroyMarker();
            m_IsDisplayingMarker = false;
        }

        private void OnARSessionUpdatedFrame_Host(double timestamp, Matrix4x4 matrix)
        {
            TimedCameraPose timedCameraPose = new()
            {
                Timestamp = timestamp,
                PoseMatrix = matrix
            };

            m_TimedCameraPoseQueue.Enqueue(timedCameraPose);
            double currentTime = m_AppleNativeProvider.GetSystemUptime();
            // Get rid of early poses
            while (m_TimedCameraPoseQueue.Count > 0 && currentTime - m_TimedCameraPoseQueue.Peek().Timestamp > m_TimedCameraPoseTimestampThreshold)
            {
                _ = m_TimedCameraPoseQueue.Dequeue();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void OnRequestTimestampServerRpc(double clientTimestamp, ServerRpcParams serverRpcParams = default)
        {
            // Get the current host timestamp
            double hostTimestamp = m_AppleNativeProvider.GetSystemUptime();
            ClientRpcParams clientRpcParams = new()
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }
                }
            };
            OnRespondTimestampClientRpc(hostTimestamp, clientTimestamp, clientRpcParams);
        }

        [ServerRpc(RequireOwnership = false)]
        private void OnRequestImagePoseServerRpc(double requestedTimestamp, Vector3 clientImagePosition, Quaternion clientImageRotation, ServerRpcParams serverRpcParams = default)
        {
            // We must have at least one camera pose in the queue
            if (m_TimedCameraPoseQueue.Count == 0)
                return;

            double minTimestampDeviation = 99;
            TimedCameraPose nearestTimedCameraPose = new();
            foreach (var timedCameraPose in m_TimedCameraPoseQueue)
            {
                double timestampDeviation = Math.Abs(timedCameraPose.Timestamp - requestedTimestamp);
                if (timestampDeviation < minTimestampDeviation)
                {
                    minTimestampDeviation = timestampDeviation;
                    nearestTimedCameraPose = timedCameraPose;
                }
            }

            // We don't accept camera pose with timestamp not close enough to the requested timestamp
            if (minTimestampDeviation > m_TimestampDeviationThreshold)
                return;

            // Calculate the marker image position with offset
            Vector3 hostImagePosition = nearestTimedCameraPose.PoseMatrix.GetPosition() + nearestTimedCameraPose.PoseMatrix.rotation * m_CameraToMarkerOffset.Value;
            // Calculate the marker image rotation with offset
            Quaternion hostImageRotation = nearestTimedCameraPose.PoseMatrix.rotation * Quaternion.Euler(-90f, 0f, 0f);

            ClientRpcParams clientRpcParams = new()
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }
                }
            };
            OnRespondImagePoseClientRpc(hostImagePosition, hostImageRotation, clientImagePosition, clientImageRotation, clientRpcParams);
        }
    }
}
#endif
