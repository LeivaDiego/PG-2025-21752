using UnityEngine;

/// <summary>
/// Utility class for managing application preferences.
/// </summary>
public static class AppPrefs
{
    // Preference keys
    const string VolumeKey = "app.volume";
    const string FontPxKey = "app.fontpx";
    const string FirstRunKey = "app.firstRun";

    /// <summary>
    /// Loads the saved volume level.
    /// </summary>
    /// <returns>The saved volume level, or 50 if not set.</returns>
    public static int LoadVolume() => PlayerPrefs.GetInt(VolumeKey, 50);

    /// <summary>
    /// Loads the saved font size in pixels.
    /// </summary>
    /// <returns>The saved font size in pixels, or 100 if not set.</returns>
    public static int LoadFontPx() => PlayerPrefs.GetInt(FontPxKey, 100);

    /// <summary>
    /// Checks if this is the first run of the application.
    /// </summary>
    /// <returns>True if this is the first run; otherwise, false.</returns>
    public static bool IsFirstRun()
    {
        // Default to 1 (true) if the key does not exist
        if (PlayerPrefs.GetInt(FirstRunKey, 1) == 1)
        {
            // Mark that the first run has occurred
            PlayerPrefs.SetInt(FirstRunKey, 0);
            // Save changes to persistent storage
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Saves the specified volume level.
    /// </summary>
    /// <param name="v">The volume level to save.</param>
    public static void SaveVolume(int v)
    {
        // Set and save the volume preference
        PlayerPrefs.SetInt(VolumeKey, v);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Saves the specified font size in pixels.
    /// </summary>
    /// <param name="px">The font size in pixels to save.</param>
    public static void SaveFontPx(int px)
    {
        // Set and save the font size preference
        PlayerPrefs.SetInt(FontPxKey, px);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Clears all saved application preferences.
    /// </summary>
    public static void ClearAll()
    {
        // Delete all relevant preference keys
        PlayerPrefs.DeleteKey(VolumeKey);
        PlayerPrefs.DeleteKey(FontPxKey);
        PlayerPrefs.DeleteKey(FirstRunKey);
        PlayerPrefs.Save();
    }
}
