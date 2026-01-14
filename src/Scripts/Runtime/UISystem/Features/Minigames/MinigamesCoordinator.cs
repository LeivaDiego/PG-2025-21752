using UnityEngine;

/// <summary>
/// Coordinator for the Minigames feature in the UI system.
/// </summary>
public sealed class MinigamesCoordinator : ICoordinator<MinigamesView>
{
    private MinigamesView v;
    private readonly UIRouter router;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinigamesCoordinator"/> class.
    /// </summary>
    /// <param name="router">The UI router for navigation.</param>
    public MinigamesCoordinator(UIRouter router)
    {
        this.router = router;
    }

    /// <summary>
    /// Attaches the given view to the coordinator.
    /// </summary>
    /// <param name="view">The MinigamesView to attach.</param>
    public void Attach(MinigamesView view)
    {
        v = view;
        v.OnBreakout += OnBreakout;
        v.OnTrivia += OnTrivia;
        v.OnFlappy += OnFlappy;
        v.OnExit += OnExit;
    }

    /// <summary>
    /// Detaches the view from the coordinator.
    /// </summary>
    public void Detach()
    {
        v = null;
    }

    /// <summary>
    /// Handles the Breakout minigame selection.
    /// </summary>
    void OnBreakout()
    {
        Debug.Log("Breakout Selected");
    }

    /// <summary>
    /// Handles the Trivia minigame selection.
    /// </summary>
    void OnTrivia()
    {
        Debug.Log("Trivia Selected");
    }

    /// <summary>
    /// Handles the Flappy minigame selection.
    /// </summary>
    void OnFlappy()
    {
        Debug.Log("Flappy Selected");
    }

    /// <summary>
    /// Handles exiting the Minigames feature.
    /// </summary>
    void OnExit()
    {
        Debug.Log("Exit Minigames");
        router.ShowScreen(ScreenState.Home);
    }
}
