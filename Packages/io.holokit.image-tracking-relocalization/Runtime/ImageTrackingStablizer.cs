// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT
#if UNITY_IOS
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace HoloKit.ImageTrackingRelocalization
{
    public struct TrackedImagePoseData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public double Timestamp;

        public TrackedImagePoseData(Vector3 position, Quaternion rotation, double timestamp)
        {
            Position = position;
            Rotation = rotation;
            Timestamp = timestamp;
        }
    }

    public class ImageTrackingStablizer : MonoBehaviour
    {
        public bool IsRelocalizing
        {
            get => m_IsRelocalizing;
            set
            {
                if (value)
                    StartRelocalization();
                else
                    StopRelocalization();
            }
        }

        public float Progress
        {
            get => (float)m_TrackedImagePoses.Count / (float)m_DesiredNumOfSamples;
        }

        [Tooltip("The maximum timestamp gap between two consecutive tracked image poses.")]
        [SerializeField] private double m_MaxTimestampGap = 1.5f;

        [Tooltip("The desired number of tracked image poses needed to calculate the final result.")]
        [SerializeField] private int m_DesiredNumOfSamples = 50;

        [Tooltip("The maximum acceptable standard deviation for tracked image pose position.")]
        [SerializeField] private float m_MaxPositionStdDev = 0.02f;

        [Tooltip("The maxinum acceptable standard deviation for tracked image pose rotation.")]
        [SerializeField] private float m_MaxRotationStdDev = 36f;

        private ARTrackedImageManager m_ARTrackedImageManager;

        private bool m_IsRelocalizing = false;

        private Queue<TrackedImagePoseData> m_TrackedImagePoses;

        public UnityEvent<Vector3, Quaternion> OnTrackedImagePoseStablized;

        private void Start()
        {
            m_ARTrackedImageManager = FindFirstObjectByType<ARTrackedImageManager>();
            if (m_ARTrackedImageManager == null)
            {
                Debug.LogError("[ImageTrackingRelocalizationManager] Failed to find ARTrackedImageManager in the scene.");
            }
        }

        private void StartRelocalization()
        {
            m_IsRelocalizing = true;
            m_TrackedImagePoses = new();
            m_ARTrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;

        }

        private void StopRelocalization()
        {
            m_ARTrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
            m_TrackedImagePoses = null;
            m_IsRelocalizing = false;
        }

        private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
        {
            if (args.updated.Count == 1)
            {
                var image = args.updated[0];
                if (image.trackingState == TrackingState.Tracking)
                {
                    TrackedImagePoseData poseData = new(image.transform.position, image.transform.rotation, Time.timeAsDouble);
                    m_TrackedImagePoses.Enqueue(poseData);

                    CleanUpOldPoses();

                    if (m_TrackedImagePoses.Count >= m_DesiredNumOfSamples)
                    {
                        CalculateStableTrackedImagePose();
                    }
                }
            }
        }

        private void CleanUpOldPoses()
        {
            while (m_TrackedImagePoses.Count > 0 && (Time.timeAsDouble - m_TrackedImagePoses.Peek().Timestamp) > m_MaxTimestampGap)
            {
                m_TrackedImagePoses.Dequeue();
            }
        }

        private (float, float) CalculateStandardDeviations()
        {
            // Calculate mean position and rotation
            Vector3 meanPosition = Vector3.zero;
            Quaternion meanRotation = CalculateMeanRotation();
            foreach (var pose in m_TrackedImagePoses)
            {
                meanPosition += pose.Position;
            }
            meanPosition /= m_TrackedImagePoses.Count;

            float positionStdDev = 0f;
            float rotationStdDev = 0f;

            foreach (var pose in m_TrackedImagePoses)
            {
                positionStdDev += (pose.Position - meanPosition).sqrMagnitude;

                // Calculate angular difference in rotations
                float angleDiff = Quaternion.Angle(meanRotation, pose.Rotation);
                rotationStdDev += angleDiff * angleDiff;
            }

            positionStdDev = Mathf.Sqrt(positionStdDev / m_TrackedImagePoses.Count);
            rotationStdDev = Mathf.Sqrt(rotationStdDev / m_TrackedImagePoses.Count);

            return (positionStdDev, rotationStdDev);
        }

        private Quaternion CalculateMeanRotation()
        {
            Quaternion meanRotation = Quaternion.identity;
            foreach (var pose in m_TrackedImagePoses)
            {
                //meanRotation = Quaternion.Slerp(meanRotation, pose.Rotation, 1.0f / m_TrackedImagePoses.Count);
                meanRotation *= Quaternion.Slerp(Quaternion.identity, pose.Rotation, 1.0f / m_TrackedImagePoses.Count);
            }
            return meanRotation;
        }

        private void CalculateStableTrackedImagePose()
        {
            (float positionStdDev, float rotationStdDev) = CalculateStandardDeviations();
            Debug.Log($"[ImageTrackingRelocalizationManager] positionStdDev: {positionStdDev}, rotationStdDev: {rotationStdDev}");

            if (positionStdDev > m_MaxPositionStdDev || rotationStdDev > m_MaxRotationStdDev)
            {
                // Remove the oldest pose
                m_TrackedImagePoses.Dequeue();
                return;
            }

            // We temporarily take the last pose as the final one
            Vector3 finalPosition = m_TrackedImagePoses.Last().Position;
            Quaternion finalRotation = m_TrackedImagePoses.Last().Rotation;
            Debug.Log($"[ImageTrackingRelocalizationManager] finalPosition: {finalPosition}, finalRotation: {finalRotation}");
            StopRelocalization();
            OnTrackedImagePoseStablized?.Invoke(finalPosition, finalRotation);
        }
    }
}
#endif