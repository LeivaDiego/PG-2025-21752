using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// View for the base HUD during the tour
/// </summary>
public sealed class BaseHUDView : IScreenView
{
    public VisualElement Root { get; }
    public event Action OnMenu;

    VisualElement directionsCard,
        footer,
        menuBtn,
        titleIcon;
    Label titleLabel,
        directionsLabel,
        progressValue,
        distanceValue;

    bool footerOpen,
        dirOpen;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseHUDView"/> class.
    /// </summary>
    /// <param name="root">The root visual element of the base HUD view.</param>
    public BaseHUDView(VisualElement root)
    {
        Root = root;
    }

    /// <summary>
    /// Binds the view to the given UIDocument.
    /// </summary>
    /// <param name="doc">The UIDocument to bind to.</param>
    public void Bind(UIDocument doc)
    {
        directionsCard = Root.Q<VisualElement>("DirectionsCard");
        footer = Root.Q<VisualElement>("Footer");
        menuBtn = Root.Q<VisualElement>("MenuButton");
        titleLabel = Root.Q<Label>("Title");
        directionsLabel = Root.Q<Label>("Directions");
        progressValue = Root.Q<Label>("ProgressValue");
        distanceValue = Root.Q<Label>("DistanceValue");
        titleIcon = Root.Q<VisualElement>("TitleIcon");

        UIPickingUtils.ConfigureTreePickingMode(Root, PickingMode.Ignore);
        UIPickingUtils.SetPickable(menuBtn);
        menuBtn?.RegisterCallback<ClickEvent>(_ => OnMenu?.Invoke());

        if (footer != null)
            footer.RegisterCallback<TransitionEndEvent>(OnFooterTransitionEnd);
        if (directionsCard != null)
            directionsCard.RegisterCallback<TransitionEndEvent>(OnDirTransitionEnd);

        footer?.RemoveFromClassList("is-open");
        directionsCard?.RemoveFromClassList("is-open");
        if (footer != null)
            footer.style.display = DisplayStyle.None;
        if (directionsCard != null)
            directionsCard.style.display = DisplayStyle.None;
        footerOpen = dirOpen = false;
    }

    /// <summary>
    /// Unbinds the view from the current UIDocument.
    /// </summary>
    public void Unbind()
    {
        menuBtn?.UnregisterCallback<ClickEvent>(_ => OnMenu?.Invoke());
        footer?.UnregisterCallback<TransitionEndEvent>(OnFooterTransitionEnd);
        directionsCard?.UnregisterCallback<TransitionEndEvent>(OnDirTransitionEnd);
    }

    /// <summary>
    /// Sets the title text and optional icon in the base HUD view.
    /// </summary>
    /// <param name="text">The title text to display.</param>
    /// <param name="icon">The optional icon to display alongside the title.</param>
    public void SetTitle(string text, Sprite icon = null)
    {
        if (titleLabel != null)
            titleLabel.text = text ?? "";
        if (titleIcon != null)
            titleIcon.style.backgroundImage = icon ? new StyleBackground(icon) : StyleKeyword.Null;
    }

    /// <summary>
    /// Sets the directions text and visibility in the base HUD view.
    /// </summary>
    /// <param name="text">The directions text to display.</param>
    /// <param name="visible">Whether the directions should be visible.</param>
    public void SetDirections(string text, bool visible)
    {
        if (directionsLabel != null)
            directionsLabel.text = text ?? "";

        if (directionsCard == null)
            return;

        if (visible)
        {
            if (dirOpen)
                return;
            directionsCard.style.display = DisplayStyle.Flex;
            directionsCard.RemoveFromClassList("is-open");

            void AfterLayout(GeometryChangedEvent _)
            {
                directionsCard.UnregisterCallback<GeometryChangedEvent>(AfterLayout);
                directionsCard
                    .schedule.Execute(() =>
                    {
                        directionsCard.AddToClassList("is-open");
                        dirOpen = true;
                    })
                    .StartingIn(0);
            }
            directionsCard.RegisterCallback<GeometryChangedEvent>(AfterLayout);
        }
        else
        {
            if (!dirOpen && directionsCard.style.display == DisplayStyle.None)
                return;
            directionsCard.RemoveFromClassList("is-open");
            dirOpen = false;
        }
    }

    /// <summary>
    /// Sets the progress value in the base HUD view.
    /// </summary>
    /// <param name="normalized01">The progress value normalized between 0 and 1.</param>
    public void SetProgress(float normalized01)
    {
        if (progressValue == null)
            return;
        var pct = Mathf.Round(normalized01 * 100f);
        progressValue.text = $"{pct:0}%";
    }

    /// <summary>
    /// Sets the distance value in the base HUD view.
    /// </summary>
    /// <param name="meters">The distance value in meters.</param>
    public void SetDistance(float meters)
    {
        if (distanceValue != null)
            distanceValue.text = $"{meters:0.00}m";
    }

    /// <summary>
    /// Shows or hides the footer in the base HUD view.
    /// </summary>
    /// <param name="on">Whether to show (true) or hide (false) the footer.</param>
    public void ShowFooter(bool on)
    {
        if (footer == null)
            return;

        if (on)
        {
            if (footerOpen)
                return;
            footer.style.display = DisplayStyle.Flex;
            footer.RemoveFromClassList("is-open");

            void AfterLayout(GeometryChangedEvent _)
            {
                footer.UnregisterCallback<GeometryChangedEvent>(AfterLayout);
                footer
                    .schedule.Execute(() =>
                    {
                        footer.AddToClassList("is-open");
                        footerOpen = true;
                    })
                    .StartingIn(0);
            }
            footer.RegisterCallback<GeometryChangedEvent>(AfterLayout);
        }
        else
        {
            if (!footerOpen && footer.style.display == DisplayStyle.None)
                return;
            footer.RemoveFromClassList("is-open");
            footerOpen = false;
        }
    }

    /// <summary>
    /// Sets the title icon in the base HUD view.
    /// </summary>
    /// <param name="s">The sprite to use as the title icon.</param>
    public void SetTitleIcon(Sprite s)
    {
        if (titleIcon == null)
            return;
        titleIcon.style.backgroundImage = s != null ? new StyleBackground(s) : StyleKeyword.Null;
    }

    /// <summary>
    /// Handles the transition end event for the footer.
    /// </summary>
    /// <param name="e">The transition end event.</param>
    void OnFooterTransitionEnd(TransitionEndEvent e)
    {
        if (e.target != footer)
            return;
        bool relevant = false;
        foreach (var n in e.stylePropertyNames)
        {
            var p = n.ToString();
            if (p == "opacity" || p == "translate")
            {
                relevant = true;
                break;
            }
        }
        if (!relevant)
            return;
        if (!footerOpen)
            footer.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Handles the transition end event for the directions card.
    /// </summary>
    /// <param name="e">The transition end event.</param>
    void OnDirTransitionEnd(TransitionEndEvent e)
    {
        if (e.target != directionsCard)
            return;
        bool relevant = false;
        foreach (var n in e.stylePropertyNames)
        {
            var p = n.ToString();
            if (p == "opacity" || p == "scale")
            {
                relevant = true;
                break;
            }
        }
        if (!relevant)
            return;
        if (!dirOpen)
            directionsCard.style.display = DisplayStyle.None;
    }
}
