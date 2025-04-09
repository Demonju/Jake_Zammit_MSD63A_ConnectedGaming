using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections;

public class NetworkPingManager : NetworkBehaviour
{
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private float pingInterval = 2f;

    private void Start()
    {
        if (IsClient)
            StartCoroutine(PingLoop());
    }

    private IEnumerator PingLoop()
    {
        while (true)
        {
            float clientTime = Time.realtimeSinceStartup;
            PingServerRpc(clientTime);
            yield return new WaitForSeconds(pingInterval);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PingServerRpc(float clientTime, ServerRpcParams rpcParams = default)
    {
        float serverTime = Time.realtimeSinceStartup;
        float halfTrip = serverTime - clientTime;
        PongClientRpc(halfTrip, rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void PongClientRpc(float halfTrip, ulong targetClientId, ClientRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        float ms = halfTrip * 2f * 1000f;
        if (pingText != null)
            pingText.text = $"Ping: {ms:0.0} ms";
        else
            Debug.LogWarning("[Client] pingText is null.");
    }
}
