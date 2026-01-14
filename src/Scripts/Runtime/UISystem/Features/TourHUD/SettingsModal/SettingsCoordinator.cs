using UnityEngine;

/// <summary>
/// Coordinator for the settings modal during the tour
/// </summary>
public sealed class SettingsCoordinator
{
    private readonly UIRouter router;
    private readonly AudioAtlas audioAtlas;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsCoordinator"/> class.
    /// </summary>
    /// <param name="r">The UI router.</param>
    /// <param name="aa">The audio atlas.</param>
    public SettingsCoordinator(UIRouter r, AudioAtlas aa)
    {
        router = r;
        audioAtlas = aa;
    }

    /// <summary>
    /// Shows the settings modal.
    /// </summary>
    public void Show()
    {
        if (router.ShowOverlay(OverlayType.Settings) is not SettingsView v)
            return;

        int volume = Mathf.Clamp(AppPrefs.LoadVolume(), 0, 100);
        int fontPx = NormalizeFontPx(AppPrefs.LoadFontPx());

        v.SetSlider(volume);
        v.SetSelectedFontPx(fontPx);
        v.SetVolumeIcon(LevelFor(volume));
        ApplyVolume(volume);
        ApplyFontPx(fontPx);

        /// <summary>
        /// Handles the close request event.
        /// </summary>
        void OnClose()
        {
            v.CloseRequested -= OnClose;
            v.Hidden -= OnHidden; // avoid dupes
            v.Hidden += OnHidden;
            v.Hide(); // animate out
        }

        /// <summary>
        /// Handles the hidden event.
        /// </summary>
        void OnHidden()
        {
            v.Hidden -= OnHidden;
            router.HideOverlay(OverlayType.Settings); // remove after animation
        }

        /// <summary>
        /// Handles the volume change event.
        /// </summary>
        /// <param name="val">The new volume value.</param>
        void OnVol(int val)
        {
            val = Mathf.Clamp(val, 0, 100);
            AppPrefs.SaveVolume(val);
            ApplyVolume(val);
            v.SetVolumeIcon(LevelFor(val));
        }

        /// <summary>
        /// Handles the volume change committed event.
        /// </summary>
        void OnVolCommit()
        {
            if (audioAtlas && audioAtlas.settingsPreview)
            {
                AudioDirector.Instance.Play(audioAtlas.settingsPreview, 0f, 0.1f);
            }
        }

        /// <summary>
        /// Handles the font size change event.
        /// </summary>
        /// <param name="px">The new font size in pixels.</param>
        void OnFont(int px)
        {
            px = NormalizeFontPx(px);
            AppPrefs.SaveFontPx(px);
            v.SetSelectedFontPx(px);
            ApplyFontPx(px);
        }

        v.CloseRequested += OnClose;
        v.VolumeChanged += OnVol;
        v.FontPxPicked += OnFont;
        v.VolumeChangeCommitted += OnVolCommit;
    }

    /// <summary>
    /// Determines the volume level based on the given value.
    /// </summary>
    /// <param name="v">The volume value.</param>
    /// <returns>The corresponding volume level.</returns>
    static VolumeLevel LevelFor(int v)
    {
        int d = Mathf.RoundToInt(v);
        if (d == 0)
            return VolumeLevel.Mute;
        if (d <= 35)
            return VolumeLevel.Low;
        if (d <= 70)
            return VolumeLevel.Med;
        return VolumeLevel.High;
    }

    /// <summary>
    /// Normalizes the font size in pixels to allowed values.
    /// </summary>
    /// <param name="px">The font size in pixels.</param>
    /// <returns>The normalized font size in pixels.</returns>
    static int NormalizeFontPx(int px) => (px == 80 || px == 120) ? px : 100;

    /// <summary>
    /// Applies the volume setting.
    /// </summary>
    /// <param name="v">The volume value.</param>
    static void ApplyVolume(int v)
    {
        AppPrefs.SaveVolume(v);
        AudioDirector.ApplyVolumeFromPrefs();
    }

    /// <summary>
    /// Applies the font size setting.
    /// </summary>
    /// <param name="px">The font size in pixels.</param>
    static void ApplyFontPx(int px)
    {
        var boot = Object.FindFirstObjectByType<UIBootstrap>();
        if (boot != null)
        {
            boot.ApplyGlobalFontPx(px);
        }
    }
}
