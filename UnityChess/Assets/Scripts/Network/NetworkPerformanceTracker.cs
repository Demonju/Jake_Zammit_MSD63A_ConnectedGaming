using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Diagnostics;

public class NetworkPerformanceTracker : NetworkBehaviour
{
    private Stopwatch latencyStopwatch = new Stopwatch();

    [ServerRpc]
    public void MeasurePingServerRpc()
    {
        latencyStopwatch.Restart();
        ReportLatencyClientRpc();
    }

    [ClientRpc]
    private void ReportLatencyClientRpc()
    {
        latencyStopwatch.Stop();
        long latencyMs = latencyStopwatch.ElapsedMilliseconds;

        UnityEngine.Debug.Log($"Network Latency: {latencyMs}ms");
    }
}
