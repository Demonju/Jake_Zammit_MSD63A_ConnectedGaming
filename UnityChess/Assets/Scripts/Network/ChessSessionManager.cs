using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

public class ChessSessionManager : NetworkBehaviour
{
    public event Action<bool> OnSessionJoined;
    public event Action<string> OnConnectionError;

    [ServerRpc(RequireOwnership = false)]
    public void JoinSessionServerRpc(string sessionCode)
    {
        // Validate session code
        if (string.IsNullOrEmpty(sessionCode))
        {
            OnConnectionError?.Invoke("Invalid Session Code");
            return;
        }

        // Additional session management logic
        try
        {
            // Logic to add player to session
            OnSessionJoined?.Invoke(true);
        }
        catch (Exception ex)
        {
            OnConnectionError?.Invoke($"Connection Failed: {ex.Message}");
        }
    }
}
