using System;
using UnityEngine.UIElements;

/// <summary>
/// Coordinator for action popups during the tour
/// </summary>
public sealed class ActionPopupCoordinator
{
    private readonly UIRouter router;
    private readonly UIAtlas atlas;
    private readonly TourBinder binder;
    private readonly TourViewModel vm;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionPopupCoordinator"/> class.
    /// </summary>
    /// <param name="r">The UI router.</param>
    /// <param name="a">The UI atlas.</param>
    /// <param name="b">The tour binder.</param>
    /// <param name="model">The tour view model.</param>
    public ActionPopupCoordinator(UIRouter r, UIAtlas a, TourBinder b, TourViewModel model)
    {
        router = r;
        atlas = a;
        binder = b;
        vm = model;
    }

    /// <summary>
    /// Shows the start action popup.
    /// </summary>
    public void ShowStart() => ShowWith(atlas.ActionStartData, OnStartClick);

    /// <summary>
    /// Shows the ready on floor action popup.
    /// </summary>
    /// <param name="descriptionOverride">Optional description override.</param>
    public void ShowReadyOnFloor(string descriptionOverride = null)
    {
        ShowWith(atlas.ActionReadyOnFloorData, OnReadyClick, descriptionOverride);

        void OnReadyClick(ActionPopupView v)
        {
            v.Hide();
            if (vm.Phase == TourUIPhase.FloorTransition)
                TourRunner.Instance?.ContinueToNextFloor();
            else
                binder.RequestUserReady();
        }
    }

    /// <summary>
    /// Hides the action popup.
    /// </summary>
    public void Hide()
    {
        var v = router.GetOverlay<ActionPopupView>(OverlayType.ActionPopup);
        if (v == null)
            return;
        v.Hide(); // removal happens on Hidden
    }

    // Helpers

    /// <summary>
    /// Shows the action popup with the specified data and click handler.
    /// </summary>
    /// <param name="data">The action data.</param>
    /// <param name="onClick">The click handler.</param>
    /// <param name="descOverride">Optional description override.</param>
    void ShowWith(ActionData data, Action<ActionPopupView> onClick, string descOverride = null)
    {
        // If Notice popup is up, hide it first and chain
        var notice = router.GetOverlay<NoticePopupView>(OverlayType.NoticePopup);
        if (notice != null && notice.Root.style.display != DisplayStyle.None)
        {
            void AfterNoticeHidden()
            {
                notice.Hidden -= AfterNoticeHidden;
                ActuallyShow(data, onClick, descOverride);
            }
            notice.Hidden -= AfterNoticeHidden;
            notice.Hidden += AfterNoticeHidden;
            notice.Hide();
            return;
        }

        ActuallyShow(data, onClick, descOverride);
    }

    /// <summary>
    /// Actually shows the action popup with the specified data and click handler.
    /// </summary>
    /// <param name="data">The action data.</param>
    /// <param name="onClick">The click handler.</param>
    /// <param name="descOverride">Optional description override.</param>
    void ActuallyShow(ActionData data, Action<ActionPopupView> onClick, string descOverride)
    {
        if (router.CurrentScreen != ScreenState.TourHUD)
            return;

        var v = router.ShowOverlay(OverlayType.ActionPopup) as ActionPopupView;
        if (v == null)
            return;

        void OnHidden()
        {
            v.Hidden -= OnHidden;
            router.HideOverlay(OverlayType.ActionPopup);
        }
        v.Hidden -= OnHidden;
        v.Hidden += OnHidden;

        v.Show(data, () => onClick(v));
        if (!string.IsNullOrEmpty(descOverride))
            v.OverrideDescription(descOverride);

        if (data && data.ActionAudioClip)
            AudioDirector.Instance.Play(data.ActionAudioClip, 0.05f, 0.1f);
    }

    /// <summary>
    /// Handles the start click event.
    /// </summary>
    /// <param name="v">The action popup view.</param>
    void OnStartClick(ActionPopupView v)
    {
        v.Hide();
        vm.MarkTourBegan();
        binder.RequestUserReady();
    }
}
