using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;

public class NetcodeConnectionManager : MonoBehaviour
{
    [SerializeField]
    string serverIP = "192.168.0.135";
    public string ServerIP { get => serverIP; set => serverIP = value; }

    [SerializeField]
    ushort serverPort = 7777;
    public ushort Port { get => serverPort; set => serverPort = value; }

    string localIP = "";
    public string LocalIP { get => localIP; }

    public UnityEvent<ulong> OnClientJoinedEvent;
    public UnityEvent<ulong> OnClientLostEvent;
    public UnityEvent OnServerLostEvent;

    Action<bool, string> OnReceiveConnectionResultAction;

    #region Event Listener
    void RegisterCallback()
    {
        Debug.Log($"[{this.GetType()}] Register NetCode Listener");
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    void UnregisterCallback()
    {
        Debug.Log($"[{this.GetType()}] Unregister NetCode Listener");
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    void OnClientConnectedCallback(ulong client_id)
    {
        // As a server, if a new client joined
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"[{this.GetType()}] Client (ID= {client_id}) just connected.");

            OnClientJoinedEvent?.Invoke(client_id);
        }

        // As a client, if successfully connected to a server
        if (NetworkManager.Singleton.IsClient && NetworkManager.Singleton.IsHost == false)
        {
            string msg = "Client successfully connected to server.";
            OnReceiveConnectionResultAction?.Invoke(true, msg);
            OnReceiveConnectionResultAction = null;

            localIP = GetLocalIPAddress();
        }
    }

    void OnClientDisconnectCallback(ulong client_id)
    {

        // As a server, when client left
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"[{this.GetType()}] Client (ID= {client_id}) just left.");

            OnClientLostEvent?.Invoke(client_id);
        }

        // As a client
        if (NetworkManager.Singleton.IsClient && NetworkManager.Singleton.IsHost == false)
        {
            // When Client trys to connect with Server but failed
            if (OnReceiveConnectionResultAction != null)
            {
                string msg = "Couldn't connect to server.";
                OnReceiveConnectionResultAction?.Invoke(false, msg);
                OnReceiveConnectionResultAction = null;

                UnregisterCallback();
            }

            // When suddenly lost Server
            else
            {
                Debug.Log($"[{this.GetType()}] Server lost.");

                OnServerLostEvent?.Invoke();

                UnregisterCallback();
            }
        }
    }
        #endregion

    public bool IsIPAddressValide(string ip)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(ip, @"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");
    }
    

    #region Public Functions
    public void StartClient(Action<bool, string> callback)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.Log($"[{this.GetType()}] Please wait for NetworkManager to initialize");

            return;
        }

        if (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening)
        {
            Debug.Log($"[{this.GetType()}] Connection has already been established. Please shutdown first.");

            return;
        }

        if (IsIPAddressValide(serverIP) == false)
        {
            string msg = "Server ip is invalid.";

            Debug.Log($"[{this.GetType()}] {msg}");

            callback?.Invoke(false, msg);

            return;
        }
        

        OnBeforeClientStarted();

        RegisterCallback();

        Debug.Log($"[{this.GetType()}] Starting Client and connecting to {serverIP}:{serverPort}");

        bool result = NetworkManager.Singleton.StartClient();

        if (result == true)
        {
            OnReceiveConnectionResultAction = callback;
        }
        else
        {
            string msg = "Failed to start client.";

            Debug.Log($"[{this.GetType()}] {msg}");

            callback?.Invoke(false, msg);

            UnregisterCallback();
        }
    }

    public void StartServer(Action<bool, string> callback)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.Log($"[{this.GetType()}] Please wait for NetworkManager to initialize");

            return;
        }

        if (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening)
        {
            Debug.Log($"[{this.GetType()}] Connection has already been established. Please shutdown first.");

            return;
        }

        OnBeforeHostStarted();

        RegisterCallback();

        Debug.Log($"[{this.GetType()}] Starting Server.");

        // Will send back result instantly
        bool result = NetworkManager.Singleton.StartServer();

        string msg = result ? "Server established." : "Failed to start server.";        

        Debug.Log($"[{this.GetType()}] {msg}");

        serverIP = localIP = GetLocalIPAddress();

        callback?.Invoke(result, msg);

        if (result == false) UnregisterCallback();
    }

    public void StartHost(Action<bool, string> callback)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.Log($"[{this.GetType()}] Please wait for NetworkManager to initialize");

            return;
        }

        if (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening)
        {
            Debug.Log($"[{this.GetType()}] Connection has already been established. Please shutdown first.");

            return;
        }

        OnBeforeHostStarted();

        RegisterCallback();

        Debug.Log($"[{this.GetType()}] Starting Host.");

        // Will send back result instantly
        bool result = NetworkManager.Singleton.StartHost();

        string msg = result ? "Host established." : "Failed to start host.";

        Debug.Log($"[{this.GetType()}] {msg}");

        serverIP = localIP = GetLocalIPAddress();

        callback?.Invoke(result, msg);

        if (result == false) UnregisterCallback();
    }

    public void ShutDown()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.Log($"[{this.GetType()}] Please wait for NetworkManager to initialize");

            return;
        }

        if(NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening)
        {
            Debug.Log($"[{this.GetType()}] Shuting down.");

            NetworkManager.Singleton.Shutdown();

            UnregisterCallback();
        }
    } 
    #endregion

    void OnBeforeHostStarted()
    {
        var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (unityTransport != null)
        {
            unityTransport.SetConnectionData("127.0.0.1", serverPort); 
            unityTransport.ConnectionData.ServerListenAddress = "0.0.0.0";
        }
    }
    void OnBeforeClientStarted()
    {
        var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (unityTransport != null)
        {
            unityTransport.SetConnectionData(serverIP, serverPort); // default is 7777
        }
    }

    string GetLocalIPAddress()
    {

        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) // && ip.ToString().Contains("192.168"))
            {
                return ip.ToString();
            }
        }
        //throw new System.Exception("No network adapters with an IPv4 address in the system!");
        return "";
    }

}
