using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// View for the settings modal during the tour
/// </summary>
public sealed class SettingsView : IOverlayView
{
    public VisualElement Root { get; }
    public event Action CloseRequested;
    public event Action<int> VolumeChanged;
    public event Action VolumeChangeCommitted;
    public event Action<int> FontPxPicked;
    public event Action Hidden;

    Slider slider;
    VisualElement smallOpt,
        normalOpt,
        largeOpt,
        volumeIcon;
    VisualElement container;
    UIDocument baseDoc;

    IVisualElementScheduledItem previewSched;
    bool isOpen;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsView"/> class.
    /// </summary>
    /// <param name="root">The root visual element.</param>
    public SettingsView(VisualElement root) => Root = root;

    /// <summary>
    /// Binds the view to the specified UIDocument.
    /// </summary>
    /// <param name="doc">The UIDocument to bind to.</param>
    public void Bind(UIDocument doc)
    {
        baseDoc = doc;
        container = Root.Q<VisualElement>("SettingsContainer");

        slider = Root.Q<Slider>("Slider");
        smallOpt = Root.Q<VisualElement>("Small");
        normalOpt = Root.Q<VisualElement>("Normal");
        largeOpt = Root.Q<VisualElement>("Large");
        volumeIcon = Root.Q<VisualElement>("VolumeIcon");

        Root.Q<VisualElement>("CloseIcon")
            ?.RegisterCallback<ClickEvent>(_ => CloseRequested?.Invoke());

        UIPickingUtils.ConfigureTreePickingMode(Root, PickingMode.Ignore);
        UIPickingUtils.ConfigureTreePickingMode(container, PickingMode.Position);

        slider.lowValue = 0;
        slider.highValue = 100;

        slider.RegisterValueChangedCallback(e =>
        {
            VolumeChanged?.Invoke(Mathf.RoundToInt(e.newValue));
            previewSched?.Pause();
            previewSched = slider
                .schedule.Execute(() => VolumeChangeCommitted?.Invoke())
                .StartingIn(250);
        });

        var dragger = slider.Q<VisualElement>("unity-dragger");
        if (dragger != null)
        {
            dragger.RegisterCallback<PointerUpEvent>(_ => VolumeChangeCommitted?.Invoke());
            dragger.RegisterCallback<PointerCaptureOutEvent>(_ => VolumeChangeCommitted?.Invoke());
            dragger.RegisterCallback<PointerCancelEvent>(_ => VolumeChangeCommitted?.Invoke());
        }

        smallOpt?.RegisterCallback<ClickEvent>(_ => FontPxPicked?.Invoke(80));
        normalOpt?.RegisterCallback<ClickEvent>(_ => FontPxPicked?.Invoke(100));
        largeOpt?.RegisterCallback<ClickEvent>(_ => FontPxPicked?.Invoke(120));

        slider.RegisterCallback<GeometryChangedEvent>(_ => ApplySliderStyles());
        slider.RegisterValueChangedCallback(_ => ApplySliderStyles());

        container.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
        container.RemoveFromClassList("is-open");
        Root.style.display = DisplayStyle.None;
        isOpen = false;

        Show();
    }

    /// <summary>
    /// Unbinds the view from the UIDocument.
    /// </summary>
    public void Unbind()
    {
        container?.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);
    }

    /// <summary>
    /// Shows the settings modal.
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
    /// Hides the settings modal.
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

    /// <summary>
    /// Applies styles to the slider components.
    /// </summary>
    public void ApplySliderStyles()
    {
        var tracker = slider.Q<VisualElement>("unity-tracker");
        var dragger = slider.Q<VisualElement>("unity-dragger");
        var fill = tracker.Q<VisualElement>("unity-fill");

        if (tracker != null)
        {
            tracker.style.backgroundImage = null;
            tracker.style.unityBackgroundImageTintColor = StyleKeyword.None;
            tracker.style.backgroundColor = new StyleColor(new Color32(75, 75, 75, 255));
        }
        if (dragger != null)
        {
            dragger.style.backgroundImage = null;
            dragger.style.unityBackgroundImageTintColor = StyleKeyword.None;
            dragger.style.backgroundColor = new StyleColor(new Color32(242, 242, 242, 255));
        }
        if (fill != null)
        {
            fill.style.backgroundColor = new StyleColor(new Color32(0, 165, 0, 255));
        }
    }

    /// <summary>
    /// Sets the slider value without triggering change events.
    /// </summary>
    /// <param name="v">The slider value.</param>
    public void SetSlider(int v)
    {
        slider.SetValueWithoutNotify(v);
    }

    /// <summary>
    /// Sets the selected font size in pixels.
    /// </summary>
    /// <param name="px">The font size in pixels.</param>
    public void SetSelectedFontPx(int px)
    {
        ToggleSel(smallOpt, px == 80);
        ToggleSel(normalOpt, px == 100);
        ToggleSel(largeOpt, px == 120);
    }

    /// <summary>
    /// Toggles the "selected" class on a visual element.
    /// </summary>
    /// <param name="ve">The visual element.</param>
    /// <param name="on">Whether to add or remove the "selected" class.</param>
    void ToggleSel(VisualElement ve, bool on)
    {
        if (ve == null)
            return;
        ve.EnableInClassList("selected", on);
    }

    /// <summary>
    /// Sets the volume icon based on the volume level.
    /// </summary>
    /// <param name="level">The volume level.</param>
    public void SetVolumeIcon(VolumeLevel level)
    {
        if (volumeIcon == null)
            return;
        volumeIcon.EnableInClassList("mute", level == VolumeLevel.Mute);
        volumeIcon.EnableInClassList("low", level == VolumeLevel.Low);
        volumeIcon.EnableInClassList("med", level == VolumeLevel.Med);
        volumeIcon.EnableInClassList("high", level == VolumeLevel.High);
    }
}
