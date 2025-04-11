using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections;
using UnityChess;

public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer LocalInstance;

    public NetworkVariable<FixedString64Bytes> PlayerUniqueID = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsWhite = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString64Bytes> EquippedSkinFileName = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    private const string UserIdKey = "LocalUserId";

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            PlayerUniqueID.OnValueChanged += OnPlayerUniqueIDChanged;

        if (IsOwner)
        {
            LocalInstance = this;
            Invoke(nameof(SetupPlayer), 0.1f);
        }

        EquippedSkinFileName.OnValueChanged += OnSkinChanged;

        // Apply skin immediately if already set
        if (!string.IsNullOrEmpty(EquippedSkinFileName.Value.ToString()))
            OnSkinChanged("", EquippedSkinFileName.Value.ToString());
    }

    private void OnSkinChanged(FixedString64Bytes oldSkin, FixedString64Bytes newSkin)
    {
        if (string.IsNullOrEmpty(newSkin.ToString())) return;

        // Use player color from IsWhite
        Side side = IsWhite.Value ? Side.White : Side.Black;
        SkinLoader.Instance.ApplySkinFromFirebase(newSkin.ToString(), side);
    }



    private void SetupPlayer()
    {
        string userId = PlayerPrefs.GetString(UserIdKey, "");
        if (string.IsNullOrEmpty(userId))
        {
            userId = $"User_{Random.Range(1000, 9999)}";
            PlayerPrefs.SetString(UserIdKey, userId);
        }

        SetPlayerColorServerRpc();
        SetPlayerUniqueIdServerRpc(userId);
    }

    private void OnPlayerUniqueIDChanged(FixedString64Bytes oldVal, FixedString64Bytes newVal)
    {
        string newID = newVal.ToString();
        if (!string.IsNullOrEmpty(newID))
        {
            Debug.Log($"[Server] Unique ID updated: {OwnerClientId} => {newID}");
            NetworkChessManager.Instance.HandlePlayerIDSet(OwnerClientId, newID);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerUniqueIdServerRpc(string id)
    {
        PlayerUniqueID.Value = id;
        Debug.Log($"[Server] Set ID '{id}' for player {OwnerClientId}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerColorServerRpc()
    {
        IsWhite.Value = NetworkManager.Singleton.ConnectedClients.Count <= 1;
    }

    public bool IsMyTurn() => TurnManager.Instance.CanMove(IsWhite.Value);

    [ServerRpc(RequireOwnership = false)]
    public void SetEquippedSkinServerRpc(string skinFileName)
    {
        EquippedSkinFileName.Value = skinFileName;
    }

}
