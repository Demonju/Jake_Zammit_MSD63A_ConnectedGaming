using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChessBoardSync : NetworkBehaviour
{
    private NetworkVariable<ChessPieceState[]> boardState =
        new NetworkVariable<ChessPieceState[]>();

    public override void OnNetworkSpawn()
    {
        // Optional: Add listener for board state changes
        boardState.OnValueChanged += OnBoardStateChanged;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateBoardServerRpc(ChessPieceState[] newBoardState)
    {
        // Server-side validation of board state
        if (ValidateBoardState(newBoardState))
        {
            boardState.Value = newBoardState;
            BroadcastBoardStateClientRpc(newBoardState);
        }
    }

    [ClientRpc]
    private void BroadcastBoardStateClientRpc(ChessPieceState[] state)
    {
        // Update local board representation
        UnityEngine.Debug.Log($"Board state updated. Pieces: {state?.Length}");
    }

    private bool ValidateBoardState(ChessPieceState[] newState)
    {
        // Basic validation
        return newState != null && newState.Length <= 32; // Max pieces in chess
    }

    private void OnBoardStateChanged(ChessPieceState[] previousValue, ChessPieceState[] newValue)
    {
        UnityEngine.Debug.Log($"Board state changed. Previous pieces: {previousValue?.Length}, New pieces: {newValue?.Length}");
    }

    public override void OnDestroy()
    {
        // Cleanup event listener
        boardState.OnValueChanged -= OnBoardStateChanged;
        base.OnDestroy();
    }
}

// Simplified piece state representation
[Serializable]
public struct ChessPieceState : INetworkSerializable
{
    public int PieceType;
    public int PositionX;
    public int PositionY;
    public bool IsWhite;

    public Vector2Int Position
    {
        get => new Vector2Int(PositionX, PositionY);
        set
        {
            PositionX = value.x;
            PositionY = value.y;
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Serialize each field
        serializer.SerializeValue(ref PieceType);
        serializer.SerializeValue(ref PositionX);
        serializer.SerializeValue(ref PositionY);
        serializer.SerializeValue(ref IsWhite);
    }
}
