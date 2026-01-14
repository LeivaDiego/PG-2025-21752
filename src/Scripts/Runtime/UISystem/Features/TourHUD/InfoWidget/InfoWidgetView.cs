using System;
using UnityEngine.UIElements;

/// <summary>
/// View for the info widget during the tour
/// </summary>
public sealed class InfoWidgetView : IOverlayView
{
    public VisualElement Root { get; }
    public event Action OnContinue;
    public event Action Hidden;
    VisualElement widget,
        areaImage,
        continueBtn;
    Label description;
    bool isOpen;

    /// <summary>
    /// Click event callback for the continue button.
    /// </summary>
    EventCallback<ClickEvent> continueClick;

    /// <summary>
    /// Initializes a new instance of the <see cref="InfoWidgetView"/> class.
    /// </summary>
    /// <param name="root">The root visual element of the info widget view.</param>
    public InfoWidgetView(VisualElement root)
    {
        Root = root;
    }

    /// <summary>
    /// Binds the view to the given UIDocument.
    /// </summary>
    /// <param name="doc">The UIDocument to bind to.</param>
    public void Bind(UIDocument doc)
    {
        widget = Root.Q<VisualElement>("InfoWidget");
        areaImage = Root.Q<VisualElement>("AreaImage");
        description = Root.Q<Label>("Description");
        continueBtn = Root.Q<VisualElement>("ContinueButton");

        UIPickingUtils.ConfigureTreePickingMode(Root, PickingMode.Ignore);

        UIPickingUtils.ConfigureTreePickingMode(widget, PickingMode.Position);

        continueClick = _ => OnContinue?.Invoke();
        continueBtn?.RegisterCallback(continueClick);

        widget.RegisterCallback<TransitionEndEvent>(OnAnyTransitionEnd);

        widget.RemoveFromClassList("is-open");
        Root.style.display = DisplayStyle.None;
        isOpen = false;
    }

    /// <summary>
    /// Unbinds the view from the current UIDocument.
    /// </summary>
    public void Unbind()
    {
        if (continueBtn != null && continueClick != null)
            continueBtn.UnregisterCallback(continueClick);
        widget?.UnregisterCallback<TransitionEndEvent>(OnAnyTransitionEnd);
    }

    /// <summary>
    /// Shows the info widget for the specified area.
    /// </summary>
    /// <param name="area">The area definition to display information for.</param>
    public void Show(AreaDefinition area)
    {
        if (isOpen)
            return;

        if (description != null)
            description.text = area ? area.AreaText : "";

        if (areaImage != null)
            areaImage.style.backgroundImage =
                area && area.AreaImage ? new StyleBackground(area.AreaImage) : StyleKeyword.Null;

        Root.style.display = DisplayStyle.Flex;

        widget.RemoveFromClassList("is-open");

        Root.schedule.Execute(() =>
            {
                widget.AddToClassList("is-open");
                isOpen = true;
            })
            .StartingIn(1);
    }

    /// <summary>
    /// Hides the info widget.
    /// </summary>
    public void Hide()
    {
        if (!isOpen && Root.style.display == DisplayStyle.None)
        {
            Hidden?.Invoke();
            return;
        }
        widget.RemoveFromClassList("is-open");
        isOpen = false;
    }

    /// <summary>
    /// Handles the transition end event for any transition on the widget.
    /// </summary>
    /// <param name="e">The transition end event data.</param>
    void OnAnyTransitionEnd(TransitionEndEvent e)
    {
        if (e.target != widget)
            return;

        bool relevant = false;
        foreach (var name in e.stylePropertyNames)
        {
            var prop = name.ToString();
            if (prop == "opacity" || prop == "translate")
            {
                relevant = true;
                break;
            }
        }
        if (!relevant)
            return;

        if (!isOpen)
            Root.style.display = DisplayStyle.None;
    }
}
