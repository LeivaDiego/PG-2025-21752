using System;
using UnityEngine;

/// <summary>
/// Manages the logic for displaying onboarding based on user interaction history.
/// </summary>
public static class OnboardingGate
{
    private const string SeenKey = "onboarding_seen_utcbin";
    private const string ForceKey = "onboarding_force_always";

    /// <summary>
    /// Determines if the onboarding should be shown based on the number of days since last seen.
    /// </summary>
    /// <param name="days">The number of days since the onboarding was last seen.</param>
    /// <returns>True if the onboarding should be shown; otherwise, false.</returns>
    public static bool ShouldShow(int days)
    {
        // If forced to always show, return true
        if (GetForceAlways())
            return true;

        // If days is zero or negative, show if never seen
        if (days <= 0)
            return !PlayerPrefs.HasKey(SeenKey);

        // Show if never seen or if enough days have passed since last seen
        if (!PlayerPrefs.HasKey(SeenKey))
            return true;

        // Calculate the last seen date and compare with the current date
        var last = DateTime.FromBinary(long.Parse(PlayerPrefs.GetString(SeenKey)));
        return (DateTime.UtcNow - last).TotalDays >= days;
    }

    /// <summary>
    /// Marks the onboarding as seen at the current UTC time.
    /// </summary>
    public static void MarkSeen()
    {
        PlayerPrefs.SetString(SeenKey, DateTime.UtcNow.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Sets whether the onboarding should always be shown.
    /// </summary>
    /// <param name="enabled">True to always show onboarding; false otherwise.</param>
    public static void SetForceAlways(bool enabled)
    {
        PlayerPrefs.SetInt(ForceKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Resets the onboarding seen state.
    /// </summary>
    public static void Reset()
    {
        PlayerPrefs.DeleteKey(SeenKey);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Gets whether the onboarding is forced to always show.
    /// </summary>
    /// <returns>True if onboarding is forced to always show; otherwise, false.</returns>
    public static bool GetForceAlways()
    {
        return PlayerPrefs.GetInt(ForceKey, 0) == 1;
    }

    /// <summary>
    /// Gets debug information about the last seen onboarding time.
    /// </summary>
    /// <returns>A string representing the last seen time in UTC or "never" if not seen.</returns>
    public static string DebugInfo()
    {
        return PlayerPrefs.HasKey(SeenKey)
            ? DateTime.FromBinary(long.Parse(PlayerPrefs.GetString(SeenKey))).ToString("u")
            : "never";
    }
}
