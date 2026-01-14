using UnityEngine.UIElements;

/// <summary>
/// Coordinator for notice popups during the tour
/// </summary>
public sealed class NoticePopupCoordinator
{
    private readonly UIRouter router;
    private readonly UIAtlas atlas;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoticePopupCoordinator"/> class.
    /// </summary>
    /// <param name="r">The UI router.</param>
    /// <param name="a">The UI atlas.</param>
    public NoticePopupCoordinator(UIRouter r, UIAtlas a)
    {
        router = r;
        atlas = a;
    }

    /// <summary>
    /// Shows the connecting notice popup.
    /// </summary>
    public void ShowConnecting() => ShowWithData(atlas.NoticeConnectingData);

    /// <summary>
    /// Shows the lost connection notice popup.
    /// </summary>
    public void ShowLostConnection() => ShowWithData(atlas.NoticeLostConnectionData);

    /// <summary>
    /// Shows the tour complete notice popup.
    /// </summary>
    public void ShowTourComplete() => ShowWithData(atlas.NoticeTourCompleteData);

    /// <summary>
    /// Hides the notice popup.
    /// </summary>
    public void Hide()
    {
        var v = router.GetOverlay<NoticePopupView>(OverlayType.NoticePopup);
        if (v == null)
            return;
        v.Hide();
    }

    /// <summary>
    /// Shows the notice popup with the specified data.
    /// </summary>
    /// <param name="data">The notice data.</param>
    void ShowWithData(NoticeData data)
    {
        var action = router.GetOverlay<ActionPopupView>(OverlayType.ActionPopup);
        if (action != null && action.Root.style.display != DisplayStyle.None)
        {
            void AfterActionHidden()
            {
                action.Hidden -= AfterActionHidden;
                ActuallyShow(data);
            }
            action.Hidden -= AfterActionHidden;
            action.Hidden += AfterActionHidden;
            action.Hide();
            return;
        }

        ActuallyShow(data);
    }

    /// <summary>
    /// Actually shows the notice popup with the specified data.
    /// </summary>
    /// <param name="data">The notice data.</param>
    void ActuallyShow(NoticeData data)
    {
        if (router.CurrentScreen != ScreenState.TourHUD)
            return;

        var v = router.ShowOverlay(OverlayType.NoticePopup) as NoticePopupView;
        if (v == null)
            return;

        void OnHidden()
        {
            v.Hidden -= OnHidden;
            router.HideOverlay(OverlayType.NoticePopup);
        }
        v.Hidden -= OnHidden;
        v.Hidden += OnHidden;

        v.Show(data);
        if (data != null && data.NoticeAudioClip)
            AudioDirector.Instance.Play(data.NoticeAudioClip);
    }
}
