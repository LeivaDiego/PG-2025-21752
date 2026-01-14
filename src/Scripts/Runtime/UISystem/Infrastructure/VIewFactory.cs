using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Factory for creating UI views.
/// </summary>
public sealed class ViewFactory : IViewFactory
{
    // Reference to the UI atlas and document
    private readonly UIAtlas atlas;
    private readonly UIDocument doc;

    /// <summary>
    /// Constructor for ViewFactory.
    /// </summary>
    /// <param name="doc">The UIDocument instance.</param>
    /// <param name="atlas">The UIAtlas containing UI assets.</param>
    public ViewFactory(UIDocument doc, UIAtlas atlas)
    {
        this.doc = doc;
        this.atlas = atlas;
    }

    /// <summary>
    /// Create a screen view based on the given screen state.
    /// </summary>
    /// <param name="s">The screen state.</param>
    /// <returns>The created screen view.</returns>
    public IScreenView CreateScreen(ScreenState s) =>
        s switch
        {
            ScreenState.Home => new HomeView(Clone(atlas.HomeUXML)),
            ScreenState.Minigames => new MinigamesView(Clone(atlas.MinigamesUXML)),
            ScreenState.Onboarding => new OnboardingView(Clone(atlas.OnboardingUXML)),
            ScreenState.TourHUD => new BaseHUDView(Clone(atlas.BaseHUDUXML)),
            _ => null,
        };

    /// <summary>
    /// Create an overlay view based on the given overlay type.
    /// </summary>
    /// <param name="t">The overlay type.</param>
    /// <returns>The created overlay view.</returns>
    public IOverlayView CreateOverlay(OverlayType t) =>
        t switch
        {
            OverlayType.InfoModal => new InfoWidgetView(Clone(atlas.InfoWidgetUXML)),
            OverlayType.NoticePopup => new NoticePopupView(Clone(atlas.NoticePopupUXML)),
            OverlayType.ActionPopup => new ActionPopupView(Clone(atlas.ActionPopupUXML)),
            OverlayType.Menu => new MenuView(Clone(atlas.MenuUXML)),
            OverlayType.Settings => new SettingsView(Clone(atlas.SettingsUXML)),
            _ => null,
        };

    /// <summary>
    /// Clone a VisualTreeAsset into a VisualElement.
    /// </summary>
    /// <param name="vta">The VisualTreeAsset to clone.</param>
    /// <returns>The cloned VisualElement.</returns>
    private static VisualElement Clone(VisualTreeAsset vta)
    {
        // Check if the VisualTreeAsset is valid
        if (!vta)
        {
            Debug.LogError("[ViewFactory] Missing VisualTreeAsset");
            return null;
        }
        // Clone the VisualTreeAsset and set its flex grow
        var ve = vta.CloneTree();
        ve.style.flexGrow = 1;
        return ve;
    }
}
