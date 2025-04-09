using Unity.Netcode;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance;

    public NetworkVariable<bool> IsWhiteTurn = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool CanMove(bool isPlayerWhite) => IsWhiteTurn.Value == isPlayerWhite;

    [ServerRpc(RequireOwnership = false)]
    public void EndTurnServerRpc()
    {
        IsWhiteTurn.Value = !IsWhiteTurn.Value;
    }
}
