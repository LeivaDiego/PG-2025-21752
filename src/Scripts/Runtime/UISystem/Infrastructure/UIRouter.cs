using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages navigation between different UI screens and overlays.
/// </summary>
public sealed class UIRouter
{
    private readonly VisualElement baseLayer;
    private readonly VisualElement modalLayer;
    private readonly VisualElement popupLayer;
    private readonly VisualElement menuLayer;
    private readonly VisualElement settingsLayer;
    private readonly IViewFactory factory;
    private readonly UIDocument doc;

    // Current active screen and its view
    public ScreenState CurrentScreen { get; private set; }
    public IScreenView CurrentScreenView { get; private set; }

    /// <summary>
    /// Event invoked when the current screen changes.
    /// </summary>
    public event System.Action<IScreenView> ScreenChanged;

    /// <summary>
    /// Active overlays mapped by their type.
    /// </summary>
    private readonly Dictionary<OverlayType, IOverlayView> overlays = new();

    // Counters to track number of overlays per layer
    private int modalCount,
        popupCount,
        menuCount,
        settingsCount;

    /// <summary>
    /// Constructs a UIRouter with specified layers and view factory.
    /// </summary>
    /// <param name="baseLayer">The base layer for screens.</param>
    /// <param name="modalLayer">The layer for modal overlays.</param>
    /// <param name="popupLayer">The layer for popup overlays.</param>
    /// <param name="menuLayer">The layer for menu overlays.</param>
    /// <param name="settingsLayer">The layer for settings overlays.</param>
    /// <param name="factory">The view factory for creating views.</param>
    /// <param name="doc">The UIDocument associated with the UI.</param>
    public UIRouter(
        VisualElement baseLayer,
        VisualElement modalLayer,
        VisualElement popupLayer,
        VisualElement menuLayer,
        VisualElement settingsLayer,
        IViewFactory factory,
        UIDocument doc
    )
    {
        this.baseLayer = baseLayer;
        this.modalLayer = modalLayer;
        this.popupLayer = popupLayer;
        this.menuLayer = menuLayer;
        this.settingsLayer = settingsLayer;
        this.factory = factory;
        this.doc = doc;
    }

    /// <summary>
    /// Shows the specified screen, hiding all overlays.
    /// </summary>
    /// <param name="s">The screen state to show.</param>
    public void ShowScreen(ScreenState s)
    {
        // Hide all overlays when switching screens
        HideAllOverlays();

        // Unbind and clear current screen
        CurrentScreenView?.Unbind();
        baseLayer.Clear();

        // Create and bind new screen view
        var view = factory.CreateScreen(s);
        if (view == null || view.Root == null)
        {
            Debug.LogError($"[UIRouter] Failed to create screen {s}");
            return;
        }

        // Add new screen to base layer
        baseLayer.Add(view.Root);
        view.Bind(doc);
        // Update current screen state and view
        CurrentScreen = s;
        CurrentScreenView = view;
        ScreenChanged?.Invoke(view);
    }

    /// <summary>
    /// Shows the specified overlay.
    /// </summary>
    /// <param name="t">The type of overlay to show.</param>
    /// <returns>The created overlay view, or null if creation failed.</returns>
    public IOverlayView ShowOverlay(OverlayType t)
    {
        // If overlay already exists, return it
        if (overlays.TryGetValue(t, out var existing))
            return existing;

        // Create new overlay view
        var v = factory.CreateOverlay(t);
        if (v == null || v.Root == null)
        {
            Debug.LogError($"[UIRouter] Failed to create overlay {t}");
            return null;
        }

        // Resolve the appropriate layer for the overlay
        var layer = ResolveLayer(t);
        if (layer == null)
        {
            Debug.LogError($"[UIRouter] No layer for overlay {t}");
            return null;
        }

        // Show the layer and add the overlay view
        layer.style.display = DisplayStyle.Flex;
        IncrementLayerCount(t);

        // Add overlay to the layer
        layer.Add(v.Root);
        v.Bind(doc);
        overlays[t] = v;
        return v;
    }

    /// <summary>
    /// Hides all active overlays.
    /// </summary>
    public void HideAllOverlays()
    {
        // Create a list of keys to avoid modifying the collection during iteration
        var keys = new List<OverlayType>(overlays.Keys);
        // Hide each overlay
        foreach (var key in keys)
        {
            HideOverlay(key);
        }
        // Ensure all layers are hidden
        modalLayer.style.display = DisplayStyle.None;
        popupLayer.style.display = DisplayStyle.None;
        menuLayer.style.display = DisplayStyle.None;
        settingsLayer.style.display = DisplayStyle.None;
        modalCount = 0;
        popupCount = 0;
        menuCount = 0;
        settingsCount = 0;
    }

    /// <summary>
    /// Hides the specified overlay.
    /// </summary>
    /// <param name="t">The type of overlay to hide.</param>
    public void HideOverlay(OverlayType t)
    {
        // If overlay does not exist, return
        if (!overlays.TryGetValue(t, out var v))
            return;
        // Unbind and remove overlay from its layer
        v.Unbind();
        v.Root.RemoveFromHierarchy();
        overlays.Remove(t);
        // Decrement the layer count and hide layer if necessary
        DecrementLayerCount(t);
    }

    /// <summary>
    /// Gets the overlay of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the overlay view.</typeparam>
    /// <param name="t">The type of overlay to get.</param>
    /// <returns>The overlay view of the specified type, or null if not found.</returns>
    public T GetOverlay<T>(OverlayType t)
        where T : class, IOverlayView
    {
        return overlays.TryGetValue(t, out var v) ? v as T : null;
    }

    /// <summary>
    /// Resolves the appropriate layer for the specified overlay type.
    /// </summary>
    /// <param name="t">The type of overlay.</param>
    /// <returns>The VisualElement representing the layer, or null if not found.</returns>
    private VisualElement ResolveLayer(OverlayType t) =>
        t switch
        {
            OverlayType.InfoModal => modalLayer,
            OverlayType.NoticePopup => popupLayer,
            OverlayType.ActionPopup => popupLayer,
            OverlayType.Menu => menuLayer,
            OverlayType.Settings => settingsLayer,
            _ => null,
        };

    /// <summary>
    /// Increments the count of overlays for the specified layer.
    /// </summary>
    /// <param name="t">The type of overlay.</param>
    private void IncrementLayerCount(OverlayType t)
    {
        switch (t)
        {
            case OverlayType.InfoModal:
                modalCount++;
                break;
            case OverlayType.Settings:
                settingsCount++;
                break;
            case OverlayType.NoticePopup:
            case OverlayType.ActionPopup:
                popupCount++;
                break;
            case OverlayType.Menu:
                menuCount++;
                break;
        }
    }

    /// <summary>
    /// Decrements the count of overlays for the specified layer and hides the layer if count reaches
    /// zero.
    /// </summary>
    private void DecrementLayerCount(OverlayType t)
    {
        switch (t)
        {
            case OverlayType.InfoModal:
                modalCount = Mathf.Max(0, modalCount - 1);
                if (modalCount == 0)
                    modalLayer.style.display = DisplayStyle.None;
                break;
            case OverlayType.Settings:
                settingsCount = Mathf.Max(0, settingsCount - 1);
                if (settingsCount == 0)
                    settingsLayer.style.display = DisplayStyle.None;
                break;

            case OverlayType.NoticePopup:
            case OverlayType.ActionPopup:
                popupCount = Mathf.Max(0, popupCount - 1);
                if (popupCount == 0)
                    popupLayer.style.display = DisplayStyle.None;
                break;

            case OverlayType.Menu:
                menuCount = Mathf.Max(0, menuCount - 1);
                if (menuCount == 0)
                    menuLayer.style.display = DisplayStyle.None;
                break;
        }
    }
}
