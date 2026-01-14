using UnityEngine;

/// <summary>
/// Coordinator for the info widget during the tour
/// </summary>
public sealed class InfoWidgetCoordinator
{
    private readonly UIRouter router;
    private readonly TourBinder binder;

    private bool showing;

    /// <summary>
    /// Initializes a new instance of the <see cref="InfoWidgetCoordinator"/> class.
    /// </summary>
    /// <param name="r">The UI router.</param>
    /// <param name="model">The tour view model.</param>
    /// <param name="b">The tour binder.</param>
    public InfoWidgetCoordinator(UIRouter r, TourViewModel model, TourBinder b)
    {
        router = r;
        binder = b;
    }

    /// <summary>
    /// Shows the info widget for the specified area.
    /// </summary>
    /// <param name="area">The area definition to display information for.</param>
    public void Show(AreaDefinition area)
    {
        if (showing)
        {
            Hide();
            return;
        }

        var w = router.ShowOverlay(OverlayType.InfoModal) as InfoWidgetView;
        if (w == null)
            return;

        showing = true;

        w.Hidden -= OnHidden;
        w.Hidden += OnHidden;

        void Continue()
        {
            w.OnContinue -= Continue;

            AudioDirector.Instance.Stop(0.2f);
            binder.RequestNext();

            w.Hide();
        }

        w.OnContinue += Continue;
        w.Show(area);
    }

    /// <summary>
    /// Hides the info widget.
    /// </summary>
    public void Hide()
    {
        var w = router.GetOverlay<InfoWidgetView>(OverlayType.InfoModal);
        if (w == null)
        {
            showing = false;
            return;
        }
        w.Hide();
    }

    /// <summary>
    /// Handles the hidden event of the info widget.
    /// </summary>
    private void OnHidden()
    {
        var w = router.GetOverlay<InfoWidgetView>(OverlayType.InfoModal);
        if (w != null)
            w.Hidden -= OnHidden;

        showing = false;
        router.HideOverlay(OverlayType.InfoModal);
    }
}
