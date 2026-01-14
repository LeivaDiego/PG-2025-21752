/// <summary>
/// Coordinates all HUD-related UI elements (base HUD, info widget, popups, settings)
/// for the active tour session.
/// </summary>
public sealed class HUDCoordinator : ICoordinator<BaseHUDView>
{
    // Dependencies
    private readonly UIRouter router;
    private readonly UIAtlas uiAtlas;
    private readonly TourViewModel vm;
    private readonly TourBinder binder;
    private readonly AudioAtlas audioAtlas;

    // Sub coordinators per feature
    private BaseHUDCoordinator baseHud;
    private InfoWidgetCoordinator info;
    private ActionPopupCoordinator action;
    private NoticePopupCoordinator notice;
    private SettingsCoordinator settings;

    // State
    private bool sawConnectionThisFloor;
    private BaseHUDView v;

    /// <summary>
    /// Constructor for HUDCoordinator.
    /// </summary>
    /// <param name="r">The UI router used for navigation.</param>
    /// <param name="model">The tour view model.</param>
    /// <param name="b">The tour binder for handling tour actions.</param>
    /// <param name="ua">The UI atlas containing UI assets.</param>
    /// <param name="aa">The audio atlas containing audio assets.</param>
    /// </summary>
    public HUDCoordinator(UIRouter r, TourViewModel model, TourBinder b, UIAtlas ua, AudioAtlas aa)
    {
        // Assign dependencies
        router = r;
        uiAtlas = ua;
        vm = model;
        binder = b;
        audioAtlas = aa;
        // Initialize sub-coordinators
        info = new InfoWidgetCoordinator(r, vm, b);
        action = new ActionPopupCoordinator(r, ua, b, model);
        notice = new NoticePopupCoordinator(r, ua);
        settings = new SettingsCoordinator(r, audioAtlas);
        baseHud = new BaseHUDCoordinator(r, vm, ua, aa);
    }

    /// <summary>
    /// Attach the BaseHUDView and set up event handlers.
    /// </summary>
    /// <param name="view">The BaseHUDView to attach.</param>
    /// <remarks> This method also attaches sub-coordinators and subscribes to view model events.</remarks>
    public void Attach(BaseHUDView view)
    {
        // Assign the view instance
        v = view;
        baseHud.Attach(view);
        // Subscribe to view model events
        vm.Changed += ApplyPhase;
        vm.EnteredArea += OnEnteredArea;
        vm.GuidingTo += _ => { };
        vm.FloorBegan += _ => sawConnectionThisFloor = false;
        vm.ConnectionLost += OnConnLost;
        vm.ConnectionRestored += OnConnRestored;
        vm.TourCompletedEvent += OnTourCompleted;
        vm.GuidingTo += OnGuidingTo;
        // Apply the initial phase
        ApplyPhase();
    }

    /// <summary>
    /// Detaches this coordinator from the HUD view and unsubscribes from tour events.
    /// </summary>
    public void Detach()
    {
        // Unsubscribe from view model events
        vm.Changed -= ApplyPhase;
        vm.EnteredArea -= OnEnteredArea;
        vm.ConnectionLost -= OnConnLost;
        vm.ConnectionRestored -= OnConnRestored;
        vm.TourCompletedEvent -= OnTourCompleted;
        vm.GuidingTo -= OnGuidingTo;
        // Detach sub-coordinators and clear the view reference
        baseHud?.Detach();
        info?.Hide();
        action?.Hide();
        notice?.Hide();
        v = null;
    }

    /// <summary>
    /// Applies the current tour phase to the HUD, updating visible widgets and audio.
    /// </summary>
    private void ApplyPhase()
    {
        // Update HUD elements based on the current phase
        switch (vm.Phase)
        {
            case TourUIPhase.WaitingForConnection:
                // Play connecting audio and show connecting notice
                AudioDirector.Instance.Stop(0.12f);
                action.Hide();
                info.Hide();
                notice.ShowConnecting();
                break;

            case TourUIPhase.ConnectionLost:
                // Play lost connection audio and show lost connection notice
                AudioDirector.Instance.Stop(0.12f);
                action.Hide();
                info.Hide();
                notice.ShowLostConnection();
                break;

            case TourUIPhase.ReadyPrompt:
                // Prepare the HUD for the ready prompt phase
                AudioDirector.Instance.Stop(0.12f);
                notice.Hide();
                info.Hide();
                // Show appropriate action popup based on tour state
                if (!vm.HasBegunTour)
                {
                    action.ShowStart();
                }
                else if (binder.WaitingForFloorContinue)
                {
                    var desc = vm.CurrentFloor ? vm.CurrentFloor.TransitionText : null;
                    action.ShowReadyOnFloor(desc);
                }
                else
                {
                    action.ShowReadyOnFloor();
                }
                break;

            case TourUIPhase.Navigating:
                // Show/hide relevant widgets
                notice.Hide();
                action.Hide();
                info.Hide();
                break;

            case TourUIPhase.InAreaInfo:
                // Hide irrelevant widgets
                notice.Hide();
                action.Hide();
                // Show info if available
                if (vm.CurrentAreaDef != null && vm.CurrentAreaDef.ShowInfo)
                    info.Show(vm.CurrentAreaDef);
                break;

            case TourUIPhase.FloorTransition:
                // Prepare the HUD for floor transition
                AudioDirector.Instance.Stop(0.12f);
                notice.Hide();
                // Show ready on floor action popup
                action.ShowReadyOnFloor();
                info.Hide();
                break;

            case TourUIPhase.TourComplete:
                // Show tour complete notice
                AudioDirector.Instance.Stop(0.12f);
                action.Hide();
                info.Hide();
                notice.ShowTourComplete();
                break;
        }
    }

    /// <summary>
    /// Handles entry into a new area, deciding whether to show info or advance.
    /// </summary>
    /// <param name="def">Definition of the area that was entered.</param>
    private void OnEnteredArea(AreaDefinition def)
    {
        // Decide whether to show area info or advance
        if (def && def.ShowInfo)
        {
            // Show area info widget
            vm.SetPhase(TourUIPhase.InAreaInfo);
            info.Show(def);
        }
        else
        {
            binder.RequestNext();
        }
    }

    /// <summary>
    /// Handles loss of connection and shows the appropriate notice.
    /// </summary>
    private void OnConnLost()
    {
        // Show lost connection or connecting notice
        // based on prior connection state
        if (sawConnectionThisFloor)
            notice.ShowLostConnection();
        else
            notice.ShowConnecting();
    }

    /// <summary>
    /// Handles connection restoration, updating state and tour phase as needed.
    /// </summary>
    private void OnConnRestored()
    {
        // Update connection state
        sawConnectionThisFloor = true;

        // Update tour phase if currently in connection lost state
        if (vm.Phase == TourUIPhase.ConnectionLost)
        {
            // Decide next phase based on current area
            if (vm.CurrentAreaDef != null && vm.CurrentAreaDef.ShowInfo)
                vm.SetPhase(TourUIPhase.InAreaInfo);
            else
                vm.SetPhase(TourUIPhase.Navigating);
        }

        notice.Hide();
    }

    /// <summary>
    /// Handles the event when guiding to a new area.
    /// </summary>
    private void OnGuidingTo(AreaDefinition _)
    {
        if (audioAtlas && audioAtlas.navigating)
            AudioDirector.Instance.Play(audioAtlas.navigating, 0f, 0.1f);
        else
            AudioDirector.Instance.Stop(0.1f);
    }

    /// <summary>
    /// Opens the settings view.
    /// </summary>
    public void OpenSettings() => settings.Show();

    /// <summary>
    /// Stub for handling tour completion.
    /// </summary>
    private void OnTourCompleted() { }
}
