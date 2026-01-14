using UnityEngine.UIElements;

/// <summary>
/// Interface for views in the UI system.
/// </summary>
public interface IView
{
    VisualElement Root { get; }
    void Bind(UIDocument doc);
    void Unbind();
}

/// <summary>
/// Interface for screen views.
/// </summary>
public interface IScreenView : IView { }

/// <summary>
/// Interface for overlay views.
/// </summary>
public interface IOverlayView : IView { }
