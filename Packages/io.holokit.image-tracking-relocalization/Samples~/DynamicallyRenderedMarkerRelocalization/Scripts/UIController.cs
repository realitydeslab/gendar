// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

#if UNITY_IOS
using UnityEngine;
using HoloInteractive.XR.ImageTrackingRelocalization.iOS;
using TMPro;
using Unity.Netcode;

namespace HoloKit.ImageTrackingRelocalization.Samples.DynamicallyRenderedMarkerRelocalization
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private GameObject m_HomePanel;

        [SerializeField] private GameObject m_HostPanel;

        [SerializeField] private GameObject m_ClientPanel;

        [SerializeField] private GameObject m_AlignmentMarkerPanel;

        [SerializeField] private GameObject m_ResyncPoseButton;

        [SerializeField] private GameObject m_ConnectingText;

        [SerializeField] private GameObject m_SyncingTimestampText;

        [SerializeField] private GameObject m_TrackingMarkerText;

        [SerializeField] private NetworkImageTrackingStablizer m_NetworkImageTrackingStablizer;

        [SerializeField] private TMP_Text m_ToggleDisplayMarkerButtonText;

        private void Start()
        {
            m_HomePanel.SetActive(true);
            m_HostPanel.SetActive(false);
            m_ClientPanel.SetActive(false);

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[UIController] OnClientConnected: {clientId}");

            if (NetworkManager.Singleton.LocalClientId != 0)
            {
                m_ConnectingText.SetActive(false);
                m_SyncingTimestampText.SetActive(true);
            }
        }

        private void OnClientDisconnect(ulong clientId)
        {
            Debug.Log($"[UIController] OnClientDisconnect: {clientId}");
        }

        public void StartHost()
        {
            m_HomePanel.SetActive(false);
            m_HostPanel.SetActive(true);
            m_ClientPanel.SetActive(false);

#if !UNITY_EDITOR
            NetworkManager.Singleton.StartHost();
#endif
        }

        public void StartClient()
        {
            m_HomePanel.SetActive(false);
            m_HostPanel.SetActive(false);
            m_ClientPanel.SetActive(true);
            m_AlignmentMarkerPanel.SetActive(false);
            m_ConnectingText.SetActive(true);

#if !UNITY_EDITOR
            NetworkManager.Singleton.StartClient();
#endif
        }

        public void Shutdown()
        {
            m_HomePanel.SetActive(true);
            m_HostPanel.SetActive(false);
            m_ClientPanel.SetActive(false);
            m_AlignmentMarkerPanel.SetActive(false);
            m_ResyncPoseButton.SetActive(false);
            m_ConnectingText.SetActive(false);
            m_SyncingTimestampText.SetActive(false);
            m_TrackingMarkerText.SetActive(false);

            if (NetworkManager.Singleton.IsHost)
            {
                if (m_NetworkImageTrackingStablizer.IsDisplayingMarker)
                    ToggleDisplayMarker();
            }

            NetworkManager.Singleton.Shutdown();
        }

        public void ToggleDisplayMarker()
        {
            if (m_NetworkImageTrackingStablizer.IsDisplayingMarker)
            {
                m_NetworkImageTrackingStablizer.StopDisplayingMarker();
                m_ToggleDisplayMarkerButtonText.text = "Display Marker";
            }
            else
            {
                m_NetworkImageTrackingStablizer.StartDisplayingMarker();
                m_ToggleDisplayMarkerButtonText.text = "Hide Marker";
            }
        }

        public void OnTimestampSynced()
        {
            m_SyncingTimestampText.SetActive(false);
            m_TrackingMarkerText.SetActive(true);
        }

        public void OnPoseSynced()
        {
            m_AlignmentMarkerPanel.SetActive(true);
            m_TrackingMarkerText.SetActive(false);
        }

        public void OnAlignmentMarkerDenied()
        {
            m_AlignmentMarkerPanel.SetActive(false);
            m_TrackingMarkerText.SetActive(true);
        }

        public void OnAlignmentMarkerAccepted()
        {
            m_AlignmentMarkerPanel.SetActive(false);
            m_ResyncPoseButton.SetActive(true);
        }

        public void OnResyncPose()
        {
            m_ResyncPoseButton.SetActive(false);
        }

        public void AcceptMarker()
        {
            m_NetworkImageTrackingStablizer.AcceptAlignmentMarker();
        }

        public void DenyMarker()
        {
            m_NetworkImageTrackingStablizer.DenyAlignmentMarker();
        }

        public void ResyncPose()
        {
            m_NetworkImageTrackingStablizer.ResyncPose();
        }
    }
}
#endif
