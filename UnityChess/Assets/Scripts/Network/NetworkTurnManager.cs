using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections;

public class NetworkTurnManager : NetworkBehaviour
{
    public static NetworkTurnManager Instance;

    public NetworkVariable<bool> IsWhiteTurn = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool CanMove(bool isPlayerWhite)
    {
        return IsWhiteTurn.Value == isPlayerWhite;
    }

    [ServerRpc(RequireOwnership = false)]
    public void EndTurnServerRpc()
    {
        IsWhiteTurn.Value = !IsWhiteTurn.Value;
    }
}
