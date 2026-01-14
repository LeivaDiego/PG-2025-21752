using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Binds the running tour logic (<see cref="TourRunner"/>, <see cref="FloorManager"/>,
/// movement, positioning, and path provider) to the <see cref="TourViewModel"/>.
/// Handles floor transitions, connection changes, and view-model updates.
/// </summary>
public sealed class TourBinder : MonoBehaviour
{
    // External References
    private TourRunner tourRunner;
    private PathProvider pathProvider;
    private MovementAgent movementAgent;
    private UWBPositioning uwb;
    private TourViewModel vm;
    private FloorManager fm;

    // Internal State
    private bool waitingForFloorStart;
    private bool waitingForFloorContinue;
    private TourUIPhase lastPhaseBeforeDisconnect = TourUIPhase.Navigating;

    // Public Properties
    public bool WaitingForFloorStart => waitingForFloorStart;
    public bool WaitingForFloorContinue => waitingForFloorContinue;
    public FloorManager ActiveFloorManager => fm;
    public MovementAgent Movement => movementAgent;

    /// <summary>
    /// Initializes the binder with a tour view-model and hooks into the
    /// current <see cref="TourRunner"/> and active <see cref="FloorManager"/>.
    /// </summary>
    /// <param name="model">The view-model to bind to.</param>
    public void Init(TourViewModel model)
    {
        // Store reference to view-model
        vm = model;

        // Find and hook to TourRunner
        if (!tourRunner)
            tourRunner = FindFirstObjectByType<TourRunner>(FindObjectsInactive.Include);

        // Hook to TourRunner events
        if (tourRunner != null)
        {
            // Subscribe to tour events
            tourRunner.FloorLoaded += OnFloorLoaded;
            tourRunner.FloorUnloaded += OnFloorUnloaded;
            tourRunner.TourCompleted += vm.NotifyTourCompleted;
            // Initialize view-model state
            vm.SetProgress(tourRunner.Progress);
        }
        // Hook to active FloorManager if present
        HookToActiveFloorManager();
    }

    /// <summary>
    /// Sets the connection state, notifying the view-model and updating
    /// the current UI phase based on floor start/continue state.
    /// </summary>
    /// <param name="connected">Whether the system is connected.</param>
    public void SetConnection(bool connected)
    {
        Debug.Log(
            $"[TourBinder] SetConnection({connected}), waitingForFloorStart={waitingForFloorStart}, waitingForFloorContinue={waitingForFloorContinue}, lastPhase={lastPhaseBeforeDisconnect}"
        );
        // Update view-model connection state
        vm.SetConnected(connected);

        // Update UI phase based on connection and waiting states
        if (!connected)
        {
            // Store last phase before disconnect
            lastPhaseBeforeDisconnect = vm.Phase;
            // Transition to appropriate disconnected phase
            if (waitingForFloorStart || waitingForFloorContinue)
                vm.SetPhase(TourUIPhase.WaitingForConnection);
            else
                // Transition to connection lost phase
                vm.SetPhase(TourUIPhase.ConnectionLost);
            return;
        }

        // Handle reconnection
        if (waitingForFloorContinue)
        {
            // On reconnection during floor continue,
            // prompt user to continue
            vm.SetPhase(TourUIPhase.ReadyPrompt);
            return;
        }

        // Handle reconnection during floor start
        if (waitingForFloorStart)
        {
            // On reconnection during floor start,
            // prompt user to start tour
            if (!vm.HasBegunTour)
            {
                // First floor of tour, prompt to begin
                vm.SetPhase(TourUIPhase.ReadyPrompt);
            }
            else
            {
                // Subsequent floor, resume navigation
                waitingForFloorStart = false;
                vm.SetPhase(TourUIPhase.Navigating);
                RequestUserReady();
            }
            return;
        }
        // For other cases, restore last phase
        vm.SetPhase(lastPhaseBeforeDisconnect);
    }

    /// <summary>
    /// Requests that the user is ready to begin the current floor.
    /// Notifies the <see cref="FloorManager"/> and unpauses the tour.
    /// </summary>
    public void RequestUserReady()
    {
        // Validate FloorManager
        if (fm == null)
        {
            Debug.LogWarning("[TourBinder] RequestUserReady called with no FloorManager.");
            return;
        }

        // Update internal state and notify FloorManager
        waitingForFloorStart = false;
        fm.UserReady();
        vm.SetPaused(false);
    }

    /// <summary>
    /// Requests that the <see cref="FloorManager"/> proceed to the next area.
    /// </summary>
    public void RequestNext()
    {
        // Validate FloorManager
        if (fm == null)
        {
            Debug.LogWarning("[TourBinder] RequestNext called with no FloorManager.");
            return;
        }
        // Instruct FloorManager to proceed to next area
        fm.Next();
    }

    /// <summary>
    /// Unity callback invoked when this component is destroyed.
    /// Unhooks from all external events and dependencies.
    /// </summary>
    void OnDestroy()
    {
        // Unhook from TourRunner events
        if (tourRunner != null)
        {
            tourRunner.FloorLoaded -= OnFloorLoaded;
            tourRunner.FloorUnloaded -= OnFloorUnloaded;
            tourRunner.TourCompleted -= vm.NotifyTourCompleted;
        }
        // Unhook from FloorManager events
        UnhookFM();

        // Unhook from PathProvider events if present
        if (pathProvider)
            pathProvider.OnPathUpdated -= OnPathUpdated;
    }

#if UNITY_EDITOR && !UNITY_IOS
    /// <summary>
    /// Unity update loop for editor builds on non-iOS platforms.
    /// Ensures the binder is hooked to an active <see cref="FloorManager"/>.
    /// </summary>
    void Update()
    {
        // Ensure we are hooked to an active FloorManager
        if (fm == null)
            HookToActiveFloorManager();
    }
#endif

    /// <summary>
    /// Handles a floor being loaded by the <see cref="TourRunner"/>.
    /// Sets up movement, path provider, UWB, and updates the view-model state.
    /// </summary>
    /// <param name="floor">The loaded floor definition.</param>
    /// <param name="floorMgr">The manager for the loaded floor.</param>
    private void OnFloorLoaded(FloorDefinition floor, FloorManager floorMgr)
    {
        // Swap to the new FloorManager and hook events
        SwapFM(floorMgr);
        // Find MovementAgent and set paused state
        movementAgent = FindFirstObjectByType<MovementAgent>(FindObjectsInactive.Include);
        // Initially pause movement until user is ready
        vm.SetPaused(movementAgent ? !movementAgent.IsEnabled : true);
        // Bind to PathProvider and UWBPositioning
        BindPathProvider();
        BindUWB();
#if UNITY_IOS && !UNITY_EDITOR
        // Enable UWB positioning on iOS
        var u = movementAgent ? movementAgent.GetComponent<UWBPositioning>() : null;
        if (u)
        {
            // Enable and start UWB tracking
            u.enabled = true;
            Debug.Log("[TourBinder] Enabled UWBPositioning component.");
            u.StartTracking();
        }
        else
        {
            Debug.LogWarning("[TourBinder] No UWBPositioning component found on MovementAgent.");
        }
#endif

        // Update view-model with new floor and reset states
        vm.SetCurrentFloor(floor);
        waitingForFloorStart = true;
        waitingForFloorContinue = false;
        vm.SetPhase(TourUIPhase.WaitingForConnection);
        vm.SetProgress(tourRunner.Progress);
        vm.SetDistance(0f);
    }

    /// <summary>
    /// Handles a floor being unloaded by the <see cref="TourRunner"/>.
    /// Tears down bindings and updates the view-model for transition
    /// or tour stop as appropriate.
    /// </summary>
    /// <param name="floor">The unloaded floor definition.</param>
    private void OnFloorUnloaded(FloorDefinition floor)
    {
        // Unhook from path provider and UWB positioning
        UnbindPathProvider();
        UnbindUWB();

#if UNITY_IOS && !UNITY_EDITOR
        // Disable UWB positioning on iOS
        var u = movementAgent ? movementAgent.GetComponent<UWBPositioning>() : null;
        if (u)
        {
            // Stop and disable UWB tracking
            u.StopTracking();
            u.enabled = false;
        }
#endif
        // Clear movement agent reference
        movementAgent = null;
        // Pause the tour
        vm.SetPaused(true);
        // Check if the tour is stopping
        if (tourRunner != null && tourRunner.IsStopping)
        {
            // Tour is stopping, reset state
            Debug.Log("[TourBinder] Tour is stopping, not prompting for continue.");
            waitingForFloorStart = false;
            waitingForFloorContinue = false;
            vm.ResetAll();
            return;
        }
        // Prepare for floor transition
        waitingForFloorStart = false;
        waitingForFloorContinue = true;
        vm.SetPhase(TourUIPhase.FloorTransition);
    }

    /// <summary>
    /// Binds this binder to the specified <see cref="FloorManager"/>,
    /// subscribing to its events and unhooking any previous manager.
    /// </summary>
    /// <param name="floorMgr">The floor manager to bind to.</param>
    private void SwapFM(FloorManager floorMgr)
    {
        // Unhook from previous FloorManager
        UnhookFM();
        // Hook to new FloorManager
        fm = floorMgr;
        // Subscribe to FloorManager events if valid
        if (fm == null)
            return;
        fm.AreaConfirmed += OnAreaConfirmed;
        fm.GuidingToNext += OnGuidingToNext;
    }

    /// <summary>
    /// Unhooks from the current <see cref="FloorManager"/>, if any.
    /// </summary>
    private void UnhookFM()
    {
        // Unsubscribe from FloorManager events if valid
        if (fm == null)
            return;
        fm.AreaConfirmed -= OnAreaConfirmed;
        fm.GuidingToNext -= OnGuidingToNext;
        fm = null;
    }

    /// <summary>
    /// Handles an area being confirmed by the <see cref="FloorManager"/>.
    /// Updates the view-model and plays any associated audio sequence.
    /// </summary>
    /// <param name="def">The confirmed area definition.</param
    private void OnAreaConfirmed(AreaDefinition def)
    {
        // Notify view-model of area entry
        if (tourRunner != null)
        {
            // Provide visit counts from tour runner
            vm.NotifyEnteredArea(def, tourRunner.VisitedAcrossTour, tourRunner.TotalAcrossTour);
            // Play associated audio clips if available
            if (def != null && def.AudioClips != null && def.AudioClips.Count > 0)
                AudioDirector.Instance.PlaySequence(def.AudioClips, 0.1f);
        }
        else
        {
            // No tour runner, notify with zero counts
            vm.NotifyEnteredArea(def, 0, 0);
        }
    }

    /// <summary>
    /// Handles the event when the system begins guiding to the next area.
    /// Updates the view-model and sets the phase to navigating.
    /// </summary>
    /// <param name="def">The next area definition.</param>
    private void OnGuidingToNext(AreaDefinition def)
    {
        // Notify view-model of guiding to next area
        vm.NotifyGuidingTo(def);
        // Update UI phase to navigating
        vm.SetPhase(TourUIPhase.Navigating);
    }

    /// <summary>
    /// Binds to the first available <see cref="PathProvider"/> and subscribes
    /// to path update events to keep the view-model distance updated.
    /// </summary>
    private void BindPathProvider()
    {
        // Avoid rebinding if already bound
        if (pathProvider)
            return;

        // Find MovementAgent if not already set
        if (movementAgent == null)
            movementAgent = FindFirstObjectByType<MovementAgent>(FindObjectsInactive.Include);

        // Try to get PathProvider from MovementAgent
        if (pathProvider == null)
            pathProvider = movementAgent.GetComponent<PathProvider>();

        // Fallback: Find any PathProvider in the scene
        if (!pathProvider)
        {
            // Attempt to find player GameObject
            var player = GameObject.FindWithTag("Player");
            if (player)
                // Attempt to get PathProvider from the player GameObject
                pathProvider = player.GetComponent<PathProvider>();
        }

        // Final fallback: Find any PathProvider in the scene
        if (pathProvider)
        {
            // Subscribe to path update events
            pathProvider.OnPathUpdated += OnPathUpdated;
            Debug.Log(
                $"[TourBinder] Subscribed to PathProvider #{pathProvider.GetInstanceID()} on {pathProvider.gameObject.name}"
            );
        }
        else
        {
            Debug.LogWarning("[TourBinder] No PathProvider found after floor load.");
        }
    }

    /// <summary>
    /// Unbinds from the current <see cref="PathProvider"/>, if any.
    /// </summary>
    private void UnbindPathProvider()
    {
        if (pathProvider)
        {
            // Unsubscribe from path update events
            pathProvider.OnPathUpdated -= OnPathUpdated;
            Debug.Log("[TourBinder] Unsubscribed from PathProvider");
            pathProvider = null;
        }
    }

    /// <summary>
    /// Binds to a <see cref="UWBPositioning"/> component and subscribes
    /// to its connection status changes.
    /// </summary>
    private void BindUWB()
    {
        // Avoid rebinding if already bound
        if (uwb)
            return;

        // Find MovementAgent if not already set
        if (movementAgent == null)
        {
            // Attempt to find MovementAgent in the scene
            movementAgent = FindFirstObjectByType<MovementAgent>(FindObjectsInactive.Include);
            Debug.Log("[TourBinder] Found MovementAgent");
        }

        // Try to get UWBPositioning from MovementAgent
        if (movementAgent)
        {
            // Attempt to get UWBPositioning component
            uwb = movementAgent.GetComponent<UWBPositioning>();
            Debug.Log("[TourBinder] Found UWBPositioning component");
        }
        // Fallback: Find any UWBPositioning in the scene
        if (uwb == null)
        {
            uwb = FindFirstObjectByType<UWBPositioning>(FindObjectsInactive.Include);
            Debug.Log("[TourBinder] Found UWBPositioning in scene");
        }

        // Subscribe to connection status changes
        if (uwb)
        {
            uwb.OnConnectionStatusChanged += HandleUWBConnectionChanged;
            Debug.Log("[TourBinder] Subscribed to UWBPositioning");
        }
    }

    /// <summary>
    /// Unbinds from the current <see cref="UWBPositioning"/>, if any,
    /// and stops tracking.
    /// </summary>
    private void UnbindUWB()
    {
        // Unsubscribe from UWBPositioning events if valid
        if (uwb == null)
            return;
        uwb.StopTracking();
        // Unsubscribe from connection status changes
        uwb.OnConnectionStatusChanged -= HandleUWBConnectionChanged;
        uwb = null;
    }

    /// <summary>
    /// Handles UWB connection status changes and updates the view-model
    /// connection state and phase accordingly.
    /// </summary>
    /// <param name="connected">Whether UWB is connected.</param>
    private void HandleUWBConnectionChanged(bool connected)
    {
        Debug.Log($"[TourBinder] UWB connection changed: {connected}");
        // Update connection state in view-model
        SetConnection(connected);
        // On reconnection during floor start, prompt user to start tour
        if (connected && waitingForFloorStart)
        {
            Debug.Log("[TourBinder] UWB reconnected, prompting user to start tour.");
            vm.SetPhase(TourUIPhase.ReadyPrompt);
        }
    }

    /// <summary>
    /// Handles path updates from the <see cref="PathProvider"/> and
    /// updates the distance in the view-model.
    /// </summary>
    /// <param name="path">The updated navigation mesh path.</param>
    private void OnPathUpdated(NavMeshPath path)
    {
        if (vm != null && pathProvider != null)
            // Update distance in view-model
            vm.SetDistance(pathProvider.CurrentDistance);
    }

    /// <summary>
    /// Attempts to hook to the currently active <see cref="FloorManager"/>
    /// in the scene and triggers floor loaded logic if found.
    /// </summary>
    private void HookToActiveFloorManager()
    {
        // Find active FloorManager in the scene
        var found = FindFirstObjectByType<FloorManager>(FindObjectsInactive.Include);
        if (found != null)
            // Trigger floor loaded logic
            OnFloorLoaded(found.Floor, found);
    }
}
