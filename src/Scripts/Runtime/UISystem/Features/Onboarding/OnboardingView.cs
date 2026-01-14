using System;
using UnityEngine.UIElements;

/// <summary>
/// View for onboarding screens
/// </summary>
public sealed class OnboardingView : IScreenView
{
    public VisualElement Root { get; }
    public event Action OnNext;

    VisualElement art,
        btn;

    Label title,
        desc,
        btnText;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnboardingView"/> class.
    /// </summary>
    /// <param name="root">The root visual element of the onboarding view.</param>
    public OnboardingView(VisualElement root)
    {
        Root = root;
    }

    /// <summary>
    /// Binds the view to the given UIDocument.
    /// </summary>
    /// <param name="doc">The UIDocument to bind to.</param>
    public void Bind(UIDocument doc)
    {
        art = Root.Q<VisualElement>("Art");
        title = Root.Q<Label>("Title");
        desc = Root.Q<Label>("Description");
        btn = Root.Q<VisualElement>("Button");
        btnText = Root.Q<Label>("Text");

        btn?.RegisterCallback<ClickEvent>(_ =>
        {
            AudioDirector.Instance.Stop();
            OnNext?.Invoke();
        });
    }

    /// <summary>
    /// Unbinds the view from the current UIDocument.
    /// </summary>
    public void Unbind()
    {
        btn?.UnregisterCallback<ClickEvent>(_ => OnNext?.Invoke());
    }

    /// <summary>
    /// Sets the current slide to be displayed in the onboarding view.
    /// </summary>
    /// <param name="s">The onboarding slide to display.</param>
    /// <param name="isLast">Indicates if this is the last slide.</param>
    public void SetSlide(OnboardingSlide s, bool isLast)
    {
        title.text = s != null ? s.Title : null ?? "";
        desc.text = s != null ? s.Description : null ?? "";
        if (btnText != null)
            btnText.text = !string.IsNullOrEmpty(s?.ButtonText ?? s?.ButtonText)
                ? (s.ButtonText ?? s.ButtonText)
                : (isLast ? "Empezar" : "Siguiente");
        if (s != null ? s.Art : null)
            art.style.backgroundImage = new StyleBackground(s.Art);
        else
            art.style.backgroundImage = StyleKeyword.Null;
        if (s != null && s.Narration != null)
            AudioDirector.Instance.Play(s.Narration);
    }
}
