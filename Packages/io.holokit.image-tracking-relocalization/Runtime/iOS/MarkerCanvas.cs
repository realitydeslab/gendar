// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

#if UNITY_IOS
using UnityEngine;
using UnityEngine.UI;

namespace HoloKit.ImageTrackingRelocalization.iOS
{
    public class MarkerCanvas : MonoBehaviour
    {
        public RectTransform Background => m_Background;

        public RectTransform MarkerImage => m_MarkerImage;

        [SerializeField] private RectTransform m_Background;

        [SerializeField] private RectTransform m_MarkerImage;
    }
}
#endif
