using System;
using UnityEngine.UIElements;

/// <summary>
/// View for the notice popup during the tour
/// </summary>
public sealed class NoticePopupView : IOverlayView
{
    public VisualElement Root { get; }
    public event Action Hidden;

    VisualElement container,
        roundImage;
    Label title,
        message;
    bool isOpen;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoticePopupView"/> class.
    /// </summary>
    /// <param name="root">The root visual element.</param>
    public NoticePopupView(VisualElement root)
    {
        Root = root;
    }

    /// <summary>
    /// Binds the view to the specified UIDocument.
    /// </summary>
    /// <param name="doc">The UIDocument to bind to.</param>
    public void Bind(UIDocument doc)
    {
        container = Root.Q<VisualElement>("PopUp");
        roundImage = Root.Q<VisualElement>("RoundImage");
        title = Root.Q<Label>("MessageTitle");
        message = Root.Q<Label>("Message");

        UIPickingUtils.ConfigureTreePickingMode(Root, PickingMode.Ignore);
        UIPickingUtils.ConfigureTreePickingMode(container, PickingMode.Position);

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
        container?.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);
    }

    /// <summary>
    /// Shows the notice popup with the specified data.
    /// </summary>
    /// <param name="data">The notice data.</param>
    public void Show(NoticeData data)
    {
        if (title != null)
            title.text = data ? data.NoticeTitle : "";
        if (message != null)
            message.text = data ? data.NoticeMessage : "";
        if (roundImage != null)
            roundImage.style.backgroundImage =
                data && data.NoticeJack ? new StyleBackground(data.NoticeJack) : StyleKeyword.Null;

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
    /// Hides the notice popup.
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
