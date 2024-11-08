using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

public class PlayerManager : MonoBehaviour
{
    public Dictionary<ulong, Player> PlayerList { get => playerList; }
    protected Dictionary<ulong, Player> playerList = new();


    public UnityEvent<ulong> OnPlayerJoined;
    public UnityEvent<ulong> OnPlayerLeft; 

    void Update()
    {
        UpdatePlayerList();
    }

    void UpdatePlayerList()
    {
        if (GameManager.Instance.GameMode == GameMode.SinglePlayer || GameManager.Instance.GameMode == GameMode.Undefined)
            return;

        if (NetworkManager.Singleton == null)
            return;        

        var gameobject_list = GameObject.FindGameObjectsWithTag("Player");

        // check if player left
        List<ulong> player_to_be_removed = new();
        foreach (var player in playerList)
        {
            ulong client_id = player.Key;

            bool exist = false;
            for (int i = 0; i < gameobject_list.Length; i++)
            {
                if (gameobject_list[i].GetComponent<NetworkObject>().OwnerClientId == client_id)
                {
                    exist = true;
                    break;
                }
            }

            if (exist == false)
            {
                player_to_be_removed.Add(client_id);
            }
        }

        for (int i = 0; i < player_to_be_removed.Count; i++)
        {
            ulong client_id = player_to_be_removed[i];

            playerList.Remove(client_id);

            OnPlayerLeft?.Invoke(client_id);

            Debug.Log($"[{ this.GetType()}] Player {client_id} Left. Player Count:{playerList.Count}");
        }

        // check if new player joined
        for (int i = 0; i < gameobject_list.Length; i++)
        {
            ulong client_id = gameobject_list[i].GetComponent<NetworkObject>().OwnerClientId;
            if (playerList.ContainsKey(client_id) == false)
            {
                playerList.Add(client_id, gameobject_list[i].GetComponent<Player>());

                OnPlayerJoined?.Invoke(client_id);

                Debug.Log($"[{ this.GetType()}] Player {client_id} Joined. Player Count:{playerList.Count}");
            }
        }

        //
        //int debug_index = 0;
        //foreach (var player in playerList)
        //{
        //    if (player.Value == null)
        //    {
        //        Debug.Log($"[{this.GetType()}] {debug_index} is Null!");
        //    }
        //    else
        //    {
        //        Debug.Log($"[{this.GetType()}] player {player.Value.OwnerClientId}: Body:{player.Value.Body == null}, Hand:{player.Value.Hand == null}");
        //    }
        //    debug_index++;
        //}
    }

    public void ResetPlayerList()
    {
        playerList.Clear();
    }

    //private static PlayerManager _Instance;

    //public static PlayerManager Instance
    //{
    //    get
    //    {
    //        if (_Instance == null)
    //        {
    //            _Instance = GameObject.FindFirstObjectByType<PlayerManager>();
    //            if (_Instance == null)
    //            {
    //                Debug.Log("Can't find PlayerManager in the scene, will create a new one.");
    //                GameObject go = new GameObject();
    //                _Instance = go.AddComponent<PlayerManager>();
    //            }
    //        }
    //        return _Instance;
    //    }
    //}
}
