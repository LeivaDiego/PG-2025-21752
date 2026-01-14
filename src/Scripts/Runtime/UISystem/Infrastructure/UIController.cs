using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller for managing UI screens and their coordinators
/// </summary>
[RequireComponent(typeof(UIBootstrap))]
public sealed class UIController : MonoBehaviour
{
    private UIBootstrap bootstrap = null;
    private readonly Dictionary<Type, object> map = new();
    private IScreenView currentView;
    private object currentCoord;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIController"/> class.
    /// </summary>
    void Awake()
    {
        if (!bootstrap)
        {
            bootstrap = GetComponent<UIBootstrap>();
        }
        if (!bootstrap)
        {
            bootstrap = FindFirstObjectByType<UIBootstrap>();
        }
        if (!bootstrap)
        {
            Debug.LogError("[UIController] Missing UIBootstrap component.");
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// Maps screens to their coordinators and initializes the first screen.
    /// </summary>
    private void Start()
    {
        var vm = new TourViewModel();
        var binder = FindFirstObjectByType<TourBinder>(FindObjectsInactive.Include);
        if (binder != null)
        {
            binder.Init(vm);
        }
        else
        {
            Debug.LogError("[UIController] Missing TourBinder in scene.");
        }
        var r = bootstrap.Router;
        if (r == null)
        {
            Debug.LogError("[UIController] Missing UIRouter in UIBootstrap.");
            enabled = false;
            return;
        }
        map[typeof(HomeView)] = new HomeCoordinator(r);
        map[typeof(MinigamesView)] = new MinigamesCoordinator(r);
        map[typeof(OnboardingView)] = new OnboardingCoordinator(r, bootstrap.uiAtlas.OnboardingSet);
        map[typeof(BaseHUDView)] = new HUDCoordinator(
            bootstrap.Router,
            vm,
            binder,
            bootstrap.uiAtlas,
            bootstrap.audioAtlas
        );

        r.ScreenChanged += OnScreenChanged;

        if (r.CurrentScreenView != null)
        {
            OnScreenChanged(r.CurrentScreenView);
        }
    }

    /// <summary>
    /// Cleans up event subscriptions on destroy.
    /// </summary>
    void OnDestroy()
    {
        if (bootstrap && bootstrap.Router != null)
        {
            bootstrap.Router.ScreenChanged -= OnScreenChanged;
        }
        Detach();
    }

    /// <summary>
    /// Handles screen changes by detaching the current coordinator and attaching the new one.
    /// </summary>
    /// <param name="view">The new screen view.</param>
    private void OnScreenChanged(IScreenView view)
    {
        Detach();
        currentView = view;

        var t = view.GetType();

        if (map.TryGetValue(t, out var coord))
        {
            currentCoord = coord;
            coord.GetType().GetMethod("Attach")?.Invoke(coord, new object[] { view });
        }
    }

    /// <summary>
    /// Detaches the current coordinator from its view.
    /// </summary>
    private void Detach()
    {
        if (currentCoord == null)
            return;
        currentCoord.GetType().GetMethod("Detach")?.Invoke(currentCoord, null);
        currentCoord = null;
        currentView = null;
    }
}
