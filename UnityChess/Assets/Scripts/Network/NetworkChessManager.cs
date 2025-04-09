using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityChess;

public class NetworkChessManager : NetworkBehaviour
{
    public static NetworkChessManager Instance;

    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;

    private Dictionary<string, ulong> persistentPlayers = new();

    private void Awake() => Instance = this;

    private void Start()
    {
        hostButton?.onClick.AddListener(() => TryStart(NetworkManager.Singleton.StartHost, "Host"));
        joinButton?.onClick.AddListener(() => TryStart(NetworkManager.Singleton.StartClient, "Client"));

        NetworkManager.Singleton.OnClientDisconnectCallback += clientId =>
            Debug.LogError($"Player {clientId} disconnected unexpectedly!");

        NetworkManager.Singleton.OnTransportFailure += () =>
            Debug.LogError("Transport Failure! Check UnityTransport configuration.");
    }

    private void TryStart(System.Func<bool> startFunc, string role)
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            Debug.LogError("Already running as server or client.");
            return;
        }

        bool started = startFunc.Invoke();
        Debug.Log($"{role} started: {started}");
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        var player = FindPlayerByClientId(clientId);
        if (player == null || string.IsNullOrEmpty(player.PlayerUniqueID.Value.ToString()))
        {
            Debug.Log($"Player {clientId} connected, waiting for ID...");
            return;
        }

        HandlePlayerIDSet(clientId, player.PlayerUniqueID.Value.ToString());
    }

    public void HandlePlayerIDSet(ulong clientId, string uniqueId)
    {
        if (persistentPlayers.ContainsKey(uniqueId))
        {
            Debug.Log($"Player {clientId} (ID: {uniqueId}) reconnected.");
        }
        else
        {
            persistentPlayers[uniqueId] = clientId;
            Debug.Log($"Player {clientId} (ID: {uniqueId}) connected for the first time.");
        }

        SendBoardToReconnectingClient(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[Server] Player {clientId} disconnected.");
    }

    private NetworkPlayer FindPlayerByClientId(ulong clientId)
    {
        foreach (var np in FindObjectsOfType<NetworkPlayer>())
            if (np.OwnerClientId == clientId)
                return np;

        return null;
    }

    private void SendBoardToReconnectingClient(ulong clientId)
    {
        string serialized = GameManager.Instance.SerializeGame();
        SyncBoardToOneClientRpc(serialized, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        });
    }

    [ClientRpc]
    private void SyncBoardToOneClientRpc(string serializedBoard, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("[ClientRpc] Syncing board for client...");
        GameManager.Instance.LoadGame(serializedBoard, true);
        BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(GameManager.Instance.SideToMove);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(Vector2Int from, Vector2Int to, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        var player = FindPlayerByClientId(clientId);
        if (player == null || !player.IsMyTurn())
        {
            ResetPieceClientRpc(from, clientId);
            return;
        }

        var moveValid = GameManager.Instance.game.TryGetLegalMove(new Square(from.x, from.y), new Square(to.x, to.y), out var move);
        if (!moveValid || !GameManager.Instance.TryExecuteMove(move))
        {
            ResetPieceClientRpc(from, clientId);
            return;
        }

        TurnManager.Instance.EndTurnServerRpc();
        UpdateBoardStateClientRpc(GameManager.Instance.SideToMove);
        UpdateBoardClientRpc(from, to);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResignServerRpc(ulong resigningClientId)
    {
        var player = FindPlayerByClientId(resigningClientId);
        if (player == null) return;

        bool isWhite = player.IsWhite.Value;
        AnnounceOutcomeServerRpc($"{(isWhite ? "Black" : "White")} wins by resignation! ({(isWhite ? "White" : "Black")} resigned)");
    }

    [ServerRpc(RequireOwnership = false)]
    public void AnnounceOutcomeServerRpc(string message) => AnnounceOutcomeClientRpc(message);

    [ClientRpc]
    private void AnnounceOutcomeClientRpc(string message)
    {
        Debug.Log($"[ClientRpc] {message}");
        UIManager.Instance.ShowOutcomeText(message);
        UIManager.Instance.DisableResignButton();
        BoardManager.Instance.SetActiveAllPieces(false);
    }

    [ClientRpc]
    private void UpdateBoardClientRpc(Vector2Int from, Vector2Int to)
    {
        BoardManager.Instance.TryDestroyVisualPiece(new Square(to.x, to.y));
        var pieceGO = BoardManager.Instance.GetPieceGOAtPosition(new Square(from.x, from.y));
        if (pieceGO != null)
        {
            pieceGO.transform.parent = BoardManager.Instance.GetSquareGOByPosition(new Square(to.x, to.y)).transform;
            pieceGO.transform.localPosition = Vector3.zero;
        }
    }

    [ClientRpc]
    private void UpdateBoardStateClientRpc(Side sideToMove) =>
        BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(sideToMove);

    [ClientRpc]
    private void ResetPieceClientRpc(Vector2Int from, ulong targetClientId, ClientRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        var pieceGO = BoardManager.Instance.GetPieceGOAtPosition(new Square(from.x, from.y));
        if (pieceGO != null)
        {
            pieceGO.transform.position = pieceGO.transform.parent.position;
        }
    }
}

