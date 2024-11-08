// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

#if UNITY_IOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using HoloKit.iOS;

namespace HoloKit.ImageTrackingRelocalization.iOS
{
    /// <summary>
    /// Partial class separating host and client code.
    /// </summary>
    public partial class NetworkImageTrackingStablizer : NetworkBehaviour
    {
        private AppleNativeProvider m_AppleNativeProvider;

        private void Start()
        {
            m_AppleNativeProvider = new();
            if (HoloKitARKitManager.Instance == null)
            {
                Debug.LogWarning("[NetworkImageTrackingStablizer] Failed to find HoloKitARKitManager instance in the scene.");
            }

            Start_Host();
            Start_Client();

            StartCoroutine(TriggerWirelessDataPermission());
        }

        public override void OnNetworkSpawn()
        {
            OnNetworkSpawn_Host();
            OnNetworkSpawn_Client();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            m_AppleNativeProvider.Dispose();
        }

        public static double CalculateStdDev(IEnumerable<double> values)
        {
            double ret = 0;

            if (values.Count() > 0)
            {
                // Compute the Average
                double avg = values.Average();

                // Perform the Sum of (value-avg)^2
                double sum = values.Sum(d => Math.Pow(d - avg, 2));

                // Put it all together
                ret = Math.Sqrt(sum / values.Count());
            }
            return ret;
        }

        /// <summary>
        /// Call this function at start to trigger the iOS Wireless data permission.
        /// </summary>
        private IEnumerator TriggerWirelessDataPermission()
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get("https://www.apple.com/"))
            {
                // Send the request and wait for a response
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error: {webRequest.error}");
                    Debug.Log("Network call failed.");
                }
                else
                {
                    Debug.Log($"Network call succeeded.");
                }
            }
        }
    }
}
#endif
