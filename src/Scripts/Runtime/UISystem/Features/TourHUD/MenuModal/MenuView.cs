using System;
using UnityEngine.UIElements;

/// <summary>
/// View for the menu modal during the tour
/// </summary>
public sealed class MenuView : IOverlayView
{
    public VisualElement Root { get; }

    public event Action OnClose;
    public event Action OnRestart;
    public event Action OnHelp;
    public event Action OnSettings;
    public event Action OnReturnHome;

    public event Action Hidden;
    VisualElement container,
        closeBtn,
        restart,
        help,
        settings,
        returnBtn;

    bool isOpen;
    EventCallback<ClickEvent> cbClose,
        cbRestart,
        cbHelp,
        cbSettings,
        cbReturn;

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuView"/> class.
    /// </summary>
    /// <param name="root">The root visual element.</param>
    public MenuView(VisualElement root)
    {
        Root = root;
    }

    /// <summary>
    /// Binds the view to the given UIDocument.
    /// </summary>
    /// <param name="doc">The UIDocument to bind to.</param>
    public void Bind(UIDocument doc)
    {
        container = Root.Q<VisualElement>("Menu") ?? Root;
        closeBtn = Root.Q<VisualElement>("CloseButton");
        restart = Root.Q<VisualElement>("Restart");
        help = Root.Q<VisualElement>("Help");
        settings = Root.Q<VisualElement>("Settings");
        returnBtn = Root.Q<VisualElement>("Return");

        UIPickingUtils.ConfigureTreePickingMode(Root, PickingMode.Ignore);
        UIPickingUtils.ConfigureTreePickingMode(container, PickingMode.Position);

        cbClose = _ => OnClose?.Invoke();
        cbRestart = _ => OnRestart?.Invoke();
        cbHelp = _ => OnHelp?.Invoke();
        cbSettings = _ => OnSettings?.Invoke();
        cbReturn = _ => OnReturnHome?.Invoke();

        closeBtn?.RegisterCallback(cbClose);
        restart?.RegisterCallback(cbRestart);
        help?.RegisterCallback(cbHelp);
        settings?.RegisterCallback(cbSettings);
        returnBtn?.RegisterCallback(cbReturn);

        container.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);

        container.RemoveFromClassList("is-open");
        Root.style.display = DisplayStyle.None;
        isOpen = false;
    }

    /// <summary>
    /// Unbinds the view from the UIDocument.
    /// </summary>
    public void Unbind()
    {
        closeBtn?.UnregisterCallback(cbClose);
        restart?.UnregisterCallback(cbRestart);
        help?.UnregisterCallback(cbHelp);
        settings?.UnregisterCallback(cbSettings);
        returnBtn?.UnregisterCallback(cbReturn);
        container?.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);
    }

    /// <summary>
    /// Shows the menu modal.
    /// </summary>
    public void Show()
    {
        if (isOpen)
            return;

        Root.style.display = DisplayStyle.Flex;
        container.RemoveFromClassList("is-open");

        void AfterLayout(GeometryChangedEvent _)
        {
            container.UnregisterCallback<GeometryChangedEvent>(AfterLayout);
            container
                .schedule.Execute(() =>
                {
                    container.AddToClassList("is-open");
                    isOpen = true;
                })
                .StartingIn(0);
        }
        container.RegisterCallback<GeometryChangedEvent>(AfterLayout);
    }

    /// <summary>
    /// Hides the menu modal.
    /// </summary>
    public void Hide()
    {
        if (!isOpen && Root.style.display == DisplayStyle.None)
        {
            Hidden?.Invoke();
            return;
        }
        container.RemoveFromClassList("is-open");
        isOpen = false;
    }

    /// <summary>
    /// Handles the transition end event for the menu modal.
    /// </summary>
    /// <param name="e">The transition end event.</param>
    void OnTransitionEnd(TransitionEndEvent e)
    {
        if (e.target != container)
            return;

        bool relevant = false;
        foreach (var n in e.stylePropertyNames)
        {
            var prop = n.ToString();
            if (prop == "translate" || prop == "opacity")
            {
                relevant = true;
                break;
            }
        }
        if (!relevant)
            return;

        if (!isOpen)
        {
            Root.style.display = DisplayStyle.None;
            Hidden?.Invoke();
        }
    }
}
