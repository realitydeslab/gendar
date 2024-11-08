// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

#if UNITY_IOS
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Netcode;

namespace HoloKit.ImageTrackingRelocalization.iOS
{
    public class PlayerPoseSynchronizer : NetworkBehaviour
    {
        [SerializeField] private bool m_IsSyncingPose = true;

        private ARCameraManager m_ARCameraManager;

        private void Update()
        {
            if (!m_IsSyncingPose)
                return;

            if (IsSpawned && IsOwner)
            {
                if (m_ARCameraManager == null)
                {
                    m_ARCameraManager = FindFirstObjectByType<ARCameraManager>();
                }

                if (m_ARCameraManager != null)
                {
                    transform.SetPositionAndRotation(m_ARCameraManager.transform.position, m_ARCameraManager.transform.rotation);
                }
            }
        }
    }
}
#endif
