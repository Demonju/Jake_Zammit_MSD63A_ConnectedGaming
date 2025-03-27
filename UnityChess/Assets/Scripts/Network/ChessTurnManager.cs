using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;

public class ChessTurnManager : NetworkBehaviour
{
    private NetworkVariable<int> currentTurn = new NetworkVariable<int>(0);
    private NetworkVariable<bool> isWhiteTurn = new NetworkVariable<bool>(true);

    public override void OnNetworkSpawn()
    {
        // Optional: Add listeners for network variable changes
        isWhiteTurn.OnValueChanged += OnTurnChanged;
    }

    private void OnTurnChanged(bool previousValue, bool newValue)
    {
        UnityEngine.Debug.Log($"Turn changed from {(previousValue ? "White" : "Black")} to {(newValue ? "White" : "Black")}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(int fromX, int fromY, int toX, int toY)
    {
        // Server-side move validation
        if (!isWhiteTurn.Value)
        {
            UnityEngine.Debug.Log("Not White's turn");
            return;
        }

        // Validate move logic here
        bool moveValid = ValidateMove(fromX, fromY, toX, toY);

        if (moveValid)
        {
            // Update board state
            UpdateBoardStateClientRpc(fromX, fromY, toX, toY);

            // Switch turns
            isWhiteTurn.Value = !isWhiteTurn.Value;
        }
    }

    [ClientRpc]
    private void UpdateBoardStateClientRpc(int fromX, int fromY, int toX, int toY)
    {
        // Update local board representation
        UnityEngine.Debug.Log($"Move from ({fromX},{fromY}) to ({toX},{toY})");
    }

    private bool ValidateMove(int fromX, int fromY, int toX, int toY)
    {
        // Implement chess move validation
        UnityEngine.Debug.Log($"Validating move from ({fromX},{fromY}) to ({toX},{toY})");
        return true;
    }

    public override void OnDestroy()
    {
        // Cleanup event listeners
        isWhiteTurn.OnValueChanged -= OnTurnChanged;
        base.OnDestroy();
    }
}