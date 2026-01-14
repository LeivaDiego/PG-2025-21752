using UnityEngine;

/// <summary>
/// Coordinator for the Home screen.
/// </summary>
public sealed class HomeCoordinator : ICoordinator<HomeView>
{
    private HomeView v;
    private readonly UIRouter router;

    /// <summary>
    /// Constructor for HomeCoordinator.
    /// </summary>
    /// <param name="router">The UI router used for navigation.</param>
    public HomeCoordinator(UIRouter router)
    {
        // Assign the router
        this.router = router;
    }

    /// <summary>
    /// Attach the HomeView and set up event handlers.
    /// </summary>
    /// <param name="view">The HomeView instance to attach.</param>
    public void Attach(HomeView view)
    {
        // Assign the view instance
        v = view;
        // Set up event handlers
        v.OnExpress += OnExpress;
        v.OnComplete += OnComplete;
        v.OnMinigames += OnMinigames;
    }

    /// <summary>
    /// Detach the HomeView and remove event handlers.
    /// </summary>
    public void Detach()
    {
        // Check if the view is already detached
        if (v == null)
            return;
        // Remove event handlers and clear the view reference
        v.OnExpress -= OnExpress;
        v.OnComplete -= OnComplete;
        v.OnMinigames -= OnMinigames;
        v = null;
    }

    /// <summary>
    /// Handle the Express Tour selection.
    /// </summary>
    void OnExpress()
    {
        Debug.Log("[HomeCoordinator] Express Tour Selected");
        // Start the express tour and navigate to the Tour HUD
        var tourRunner = TourRunner.Instance;
        tourRunner.SelectTour(tourRunner.ExpressTour);
        tourRunner.BeginTour();
        router.ShowScreen(ScreenState.TourHUD);
    }

    /// <summary>
    /// Handle the Complete Tour selection.
    /// </summary>
    void OnComplete()
    {
        Debug.Log("Complete Tour Selected");
        // Start the complete tour and navigate to the Tour HUD
        var tourRunner = TourRunner.Instance;
        tourRunner.SelectTour(tourRunner.CompleteTour);
        tourRunner.BeginTour();
        router.ShowScreen(ScreenState.TourHUD);
    }

    /// <summary>
    /// Handle the Minigames selection.
    /// </summary>
    void OnMinigames()
    {
        Debug.Log("Minigames Selected");
        router.ShowScreen(ScreenState.Minigames);
    }
}
