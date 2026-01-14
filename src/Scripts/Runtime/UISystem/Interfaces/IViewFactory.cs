/// <summary>
/// Enumeration of screen states in the UI system.
/// </summary>
public enum ScreenState
{
    Onboarding,
    Home,
    Minigames,
    TourHUD,
}

/// <summary>
/// Enumeration of overlay types in the UI system.
/// </summary>
public enum OverlayType
{
    InfoModal,
    NoticePopup,
    ActionPopup,
    Menu,
    Settings,
}

/// <summary>
/// Interface for a factory that creates views.
/// </summary>
public interface IViewFactory
{
    IScreenView CreateScreen(ScreenState s);
    IOverlayView CreateOverlay(OverlayType t);
}
