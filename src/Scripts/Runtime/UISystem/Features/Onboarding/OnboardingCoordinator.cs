using UnityEngine;

/// <summary>
/// Coordinator for the Onboarding process.
/// </summary>
public sealed class OnboardingCoordinator : ICoordinator<OnboardingView>
{
    private readonly UIRouter router;
    private readonly OnboardingSet set;
    private int index = 0;
    private OnboardingView v;

    /// <summary>
    /// Constructor for OnboardingCoordinator.
    /// </summary>
    /// <param name="r">The UI router used for navigation.</param>
    /// <param name="s">The set of onboarding slides.</param>
    public OnboardingCoordinator(UIRouter r, OnboardingSet s)
    {
        router = r;
        set = s;
    }

    /// <summary>
    /// Attach the OnboardingView and set up event handlers.
    /// </summary>
    /// <param name="view">The OnboardingView instance to attach.</param>
    public void Attach(OnboardingView view)
    {
        // Assign the view instance
        v = view;
        index = 0;
        // Set up the first slide
        Apply();
        v.OnNext += Next;
    }

    /// <summary>
    /// Detach the OnboardingView and remove event handlers.
    /// </summary>
    public void Detach()
    {
        // Remove event handlers and clear the view reference
        if (v != null)
            v.OnNext -= Next;
        v = null;
    }

    /// <summary>
    /// Apply the current slide to the view.
    /// </summary>
    void Apply()
    {
        // Check if the onboarding set is valid
        if (set == null || set.Slides.Length == 0)
        {
            // No slides available, finish onboarding
            Finish();
            return;
        }
        // Clamp the index to valid range
        index = Mathf.Clamp(index, 0, set.Slides.Length - 1);
        // Determine if this is the last slide
        bool last = index == set.Slides.Length - 1;
        var slide = set.Slides[index];
        // Set the slide in the view
        v.SetSlide(slide, last);
    }

    /// <summary>
    /// Proceed to the next slide or finish onboarding.
    /// </summary>
    void Next()
    {
        // Increment the slide index
        index++;
        // Check if we've reached the end of the slides
        if (set == null || index >= set.Slides.Length)
        {
            // No more slides, finish onboarding
            Finish();
            return;
        }
        // Apply the next slide
        Apply();
    }

    /// <summary>
    /// Finish the onboarding process and navigate to Home screen.
    /// </summary>
    void Finish()
    {
        // Mark onboarding as seen and navigate to Home screen
        OnboardingGate.MarkSeen();
        router.ShowScreen(ScreenState.Home);
    }
}
