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

    // Dictionary to track unique player IDs and their corresponding client IDs
    private Dictionary<string, ulong> persistentPlayers = new();

    private void Awake() => Instance = this;

    private void Start()
    {
        // Assign button click listeners
        hostButton?.onClick.AddListener(() => TryStart(NetworkManager.Singleton.StartHost, "Host"));
        joinButton?.onClick.AddListener(() => TryStart(NetworkManager.Singleton.StartClient, "Client"));

        // Register disconnect and transport failure handlers
        NetworkManager.Singleton.OnClientDisconnectCallback += clientId =>
            Debug.LogError($"Player {clientId} disconnected unexpectedly!");

        NetworkManager.Singleton.OnTransportFailure += () =>
            Debug.LogError("Transport Failure! Check UnityTransport configuration.");
    }

    // Attempt to start as host or client
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
        // Only the server should handle connection callbacks
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    // Called when a client connects to the server
    private void OnClientConnected(ulong clientId)
    {
        var player = FindPlayerByClientId(clientId);
        if (player == null || string.IsNullOrEmpty(player.PlayerUniqueID.Value.ToString()))
        {
            Debug.Log($"Player {clientId} connected, waiting for ID...");
            return;
        }

        // Handle setting the player's unique ID
        HandlePlayerIDSet(clientId, player.PlayerUniqueID.Value.ToString());
    }

    // Set or recognize the player by their unique ID
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

        // Re-sync the game board state for reconnecting players
        SendBoardToReconnectingClient(clientId);
    }

    // Called when a client disconnects
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[Server] Player {clientId} disconnected.");
    }

    // Find the player associated with a specific client ID
    private NetworkPlayer FindPlayerByClientId(ulong clientId)
    {
        foreach (var np in FindObjectsOfType<NetworkPlayer>())
            if (np.OwnerClientId == clientId)
                return np;

        return null;
    }

    // Send current game board state to a specific client
    private void SendBoardToReconnectingClient(ulong clientId)
    {
        string serialized = GameManager.Instance.SerializeGame();
        SyncBoardToOneClientRpc(serialized, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        });
    }

    // Sync the game board on the client side
    [ClientRpc]
    private void SyncBoardToOneClientRpc(string serializedBoard, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("[ClientRpc] Syncing board for client...");
        GameManager.Instance.LoadGame(serializedBoard, true);
        BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(GameManager.Instance.SideToMove);
    }

    // Server-side move request
    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(Vector2Int from, Vector2Int to, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        var player = FindPlayerByClientId(clientId);

        // Validate player's turn
        if (player == null || !player.IsMyTurn())
        {
            ResetPieceClientRpc(from, clientId);
            return;
        }

        // Validate and execute the move
        var moveValid = GameManager.Instance.game.TryGetLegalMove(new Square(from.x, from.y), new Square(to.x, to.y), out var move);
        if (!moveValid || !GameManager.Instance.TryExecuteMove(move))
        {
            ResetPieceClientRpc(from, clientId);
            return;
        }

        // End turn and update board for all clients
        TurnManager.Instance.EndTurnServerRpc();
        UpdateBoardStateClientRpc(GameManager.Instance.SideToMove);
        UpdateBoardClientRpc(from, to);
    }

    // Server-side resignation logic
    [ServerRpc(RequireOwnership = false)]
    public void ResignServerRpc(ulong resigningClientId)
    {
        var player = FindPlayerByClientId(resigningClientId);
        if (player == null) return;

        bool isWhite = player.IsWhite.Value;
        string outcome = $"{(isWhite ? "Black" : "White")} wins by resignation";

        // Log analytics for the match
        float durationSeconds = Time.time - GameManager.Instance.gameStartTime;
        AnalyticsManager.Instance.TrackMatchEnd(true, isWhite ? Side.White : Side.Black,
            "resignation", durationSeconds);

        // Inform all players of the outcome
        AnnounceOutcomeServerRpc(outcome);
    }

    // Server broadcasts the outcome to all clients
    [ServerRpc(RequireOwnership = false)]
    public void AnnounceOutcomeServerRpc(string message) => AnnounceOutcomeClientRpc(message);

    // Client-side handling of match outcome
    [ClientRpc]
    private void AnnounceOutcomeClientRpc(string message)
    {
        Debug.Log($"[ClientRpc] {message}");
        UIManager.Instance.ShowOutcomeText(message);
        UIManager.Instance.DisableResignButton();
        BoardManager.Instance.SetActiveAllPieces(false);
    }

    // Move visual representation of the piece on clients
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

    // Enable only the pieces for the current player's turn
    [ClientRpc]
    private void UpdateBoardStateClientRpc(Side sideToMove) =>
        BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(sideToMove);

    // Reset the piece if the move was invalid
    [ClientRpc]
    private void ResetPieceClientRpc(Vector2Int from, ulong targetClientId, ClientRpcParams rpcParams = default)
    {
        // Ensure this only runs on the intended client
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        var pieceGO = BoardManager.Instance.GetPieceGOAtPosition(new Square(from.x, from.y));
        if (pieceGO != null)
        {
            pieceGO.transform.position = pieceGO.transform.parent.position;
        }
    }
}


