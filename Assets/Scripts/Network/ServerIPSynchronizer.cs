using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OscJack;
using UnityEngine.Events;


public class ServerIPSynchronizer : MonoBehaviour
{
    private string serverIp = "";
    public string ServerIP
    {
        get => serverIp;
    }

    float receivingTimeOut = 10;

    OscPropertySenderModified oscSender;
    OscEventReceiver oscReceiver;

    void Awake()
    {
        oscSender = gameObject.GetComponent<OscPropertySenderModified>();
        oscReceiver = gameObject.GetComponent<OscEventReceiver>();
    }

    public void StartReceivingServerIp(System.Action<bool, string> action)
    {
        oscReceiver.enabled = true;

        Debug.Log($"[{this.GetType()}] Start receiving ServerIp.");

        StartCoroutine(TryReceivingServerIp(action));
    }

    IEnumerator TryReceivingServerIp(System.Action<bool, string> action)
    {
        float start_time = Time.time;
        bool result = false;
        while (Time.time - start_time < receivingTimeOut)
        {
            if (serverIp.Length > 0 && IsIPAddressValide(serverIp))
            {
                result = true;
                break;
            }

            Debug.Log($"[{this.GetType()}] Elapsed time: {Time.time - start_time}");

            yield return new WaitForSeconds(1);            
        }

        oscReceiver.enabled = false;

        if (result)
        {
            // successfully received the server ip
            Debug.Log($"[{this.GetType()}] Server Ip has been successfully sychronized. Server Ip:{serverIp}");
            action?.Invoke(true, serverIp);
        }
        else
        {
            // failed to receive the server ip
            string msg = "Server Ip sychronization time out.";
            Debug.Log($"[{this.GetType()}] {msg}");
            action?.Invoke(false, msg);
        }
    }

    public void OnReceiveServerIp(string ip)
    {
        if (ip.Length > 0 && IsIPAddressValide(ip))
        {
            serverIp = ip;
            Debug.Log($"[{this.GetType()}]Received Server Ip:{ip}");
        }
    }

    public void StartBroadcastingServerIp(string ip)
    {
        serverIp = ip;

        // keep sending server ip
        oscSender.enabled = true;

        Debug.Log($"[{this.GetType()}] Start broadcasting ServerIp.");
    }

    public void StopBroadcastingServerIp()
    {
        oscSender.enabled = false;

        Debug.Log($"[{this.GetType()}] Stop broadcasting ServerIp.");
    }

    public void ResetConnection()
    {
        serverIp = "";
        oscSender.enabled = false;
        oscReceiver.enabled = false;

        Debug.Log($"[{this.GetType()}] Reset Server IP Synchronizer.");
    }

    public bool IsIPAddressValide(string ip)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(ip, @"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");
    }
}
