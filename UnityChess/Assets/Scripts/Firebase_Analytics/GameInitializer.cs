using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Analytics;
using UnityEngine;
using System.Threading.Tasks;
using System;

public class GameInitializer : MonoBehaviour
{
    public static bool AnalyticsReady { get; private set; } = false;

    private async void Awake()
    {
        try
        {
            await UnityServices.InitializeAsync();
            AnalyticsReady = true;
            Debug.Log("Unity Services + Analytics initialized.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to initialize Unity Services: " + ex.Message);
        }
    }
}

