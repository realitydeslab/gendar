using UnityEngine;
using UnityEngine.UI;

public class LiDarRequirement : MonoBehaviour
{
    [SerializeField]
    GameObject instructionPanel;
    
    void Start()
    {
        if (instructionPanel == null) return;

        bool supported = HoloKit.iOS.DeviceData.SupportLiDAR();

#if UNITY_EDITOR
        supported = true;
#endif

        instructionPanel.SetActive(!supported);
    }
}
