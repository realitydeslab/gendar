using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using HoloKit;

public class UIManager : MonoBehaviour
{

    [SerializeField]
    string serverPassword = "123";

    GameManager gameManager;    
    HoloKitCameraManager holoKitManager;

    // Pages
    Transform pageHome;
    Transform pageJoinType;
    Transform pagePassword;
    Transform pageWaiting;
    Transform pageRelocalization;
    Transform pageExtraMenu;

    // Floating Panels
    Transform panelWarning;

    // Elements
    TextMeshProUGUI progressText;
    TMP_InputField inputPassword;

    Dictionary<string, Transform> registeredPages = new Dictionary<string, Transform>();

    public float LongPressedTimeThreshold = 3;
    float longPressedTime = 0;

    Transform currentPage = null;

    void Awake()
    {
        holoKitManager = FindFirstObjectByType<HoloKitCameraManager>();
        if(holoKitManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find HoloKitCameraManager.");
        }

        // Pages
        pageHome = FindTransformAndRegister("Page_Home");
        pageJoinType = FindTransformAndRegister("Page_JoinType");
        pagePassword = FindTransformAndRegister("Page_Password");
        pageWaiting = FindTransformAndRegister("Page_Waiting");
        pageRelocalization = FindTransformAndRegister("Page_Relocalization");
        pageExtraMenu = FindTransformAndRegister("Page_ExtraMenu");


        // Floating Panel
        panelWarning = FindTransformAndRegister("Panel_Warning", need_register: false);


        // Elements
        progressText = pageRelocalization.Find("Progress").GetComponent<TextMeshProUGUI>();
        inputPassword = pagePassword.Find("InputField_Password").GetComponent<TMP_InputField>();
    }

    void Start()
    {
        FindButtonAndBind("Page_Home/Button_Play", OnClickPlay_PageHome);
        FindButtonAndBind("Page_Home/Button_Join", OnClickJoin_PageHome);

        FindButtonAndBind("Page_JoinType/Button_Player", OnClickPlayer_PageJoinType);
        FindButtonAndBind("Page_JoinType/Button_Spectator", OnClickSpectator_PageJoinType);
        FindButtonAndBind("Page_JoinType/Button_Server", OnClickServer_PageJoinType);
        FindButtonAndBind("Page_JoinType/Button_Return", ()=> { GotoPage("Page_Home"); });

        FindButtonAndBind("Page_Password/Button_Enter", () => { OnConfirmPassword(transform.Find("Page_Password/InputField_Password")); });
        FindButtonAndBind("Page_Password/Button_Close", () => { GotoPage("Page_JoinType"); });

        FindButtonAndBind("Page_Relocalization/Button_Close", () => { CloseRelocalizationPage(); });

        FindButtonAndBind("Page_ExtraMenu/Button_Calibrate", () => { GotoRelocalizationPage(); });       
        FindButtonAndBind("Page_ExtraMenu/Button_Exit", () => { RestartGame(); });
        FindButtonAndBind("Page_ExtraMenu/Button_Return", () => { HideExtraMenu(); });


        GotoPage("Page_Home");
    }

    void Update()
    {
        // Long Pressed
        if (Input.GetMouseButton(0))
        {
            longPressedTime += Time.deltaTime;
            if (longPressedTime >= LongPressedTimeThreshold)
            {
                // If it's in game mode and screen mode is mono
                if (currentPage == null && (holoKitManager != null && holoKitManager.ScreenRenderMode == HoloKit.ScreenRenderMode.Mono))
                {
                    ShowExtraMenu();
                    longPressedTime = 0;
                }
            }
        }
        else
        {
            longPressedTime = 0;
        }

        // Update relocalization progress
        if(currentPage == pageRelocalization)
            progressText.text = Mathf.FloorToInt(GameManager.Instance.GetRelocalizationProgress() * 100f).ToString() + "%";
    }

    Transform FindTransformAndRegister(string name, bool need_register = true)
    {
        Transform ui_element = transform.Find(name);

        if (ui_element == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find UI element: {name}");
        }
        else
        {
            if(need_register)
                registeredPages.Add(name, ui_element);
        }
        return ui_element;
    }

    Button FindButtonAndBind(string name, UnityAction action)
    {
        Transform ui_element = transform.Find(name);

        if (ui_element == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find UI element: {name}");
            return null;
        }

        Button button = ui_element.GetComponent<Button>();
        if(button == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find Button components on element: {name}");
            return null;
        }

        button.onClick.AddListener(action);
        
        return button;
    }

    #region Click Listeners
    void OnClickPlay_PageHome()
    {
        GameManager.Instance.StartSinglePlayer((result, msg) =>
        {
            if(result == true)
            {
                EnterGame();
            }
        });
    }
    void OnClickJoin_PageHome()
    {
        GotoPage("Page_JoinType");
    }
    void OnClickPlayer_PageJoinType()
    {
        GotoWaitingPage("Connecting to server..");
            
        GameManager.Instance.JoinAsPlayer((result, msg)=> {
            if(result == true)
            {
                RegisterCallback();

                GotoRelocalizationPage();
            }
            else
            {
                GotoWaitingPage(msg, 2, ()=>
                {
                    GotoPage("Page_JoinType");
                }); // display for 2s
            }
        });
    }
    void OnClickSpectator_PageJoinType()
    {
        GotoWaitingPage("Connecting to server..");

        GameManager.Instance.JoinAsSpectator((result, msg) => {
            if (result == true)
            {
                RegisterCallback();

                GotoRelocalizationPage();
            }
            else
            {
                GotoWaitingPage(msg, 2, ()=>
                {
                    GotoPage("Page_JoinType");
                }); // display for 2s
            }
        });
    }
    void OnClickServer_PageJoinType()
    {
        GotoPage("Page_Password");
    }

    void OnConfirmPassword(Transform input_transform)
    {
        if(input_transform == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find input element for Password.");
            return;
        }

        TMP_InputField input_field = input_transform.GetComponent<TMP_InputField>();
        if (input_field == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find input component for Password.");
            return;
        }

        string pwd = input_field.text;
        if(pwd == serverPassword)
        {
            GameManager.Instance.JoinAsHost((result, msg) =>
            {
                if (result == true)
                {
                    GotoRelocalizationPage();
                }
                else
                {
                    GotoWaitingPage(msg, auto_close_time:2, ()=> {
                        // if failed, go back to previous page
                        GotoPage("Page_JoinType");
                    });
                }
            });
        }
        else
        {
            GotoWaitingPage("Wrong Password.", auto_close_time: 2, ()=> {
                // if wrong, type again
                GotoPage("Page_Password");
            });
        }
    }
    #endregion

    #region UI controlness
    void GotoPage(string page_name)
    {
        Debug.Log($"[{this.GetType()}] Goto Page: {page_name}");

        Transform target_page = null;

        if(registeredPages.ContainsKey(page_name))
        {
            target_page = registeredPages[page_name];
        }        

        GotoPage(target_page);
    }

    void GotoPage(Transform target_page)
    {
        // Only show desired page
        ShowPageOnly(target_page);

        // Extra operation: Set default password if in development state
        if(target_page == pagePassword && GameManager.Instance.IsInDevelopment)
        {
            inputPassword.text = serverPassword;
        }

        // update indicator
        currentPage = target_page;
    }    

    void ShowPageOnly(Transform target_page)
    {
        foreach (var page in registeredPages.Values)
        {
            page.gameObject.SetActive(page == target_page);
        }
    }

    void ShowElementsOnly(Transform[] dst_elemetns)
    {
        foreach (Transform element in registeredPages.Values)
        {
            bool match = false;
            if (dst_elemetns != null)
            {
                foreach (Transform dst_element in dst_elemetns)
                {
                    if (element == dst_element)
                    {
                        match = true;
                        break;
                    }
                }
            }
            element.gameObject.SetActive(match);
        }
    }
    #endregion

    #region Page navagations
    void GotoWaitingPage(string msg, float auto_close_time = 0, System.Action action = null)
    {
        SetMessage_WaitingPage(msg);

        GotoPage("Page_Waiting");

        if(auto_close_time > 0)
        {
            StartCoroutine(HideWaitingPage(auto_close_time, action));
        }
    }

    IEnumerator HideWaitingPage(float delay_time, System.Action action)
    {
        yield return new WaitForSeconds(delay_time);

        SetMessage_WaitingPage("");

        action?.Invoke();
    }

    void SetMessage_WaitingPage(string msg)
    {
        pageWaiting.Find("Message").GetComponent<TextMeshProUGUI>().text = msg;
    }

    void GotoRelocalizationPage()
    {
        GotoPage("Page_Relocalization");

        GameManager.Instance.StartRelocalization(()=> {
            EnterGame();
        });
    }

    void CloseRelocalizationPage()
    {
        EnterGame();

        GameManager.Instance.StopRelocalization();
    }

    void EnterGame()
    {
        GotoPage(page_name:"EnterGame");
    }

    void ShowExtraMenu()
    {
        GotoPage("Page_ExtraMenu");
    }

    void HideExtraMenu()
    {
        EnterGame();
    }

    public void ShowWarningText(string msg)
    {
        panelWarning.Find("Message").GetComponent<TextMeshProUGUI>().text = msg;
        panelWarning.gameObject.SetActive(true);

        StartCoroutine(HideWarning());
    }

    IEnumerator HideWarning()
    {
        yield return new WaitForSeconds(2);
        panelWarning.gameObject.SetActive(false);
    }

    /// <summary>
    /// Restart game when
    /// 1. click exit game - client/server
    /// 2. suddenly lost server - client
    /// </summary>
    void RestartGame()
    {
        GameManager.Instance.RestartGame(() =>
        {
            UnregisterCallback();

            GotoPage("Page_Home");            
        });
    }
    #endregion

    #region Callbacks
    void RegisterCallback()
    {
        GameManager.Instance.ConnectionManager.OnServerLostEvent.AddListener(OnServerLostCallback);
    }

    void UnregisterCallback()
    {
        GameManager.Instance.ConnectionManager.OnServerLostEvent.RemoveListener(OnServerLostCallback);
    }

    void OnServerLostCallback()
    {
        RestartGame();
    }
    #endregion
}
