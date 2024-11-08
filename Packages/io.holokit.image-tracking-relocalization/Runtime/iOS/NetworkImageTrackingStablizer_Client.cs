// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

#if UNITY_IOS
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Events;
using Unity.Netcode;
using System;
using HoloKit.iOS;

namespace HoloKit.ImageTrackingRelocalization.iOS
{
    public enum ClientStatus
    {
        None = 0,
        // The client trying to sync its local timestamp to the host
        SyncingTimestamp = 1,
        // The client trying to sync its local coordinate system to the host
        SyncingPose = 2,
        // The client has synced its local timestamp and coordinate system to the host
        Synced = 3,
        // The client has manually confirmed the syncing result
        Checked = 4
    }

    public struct ImagePosePair
    {
        public Vector3 HostImagePosition; // Tracked marker pose in the host's coordinate system
        public Quaternion HostImageRotation;
        public Vector3 ClientImagePosition; // Tracked marker pose in the client's coordinate system
        public Quaternion ClientImageRotation;
    }

    // Rotate the client's coordinate system by theta first, then translate it.
    public struct SyncResult
    {
        public float ThetaInDegree;
        public Vector3 Translate;
    }

    /// <summary>
    /// Partial class separating host and client code; this file contains client-specific code.
    /// </summary>
    public partial class NetworkImageTrackingStablizer : NetworkBehaviour
    {
        public bool IsTrackingMarker => m_IsTrackingMarker;

        private ARTrackedImageManager m_ARTrackedImageManager;

        private bool m_IsTrackingMarker;

        private ClientStatus m_ClientStatus;

        /// <summary>
        /// Stores a sequence of timestamp offset between the local client device to the host device.
        /// </summary>
        private readonly Queue<double> m_TimestampOffsetQueue = new();

        /// <summary>
        /// The final result derived from the timestamp offset calculation.
        /// </summary>
        private double m_ResultTimestampOffset;

        /// <summary>
        /// Stores pairs of the host image poses and client image poses.
        /// </summary>
        private readonly Queue<ImagePosePair> m_ImagePosePairQueue = new();

        /// <summary>
        /// Stores a sequence of sync results.
        /// </summary>
        private readonly Queue<SyncResult> m_SyncResultQueue = new();

        /// <summary>
        /// The timestamp of the lastest ARSession frame.
        /// </summary>
        private double m_LatestARSessionFrameTimestamp;

        [Header("Client Side Calibration Parameters")]
        [Tooltip("The minimum number of elements in the timestamp offset queue to start calculating the standard deviation of the queue.")]
        [SerializeField] private int m_TimestampOffsetQueueStablizationCount = 10;

        [Tooltip("The maximum acceptable standard deviation of the timestamp offset queue.")]
        [SerializeField] private double m_TimestampOffsetQueueStandardDeviationThreshold = 0.01;

        [Tooltip("The minimum number of elements in the image pose pair queue to start calculating the standard deviation of the queue.")]
        [SerializeField] private int m_ClientImagePosePairQueueStablizationCount = 50;

        [Tooltip("A constant whose unit from Cos(Angle) to m meter.")]
        [SerializeField] private float m_OptimizationPenaltyConstant = 10f;

        [Tooltip("The minimum number of elements in the sync result queue to start calculating the standard deviation of the queue.")]
        [SerializeField] private int m_ClientSyncResultQueueStablizationCount = 30;

        [Tooltip("The maximum acceptable standard deivation of the sync result's theta value in degrees.")]
        [SerializeField] private double m_ThetaStandardDeviationThreshold = 0.1;

        [Header("Prefabs")]
        [SerializeField] private GameObject m_AlignmentMarkerPrefab;

        private GameObject m_AlignmentMarker;

        [Header("Client Side Events")]
        public UnityEvent OnTimestampSynced;

        public UnityEvent OnPoseSynced;

        public UnityEvent OnAlignmentMarkerAccepted;

        public UnityEvent OnAlignmentMarkerDenied;

        public UnityEvent OnResyncPose;

        private void Start_Client()
        {
            m_ARTrackedImageManager = FindFirstObjectByType<ARTrackedImageManager>();
            if (m_ARTrackedImageManager == null)
            {
                Debug.LogWarning("[NetworkImageTrackingStablizer] Failed to find ARTrackedImageManager in the scene.");
                return;
            }
            m_ARTrackedImageManager.enabled = false;
        }

        private void OnNetworkSpawn_Client()
        {
            if (!NetworkManager.Singleton.IsHost)
                m_ClientStatus = ClientStatus.SyncingTimestamp;
        }

        private void FixedUpdate()
        {
            if (m_ClientStatus == ClientStatus.SyncingTimestamp)
                OnRequestTimestampServerRpc(m_AppleNativeProvider.GetSystemUptime());
        }

        [ClientRpc]
        private void OnRespondTimestampClientRpc(double hostTimestamp, double oldClientTimestamp, ClientRpcParams _ = default)
        {
            if (m_ClientStatus != ClientStatus.SyncingTimestamp)
                return;

            double currentClientTimestamp = m_AppleNativeProvider.GetSystemUptime();
            double offset = hostTimestamp + (currentClientTimestamp - oldClientTimestamp) / 2 - currentClientTimestamp;
            m_TimestampOffsetQueue.Enqueue(offset);
            // Calculate the queue's standard deviation if the size of the queue is larger than the threshold.
            if (m_TimestampOffsetQueue.Count > m_TimestampOffsetQueueStablizationCount)
            {
                double standardDeviation = CalculateStdDev(m_TimestampOffsetQueue);
                if (standardDeviation < m_TimestampOffsetQueueStandardDeviationThreshold)
                {
                    // Take the average value of all elements in the queue as the final timestamp offset.
                    m_ResultTimestampOffset = m_TimestampOffsetQueue.Average();
                    OnTimestampSyncedInternal();
                }
                m_TimestampOffsetQueue.Dequeue();
            }
        }

        private void OnTimestampSyncedInternal()
        {
            if (m_AlignmentMarker != null)
                Destroy(m_AlignmentMarker);

            m_ImagePosePairQueue.Clear();
            m_SyncResultQueue.Clear();

            OnTimestampSynced?.Invoke();
            StartTrackingMarker();
        }

        public void StartTrackingMarker()
        {
            m_ClientStatus = ClientStatus.SyncingPose;

            HoloKitARKitManager.Instance.ARKitNativeProvider.OnARSessionUpdatedFrame += OnARSessionUpdatedFrame_Client;
            HoloKitARKitManager.Instance.ARKitNativeProvider.InterceptUnityARSessionDelegate();

            foreach (var trackable in m_ARTrackedImageManager.trackables)
                trackable.gameObject.SetActive(true);

            m_ARTrackedImageManager.trackedImagesChanged += OnTrackedImageChanged;
            m_ARTrackedImageManager.enabled = true;
        }

        public void StopTrackingMarker()
        {
            HoloKitARKitManager.Instance.ARKitNativeProvider.OnARSessionUpdatedFrame -= OnARSessionUpdatedFrame_Client;
            HoloKitARKitManager.Instance.ARKitNativeProvider.RestoreUnityARSessionDelegate();

            foreach (var trackable in m_ARTrackedImageManager.trackables)
                trackable.gameObject.SetActive(false);

            m_ARTrackedImageManager.enabled = false;
            m_ARTrackedImageManager.trackedImagesChanged -= OnTrackedImageChanged;
        }

        private void OnTrackedImageChanged(ARTrackedImagesChangedEventArgs args)
        {
            if (args.updated.Count == 1)
            {
                var image = args.updated[0];
                if (image.trackingState == TrackingState.Tracking)
                {
                    OnRequestImagePoseServerRpc(m_LatestARSessionFrameTimestamp + m_ResultTimestampOffset, image.transform.position, image.transform.rotation);
                }
            }
        }

        private void OnARSessionUpdatedFrame_Client(double timestamp, Matrix4x4 matrix)
        {
            m_LatestARSessionFrameTimestamp = timestamp;
        }

        [ClientRpc]
        private void OnRespondImagePoseClientRpc(Vector3 hostImagePosition, Quaternion hostImageRotation, Vector3 clientImagePosition, Quaternion clientImageRotation, ClientRpcParams clientRpcParams = default)
        {
            if (m_ClientStatus != ClientStatus.SyncingPose)
                return;

            m_ImagePosePairQueue.Enqueue(new ImagePosePair()
            {
                HostImagePosition = hostImagePosition,
                HostImageRotation = hostImageRotation,
                ClientImagePosition = clientImagePosition,
                ClientImageRotation = clientImageRotation
            });
            // We wait until the queue reaches the minimal size
            if (m_ImagePosePairQueue.Count > m_ClientImagePosePairQueueStablizationCount)
            {
                var clientSyncResult = CalculateClientSyncResult();
                m_SyncResultQueue.Enqueue(clientSyncResult);
                if (m_SyncResultQueue.Count > m_ClientSyncResultQueueStablizationCount)
                {
                    var thetaStandardDeviation = CalculateStdDev(m_SyncResultQueue.Select(x => (double)x.ThetaInDegree));
                    if (thetaStandardDeviation < m_ThetaStandardDeviationThreshold)
                    {
                        // Client synced successfully
                        OnPoseSyncedInternal();
                        return;
                    }
                    else
                    {
                        _ = m_SyncResultQueue.Dequeue();
                    }
                }
                _ = m_ImagePosePairQueue.Dequeue();
            }
        }

        /// <summary>
        /// Use the least square method to calculate the result.
        /// </summary>
        /// <returns></returns>
        private SyncResult CalculateClientSyncResult()
        {
            var clientImagePositionCenter = new Vector3(
                m_ImagePosePairQueue.Select(o => o.ClientImagePosition.x).Average(),
                m_ImagePosePairQueue.Select(o => o.ClientImagePosition.y).Average(),
                m_ImagePosePairQueue.Select(o => o.ClientImagePosition.z).Average());

            var hostImagePositionCenter = new Vector3(
                m_ImagePosePairQueue.Select(o => o.HostImagePosition.x).Average(),
                m_ImagePosePairQueue.Select(o => o.HostImagePosition.y).Average(),
                m_ImagePosePairQueue.Select(o => o.HostImagePosition.z).Average());

            Vector2 tanThetaAB = m_ImagePosePairQueue.Select(o =>
            {
                var p = o.HostImagePosition - hostImagePositionCenter;
                var q = o.ClientImagePosition - clientImagePositionCenter;
                var r = Matrix4x4.Rotate(o.HostImageRotation).transpose * Matrix4x4.Rotate(o.ClientImageRotation);

                var a = p.x * q.x + p.z * q.z + m_OptimizationPenaltyConstant * (r.m00 + r.m22);
                var b = -p.x * q.z + p.z * q.x + m_OptimizationPenaltyConstant * (-r.m20 + r.m02);
                return new Vector2(a, b);
            }).Aggregate(Vector2.zero, (r, o) => r + o);

            float thetaInDegree = (float)Math.Atan2(tanThetaAB.y, tanThetaAB.x) / Mathf.Deg2Rad;

            Matrix4x4 rotation = Matrix4x4.Rotate(Quaternion.AngleAxis(thetaInDegree, Vector3.up));

            Vector3 translate = -rotation.MultiplyPoint3x4(hostImagePositionCenter - clientImagePositionCenter);

            return new SyncResult()
            {
                ThetaInDegree = thetaInDegree,
                Translate = translate
            };
        }

        /// <summary>
        /// This function is called when the local device successfully synced.
        /// </summary>
        private void OnPoseSyncedInternal()
        {
            m_ClientStatus = ClientStatus.Synced;

            // Reset world origin based on the sync result
            var lastSyncResult = m_SyncResultQueue.Last();
            float theta = lastSyncResult.ThetaInDegree;
            Vector3 translate = lastSyncResult.Translate;
            HoloKitARKitManager.Instance.ARKitNativeProvider.ResetWorldOrigin(translate, Quaternion.AngleAxis(theta, Vector3.up));

            StopTrackingMarker();

            // Spawn the alignment marker to manually validate the result
            SpawnAlignmentMarker();

            OnPoseSynced?.Invoke();
            Debug.Log($"[NetworkImageTrackingStablizer] Coordinate system synced");
        }

        private void SpawnAlignmentMarker()
        {
            // There can only be one alignment marker spawned at the same time
            if (m_AlignmentMarker != null)
                return;

            m_AlignmentMarker = Instantiate(m_AlignmentMarkerPrefab);

            // Find the host player object and set the alignment marker its child so that the alignment marker will follow the host device
            var playerControllers = FindObjectsByType<PlayerPoseSynchronizer>(FindObjectsSortMode.None);
            foreach (var playerController in playerControllers)
            {
                if (playerController.OwnerClientId == NetworkManager.ServerClientId)
                {
                    m_AlignmentMarker.transform.SetParent(playerController.transform);
                    m_AlignmentMarker.transform.localPosition = m_CameraToScreenCenterOffset.Value;
                    m_AlignmentMarker.transform.localRotation = Quaternion.identity;
                }
            }
        }

        public void DenyAlignmentMarker()
        {
            OnAlignmentMarkerDenied?.Invoke();

            OnTimestampSyncedInternal();
        }

        public void AcceptAlignmentMarker()
        {
            OnAlignmentMarkerAccepted?.Invoke();

            if (m_AlignmentMarker != null)
                Destroy(m_AlignmentMarker);
        }

        public void ResyncPose()
        {
            OnResyncPose?.Invoke();

            OnTimestampSyncedInternal();
        }
    }
}
#endif
