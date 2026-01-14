using UnityEngine;

/// <summary>
/// Coordinator for the menu modal during the tour
/// </summary>
public sealed class MenuCoordinator
{
    private readonly UIRouter router;
    private readonly AudioAtlas audioAtlas;

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuCoordinator"/> class.
    /// </summary>
    /// <param name="r">The UI router.</param>
    /// <param name="ua">The UI atlas.</param>
    /// <param name="aa">The audio atlas.</param>
    public MenuCoordinator(UIRouter r, UIAtlas ua, AudioAtlas aa)
    {
        router = r;
        audioAtlas = aa;
    }

    /// <summary>
    /// Shows the menu modal.
    /// </summary>
    public void Show()
    {
        var m = router.ShowOverlay(OverlayType.Menu) as MenuView;
        if (m == null)
            return;

        /// <summary>
        /// Closes the menu modal.
        /// </summary>
        void Close()
        {
            m.Hide();
        }

        /// <summary>
        /// Handles the hidden event of the menu modal.
        /// </summary>
        void OnHidden()
        {
            Unhook();
            router.HideOverlay(OverlayType.Menu);
        }

        /// <summary>
        /// Returns to the home screen.
        /// </summary>
        void ReturnHome()
        {
            Close();
            router.HideAllOverlays();
            router.ShowScreen(ScreenState.Home);
            AudioDirector.Instance.Stop(0.12f);

            var tr = TourRunner.Instance;
            if (tr == null)
                return;
            tr.StopTour(true);
        }

        /// <summary>
        /// Restarts the current tour.
        /// </summary>
        void Restart()
        {
            var tr = TourRunner.Instance;
            if (tr == null)
                return;
            var current = tr.CurrentTour;
            tr.StopTour(false);
            tr.SelectTour(current);
            tr.BeginTour();
            Close();
        }

        /// <summary>
        /// Shows the help information.
        /// </summary>
        void Help()
        {
            Debug.Log("[MenuCoordinator] Help pressed.");
        }

        /// <summary>
        /// Shows the settings modal.
        /// </summary>
        void Settings()
        {
            var settings = new SettingsCoordinator(router, audioAtlas);
            settings.Show();
        }

        /// <summary>
        /// Unhooks all event handlers from the menu view.
        /// </summary>
        void Unhook()
        {
            m.OnClose -= Close;
            m.OnReturnHome -= ReturnHome;
            m.OnRestart -= Restart;
            m.OnHelp -= Help;
            m.OnSettings -= Settings;
            m.Hidden -= OnHidden;
        }

        m.OnClose += Close;
        m.OnReturnHome += ReturnHome;
        m.OnRestart += Restart;
        m.OnHelp += Help;
        m.OnSettings += Settings;
        m.Hidden += OnHidden;

        m.Show();
    }
}
