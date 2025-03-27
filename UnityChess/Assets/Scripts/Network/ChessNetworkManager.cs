using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
using System;

public class ChessNetworkManager : MonoBehaviour
{
    private NetworkManager networkManager;

    void Awake()
    {
        InitializeNetworkManager();
    }

    private void InitializeNetworkManager()
    {
        networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            networkManager = FindObjectOfType<NetworkManager>();

            if (networkManager == null)
            {
                UnityEngine.Debug.LogError("NetworkManager not found in the scene. Please add a NetworkManager component.");
                return;
            }
        }

        networkManager.ConnectionApprovalCallback += ApproveConnection;
    }

    private void ApproveConnection(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        if (networkManager == null)
        {
            UnityEngine.Debug.LogError("NetworkManager is null during connection approval");
            response.Approved = false;
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = true;

        if (networkManager.ConnectedClientsIds.Count >= 2)
        {
            response.Approved = false;
            UnityEngine.Debug.Log("Connection rejected: Maximum players reached");
            return;
        }
    }

    public void StartHost()
    {
        if (networkManager == null)
        {
            InitializeNetworkManager();
        }

        if (networkManager != null)
        {
            networkManager.StartHost();
        }
        else
        {
            UnityEngine.Debug.LogError("Cannot start host: NetworkManager is null");
        }
    }

    public void StartClient()
    {
        if (networkManager == null)
        {
            InitializeNetworkManager();
        }

        if (networkManager != null)
        {
            networkManager.StartClient();
        }
        else
        {
            UnityEngine.Debug.LogError("Cannot start client: NetworkManager is null");
        }
    }

    void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.ConnectionApprovalCallback -= ApproveConnection;
        }
    }
}
