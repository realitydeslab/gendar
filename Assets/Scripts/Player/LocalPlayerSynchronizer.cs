using UnityEngine;
using Unity.Netcode;
using HoloKit.iOS;
using UnityEngine.XR.ARFoundation;

public class LocalPlayerSynchronizer : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] Vector3 fakeBodyPosition;
    [SerializeField] Vector3 fakeBodyRotation;
    [SerializeField] Vector3 fakeHandPosition;
#endif

    HandTrackingManager handTrackingManager;
    ARCameraManager arCameraManager;

    void Awake()
    {
        arCameraManager = FindFirstObjectByType<ARCameraManager>();
        if (arCameraManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find ARCameraManager.");
        }

        handTrackingManager = FindFirstObjectByType<HandTrackingManager>();
        if (handTrackingManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find HandTrackingManager.");
        }
    }

    void Update()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null
            || NetworkManager.Singleton.LocalClient.PlayerObject.IsSpawned == false)
            return;

        if (GameManager.Instance == null || GameManager.Instance.GameMode == GameMode.Undefined)
            return;



        if (arCameraManager != null)
        {
            Transform body = NetworkManager.Singleton.LocalClient.PlayerObject.transform.GetChild(0);

#if !UNITY_EDITOR
            body.SetPositionAndRotation(arCameraManager.transform.position, arCameraManager.transform.rotation);
#else
            body.SetPositionAndRotation(fakeBodyPosition, Quaternion.Euler(fakeBodyRotation));
#endif
            Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Body", body.position.ToString());
        }

        if (handTrackingManager != null)
        {
            Transform hand = NetworkManager.Singleton.LocalClient.PlayerObject.transform.GetChild(1);
#if !UNITY_EDITOR
            hand.position = handTrackingManager.GetHandJointPosition(0, JointName.MiddleMCP);
#else
            hand.position = fakeHandPosition;
#endif
            Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Hand", hand.position.ToString());
        }

        
        
    }
}
