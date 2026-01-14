using System;
using UnityEngine.UIElements;

/// <summary>
/// View for the Home screen in the UI system.
/// </summary>
public sealed class HomeView : IScreenView
{
    public VisualElement Root { get; }
    public event Action OnExpress;
    public event Action OnComplete;
    public event Action OnMinigames;

    private VisualElement expressBtn;
    private VisualElement completeBtn;
    private VisualElement minigamesBtn;

    private EventCallback<ClickEvent> onExpressCb;
    private EventCallback<ClickEvent> onCompleteCb;
    private EventCallback<ClickEvent> onMinigamesCb;
    private bool bound;

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeView"/> class.
    /// </summary>
    /// <param name="root">The root visual element of the home view.</param>
    public HomeView(VisualElement root)
    {
        Root = root;
    }

    /// <summary>
    /// Binds the view to the given UIDocument.
    /// </summary>
    /// <param name="doc">The UIDocument to bind to.</param>
    public void Bind(UIDocument doc)
    {
        if (bound)
            return; // prevent double bind

        expressBtn = Root.Q<VisualElement>("ExpressBtn");
        completeBtn = Root.Q<VisualElement>("CompleteBtn");
        minigamesBtn = Root.Q<VisualElement>("MinigamesBtn");

        onExpressCb = _ => OnExpress?.Invoke();
        onCompleteCb = _ => OnComplete?.Invoke();
        onMinigamesCb = _ => OnMinigames?.Invoke();

        expressBtn?.RegisterCallback(onExpressCb);
        completeBtn?.RegisterCallback(onCompleteCb);
        minigamesBtn?.RegisterCallback(onMinigamesCb);

        bound = true;
    }

    /// <summary>
    /// Unbinds the view from the UIDocument.
    /// </summary>
    public void Unbind()
    {
        if (!bound)
            return;

        expressBtn?.UnregisterCallback(onExpressCb);
        completeBtn?.UnregisterCallback(onCompleteCb);
        minigamesBtn?.UnregisterCallback(onMinigamesCb);

        onExpressCb = null;
        onCompleteCb = null;
        onMinigamesCb = null;
        expressBtn = completeBtn = minigamesBtn = null;
        bound = false;
    }
}
