using System;
using UnityEngine.UIElements;

/// <summary>
/// View for action popups during the tour
/// </summary>
public sealed class ActionPopupView : IOverlayView
{
    public VisualElement Root { get; }
    public event Action Hidden;

    VisualElement container,
        roundedImage,
        button;
    Label title,
        description,
        buttonText;
    Action click;
    bool isOpen;

    EventCallback<ClickEvent> btnCb;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionPopupView"/> class.
    /// </summary>
    /// <param name="root">The root visual element.</param>
    public ActionPopupView(VisualElement root)
    {
        Root = root;
    }

    /// <summary>
    /// Binds the view to the given UIDocument.
    /// </summary>
    /// <param name="doc">The UIDocument to bind to.</param>
    public void Bind(UIDocument doc)
    {
        container = Root.Q<VisualElement>("PopUP") ?? Root.Q<VisualElement>("PopUp");
        roundedImage = Root.Q<VisualElement>("RoundedImage");
        title = Root.Q<Label>("ActionTitle");
        description = Root.Q<Label>("ActionDescription");
        button = Root.Q<VisualElement>("ActionButton");
        buttonText = Root.Q<Label>("ButtonText");

        UIPickingUtils.ConfigureTreePickingMode(Root, PickingMode.Ignore);
        UIPickingUtils.ConfigureTreePickingMode(container, PickingMode.Position);

        btnCb = _ => click?.Invoke();
        button?.RegisterCallback(btnCb);

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
        if (button != null && btnCb != null)
            button.UnregisterCallback(btnCb);
        container?.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);
        click = null;
    }

    /// <summary>
    /// Shows the action popup with the specified data and click handler.
    /// </summary>
    /// <param name="data">The action data.</param>
    /// <param name="onClick">The click handler.</param>
    public void Show(ActionData data, Action onClick)
    {
        click = onClick;

        if (title != null)
            title.text = data ? data.ActionTitle : "";
        if (description != null)
            description.text = data ? data.ActionDescription : "";
        if (buttonText != null)
            buttonText.text = string.IsNullOrEmpty(data?.ActionBtnText) ? "OK" : data.ActionBtnText;
        if (roundedImage != null)
            roundedImage.style.backgroundImage =
                data && data.ActionJack ? new StyleBackground(data.ActionJack) : StyleKeyword.Null;

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
    /// Overrides the description text of the action popup.
    /// </summary>
    /// <param name="text">The new description text.</param>
    public void OverrideDescription(string text)
    {
        if (description != null)
            description.text = text ?? "";
    }

    /// <summary>
    /// Hides the action popup.
    /// </summary>
    public void Hide()
    {
        if (!isOpen && Root.style.display == DisplayStyle.None)
        {
            Hidden?.Invoke();
            return;
        }
        container.RemoveFromClassList("is-open"); // shrink out
        isOpen = false;
    }

    /// <summary>
    /// Handles the transition end event.
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
            if (prop == "scale" || prop == "opacity")
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
