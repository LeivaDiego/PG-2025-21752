#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Developer menu for AR Tour related tools.
/// </summary>
public static class ARDevMenu
{
    /// <summary>
    /// Helper to find the TourBinder in the scene.
    /// </summary>
    /// <returns>TourBinder instance if found; otherwise, null.</returns>
    static TourBinder B() => Object.FindFirstObjectByType<TourBinder>(FindObjectsInactive.Include);

    /// <summary>
    /// Helper to find the FloorManager in the scene.
    /// </summary>
    /// <returns>FloorManager instance if found; otherwise, null.</returns>
    static FloorManager FM() =>
        Object.FindFirstObjectByType<FloorManager>(FindObjectsInactive.Include);

    /// <summary>
    /// Resets the onboarding progress.
    /// </summary>
    [MenuItem("AR Tour Dev Tools/Onboarding/Reset State")]
    public static void ResetOnboarding()
    {
        // Reset onboarding progress
        OnboardingGate.Reset();
        Debug.Log("[Dev Tools] Onboarding progress reset.");
    }

    /// <summary>
    /// Resets all application preferences.
    /// </summary>
    [MenuItem("AR Tour Dev Tools/AppPrefs/Reset Preferences")]
    public static void ResetPreferences()
    {
        // Clear all application preferences
        AppPrefs.ClearAll();
        Debug.Log("[Dev Tools] Preferences reset.");
    }

    /// <summary>
    /// Simulates establishing a connection.
    /// </summary>
    [MenuItem("AR Tour Dev Tools/Simulation/Connect")]
    public static void Connect()
    {
        // Simulate establishing a connection
        if (B() == null)
        {
            Debug.LogWarning("[Dev Tools] No TourBinder found in the scene.");
            return;
        }
        // Set connection status to true
        B().SetConnection(true);
        Debug.Log("[Dev Tools] Simulated connection established.");
    }

    /// <summary>
    /// Simulates disconnecting the connection.
    /// </summary>
    [MenuItem("AR Tour Dev Tools/Simulation/Disconnect")]
    public static void Disconnect()
    {
        // Simulate disconnecting the connection
        if (B() == null)
        {
            Debug.LogWarning("[Dev Tools] No TourBinder found in the scene.");
            return;
        }
        // Set connection status to false
        B().SetConnection(false);
        Debug.Log("[Dev Tools] Simulated connection disconnected.");
    }

    /// <summary>
    /// Marks the user as ready for floor transition.
    /// </summary>
    [MenuItem("AR Tour Dev Tools/Floor/User Ready (R)")]
    public static void Ready()
    {
        // Mark the user as ready for floor transition
        if (FM() == null)
        {
            Debug.LogWarning("[Dev Tools] No FloorManager found in the scene.");
            return;
        }
        // Mark the user as ready for floor transition
        FM().UserReady();
        Debug.Log("[Dev Tools] User marked as ready for floor transition.");
    }

    /// <summary>
    /// Moves to the next floor.
    /// </summary>
    [MenuItem("AR Tour Dev Tools/Floor/Next (N)")]
    public static void Next()
    {
        if (FM() == null)
        {
            Debug.LogWarning("[Dev Tools] No FloorManager found in the scene.");
            return;
        }
        // Move to the next floor
        FM().Next();
    }
}
#endif
