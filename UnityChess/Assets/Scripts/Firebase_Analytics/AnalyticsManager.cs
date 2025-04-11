using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityChess;
using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(this);
    }

    /// <summary>Universal method to send custom analytics events</summary>
    private void SendAnalyticsEvent(string eventName, Dictionary<string, object> parameters)
    {
        if (!GameInitializer.AnalyticsReady) return;

        try
        {
            // Try current recommended method first
            if (TrySendCustomEvent(eventName, parameters)) return;

            // Fallback to legacy method if modern API fails
            SendLegacyAnalyticsEvent(eventName, parameters);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Analytics] Failed to track {eventName}: {ex.Message}");
        }
    }

    private bool TrySendCustomEvent(string eventName, Dictionary<string, object> parameters)
    {
        try
        {
            // Method 1: Current standard (2023+)
            var analyticsService = AnalyticsService.Instance;
            var method = analyticsService.GetType().GetMethod("RecordEvent");
            if (method != null)
            {
                method.Invoke(analyticsService, new object[] { eventName, parameters });
                Debug.Log($"[Analytics] Tracked {eventName} (modern API)");
                return true;
            }

            // Method 2: Alternative modern approach
            method = analyticsService.GetType().GetMethod("SendCustomEvent");
            if (method != null)
            {
                method.Invoke(analyticsService, new object[] { eventName, parameters });
                Debug.Log($"[Analytics] Tracked {eventName} (alternative API)");
                return true;
            }
        }
        catch { /* Silently fail to try next method */ }
        return false;
    }

    private void SendLegacyAnalyticsEvent(string eventName, Dictionary<string, object> parameters)
    {
        try
        {
            // Fallback to legacy Unity Analytics
            UnityEngine.Analytics.Analytics.CustomEvent(eventName, parameters);
            Debug.Log($"[Analytics] Tracked {eventName} (legacy API)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Analytics] All tracking methods failed for {eventName}: {ex.Message}");
        }
    }

    // Your specific event methods (unchanged except for implementation)
    public void TrackSkinPurchase(string skinName, int price) => SendAnalyticsEvent("skin_purchased", new Dictionary<string, object>
    {
        { "skin_name", skinName },
        { "price", price },
        { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
        { "player_id", PlayerPrefs.GetString("LocalUserId", "unknown") },
        { "credits_remaining", CreditsManager.Instance.credits }
    });

    public void TrackMatchStart(bool isOnline, Side playerSide) => SendAnalyticsEvent("match_started", new Dictionary<string, object>
    {
        { "match_id", Guid.NewGuid().ToString() },
        { "is_online", isOnline },
        { "player_side", playerSide.ToString() },
        { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
        { "player_id", PlayerPrefs.GetString("LocalUserId", "unknown") }
    });

    public void TrackMatchEnd(bool isOnline, Side playerSide, string outcome, float durationSeconds) => SendAnalyticsEvent("match_ended", new Dictionary<string, object>
    {
        { "is_online", isOnline },
        { "player_side", playerSide.ToString() },
        { "outcome", outcome },
        { "duration_seconds", Mathf.RoundToInt(durationSeconds) },
        { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
        { "player_id", PlayerPrefs.GetString("LocalUserId", "unknown") }
    });
}
