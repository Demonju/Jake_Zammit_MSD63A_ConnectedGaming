using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections;
using UnityChess;

public class NetworkPlayer : NetworkBehaviour
{
    // Reference to the local player instance
    public static NetworkPlayer LocalInstance;

    // NetworkVariables to sync player info across the network
    public NetworkVariable<FixedString64Bytes> PlayerUniqueID = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsWhite = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString64Bytes> EquippedSkinFileName = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private const string UserIdKey = "LocalUserId"; // Key for storing local user ID in PlayerPrefs

    public override void OnNetworkSpawn()
    {
        // Server listens for player ID changes
        if (IsServer)
            PlayerUniqueID.OnValueChanged += OnPlayerUniqueIDChanged;

        // If this is the local player, store reference and initialize setup
        if (IsOwner)
        {
            LocalInstance = this;
            Invoke(nameof(SetupPlayer), 0.1f); // Small delay to ensure player is fully initialized
        }

        // Subscribe to skin changes
        EquippedSkinFileName.OnValueChanged += OnSkinChanged;

        // Immediately apply skin if already set
        if (!string.IsNullOrEmpty(EquippedSkinFileName.Value.ToString()))
            OnSkinChanged("", EquippedSkinFileName.Value.ToString());
    }

    // Called when skin value changes
    private void OnSkinChanged(FixedString64Bytes oldSkin, FixedString64Bytes newSkin)
    {
        if (string.IsNullOrEmpty(newSkin.ToString())) return;

        // Determine player side and apply skin
        Side side = IsWhite.Value ? Side.White : Side.Black;
        SkinLoader.Instance.ApplySkinFromFirebase(newSkin.ToString(), side);
    }

    // Setup player on the client side
    private void SetupPlayer()
    {
        // Retrieve or generate a unique user ID
        string userId = PlayerPrefs.GetString(UserIdKey, "");
        if (string.IsNullOrEmpty(userId))
        {
            userId = $"User_{Random.Range(1000, 9999)}";
            PlayerPrefs.SetString(UserIdKey, userId);
        }

        // Request server to set player color and ID
        SetPlayerColorServerRpc();
        SetPlayerUniqueIdServerRpc(userId);
    }

    // Triggered on the server when the player's unique ID changes
    private void OnPlayerUniqueIDChanged(FixedString64Bytes oldVal, FixedString64Bytes newVal)
    {
        string newID = newVal.ToString();
        if (!string.IsNullOrEmpty(newID))
        {
            Debug.Log($"[Server] Unique ID updated: {OwnerClientId} => {newID}");

            // Notify the NetworkChessManager to handle reconnections and tracking
            NetworkChessManager.Instance.HandlePlayerIDSet(OwnerClientId, newID);
        }
    }

    // Server RPC to assign unique ID to player
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerUniqueIdServerRpc(string id)
    {
        PlayerUniqueID.Value = id;
        Debug.Log($"[Server] Set ID '{id}' for player {OwnerClientId}");
    }

    // Server RPC to assign player color (white for first player, black for second)
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerColorServerRpc()
    {
        IsWhite.Value = NetworkManager.Singleton.ConnectedClients.Count <= 1;
    }

    // Helper method to check if it's this player's turn
    public bool IsMyTurn() => TurnManager.Instance.CanMove(IsWhite.Value);

    // Server RPC to update the skin filename
    [ServerRpc(RequireOwnership = false)]
    public void SetEquippedSkinServerRpc(string skinFileName)
    {
        EquippedSkinFileName.Value = skinFileName;
    }
}

