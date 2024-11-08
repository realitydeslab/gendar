// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

#if UNITY_IOS
using UnityEngine;
using HoloKit;

namespace HoloKit.ImageTrackingRelocalization.iOS
{
    public class MarkerRenderer : MonoBehaviour
    {
        [SerializeField] private PhoneModelList m_PhoneModelList;

        [SerializeField] private Sprite m_MarkerImage;

        [SerializeField] private MarkerCanvas m_MarkerCanvasPrefab;

        private MarkerCanvas m_MarkerCanvas;

        private const float BACKGROUND_RATIO = 0.4f;

        private const float MARKER_WIDTH = 0.04f; // In meters

        private const float METER_TO_INCH_RATIO = 39.3701f;

        private const float INCH_TO_METER_RATIO = 0.0254f;

        public (Vector3, Vector3) SpawnMarker()
        {
            PhoneModel phoneModel = GetCurrentPhoneModel();

            m_MarkerCanvas = Instantiate(m_MarkerCanvasPrefab);
            // Adjust the height of the background
            float screenHeight = phoneModel.ModelSpecs.ScreenResolution == Vector2.zero ? GetScreenWidth() : phoneModel.ModelSpecs.ScreenResolution.x;
            m_MarkerCanvas.Background.offsetMax = new(0f, -(screenHeight * (1 - BACKGROUND_RATIO)));
            // Adjust the size of the marker image
            float screenDpi = phoneModel.ModelSpecs.ScreenDpi == 0f ? Screen.dpi : phoneModel.ModelSpecs.ScreenDpi;
            float markerWidth = MARKER_WIDTH * METER_TO_INCH_RATIO * screenDpi;
            m_MarkerCanvas.MarkerImage.sizeDelta = new(markerWidth, markerWidth);

            // Calculate the camera to marker offset
            Vector3 phoneModelCameraOffset = new(phoneModel.ModelSpecs.CameraOffset.y, -phoneModel.ModelSpecs.CameraOffset.x, phoneModel.ModelSpecs.CameraOffset.z);
            float horizontalOffset = m_MarkerCanvas.MarkerImage.position.x / screenDpi * INCH_TO_METER_RATIO;
            float verticalOffset = -((screenHeight / 2f) - m_MarkerCanvas.MarkerImage.position.y) / screenDpi * INCH_TO_METER_RATIO;
            Vector3 cameraToMarkerOffset = phoneModelCameraOffset + new Vector3(horizontalOffset, 0f, 0f) + new Vector3(0f, verticalOffset, 0f);
            // Calculate the camera to screen center offset
            float screenWidth = phoneModel.ModelSpecs.ScreenResolution == Vector2.zero ? GetScreenHeight() : phoneModel.ModelSpecs.ScreenResolution.y;
            Vector3 cameraToScreenCenterOffset = phoneModelCameraOffset + new Vector3(screenWidth / 2f / screenDpi * INCH_TO_METER_RATIO, 0f, 0f);

            return (cameraToMarkerOffset, cameraToScreenCenterOffset);
        }

        public void DestroyMarker()
        {
            if (m_MarkerCanvas != null)
                Destroy(m_MarkerCanvas.gameObject);
        }

        private PhoneModel GetCurrentPhoneModel()
        {
#if UNITY_EDITOR
            return DeviceProfile.GetDefaultPhoneModel();
#else
            string modelName = SystemInfo.deviceModel;
            foreach (PhoneModel phoneModel in m_PhoneModelList.PhoneModels)
            {
                if (modelName.Equals(phoneModel.ModelName))
                {
                    return phoneModel;
                }
            }
            return DeviceProfile.GetDefaultPhoneModel();
#endif
        }

        public static float GetScreenWidth()
        {
            return Screen.width > Screen.height ? Screen.width : Screen.height;
        }

        public static float GetScreenHeight()
        {
            return Screen.width > Screen.height ? Screen.height : Screen.width;
        }
    }
}
#endif
