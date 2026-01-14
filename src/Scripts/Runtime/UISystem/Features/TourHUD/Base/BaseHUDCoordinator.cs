using UnityEngine;

/// <summary>
/// Coordinator for the base HUD during the tour
/// </summary>
public sealed class BaseHUDCoordinator : ICoordinator<BaseHUDView>
{
    private readonly UIRouter router;
    private readonly TourViewModel vm;
    private readonly MenuCoordinator menu;
    private readonly Sprite spinnerIcon;
    private readonly Sprite elevatorIcon;
    private readonly Sprite walkingIcon;
    private readonly Sprite doneIcon;
    private readonly Sprite startIcon;

    private BaseHUDView v;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseHUDCoordinator"/> class.
    /// </summary>
    /// <param name="r">The UI router.</param>
    /// <param name="model">The tour view model.</param>
    /// <param name="uiAtlas">The UI atlas containing HUD icons.</param>
    /// <param name="audioAtlas">The audio atlas for sound effects.</param>
    public BaseHUDCoordinator(
        UIRouter r,
        TourViewModel model,
        UIAtlas uiAtlas,
        AudioAtlas audioAtlas
    )
    {
        router = r;
        vm = model;
        spinnerIcon = uiAtlas.HudSpinnerIcon;
        elevatorIcon = uiAtlas.HudElevatorIcon;
        walkingIcon = uiAtlas.HudWalkingIcon;
        doneIcon = uiAtlas.HudDoneIcon;
        startIcon = uiAtlas.HudStartIcon;
        menu = new MenuCoordinator(r, uiAtlas, audioAtlas);
    }

    /// <summary>
    /// Attaches the base HUD view to the coordinator.
    /// </summary>
    /// <param name="view">The base HUD view to attach.</param>
    public void Attach(BaseHUDView view)
    {
        v = view;
        v.OnMenu += ShowMenu;
        vm.Changed += Apply;
        Apply();
    }

    /// <summary>
    /// Detaches the base HUD view from the coordinator.
    /// </summary>
    public void Detach()
    {
        if (v != null)
            v.OnMenu -= ShowMenu;
        vm.Changed -= Apply;
        v = null;
    }

    /// <summary>
    /// Applies the current state of the view model to the base HUD view.
    /// </summary>
    private void Apply()
    {
        if (v == null)
            return;

        var title = vm.Phase switch
        {
            TourUIPhase.WaitingForConnection => "Conectando…",
            TourUIPhase.ConnectionLost => "Reconectando…",
            TourUIPhase.ReadyPrompt => "Listo para empezar",
            TourUIPhase.Navigating => "En ruta",
            TourUIPhase.InAreaInfo => vm.CurrentArea,
            TourUIPhase.FloorTransition => "Cambiando de nivel",
            TourUIPhase.TourComplete => "Tour completado",
            _ => "",
        };
        v.SetTitle(title);
        Sprite icon = null;
        switch (vm.Phase)
        {
            case TourUIPhase.WaitingForConnection:
            case TourUIPhase.ConnectionLost:
                icon = spinnerIcon;
                break;
            case TourUIPhase.FloorTransition:
                icon = elevatorIcon;
                break;
            case TourUIPhase.Navigating:
                icon = walkingIcon;
                break;
            case TourUIPhase.TourComplete:
                icon = doneIcon;
                break;
            case TourUIPhase.InAreaInfo:
                icon = vm.CurrentAreaDef ? vm.CurrentAreaDef.AreaIcon : null; // per-area if set
                break;
            case TourUIPhase.ReadyPrompt:
                if (!vm.HasBegunTour)
                    icon = startIcon;
                break;
        }

        v.SetTitleIcon(icon);

        var showDir = vm.Phase == TourUIPhase.Navigating && !string.IsNullOrEmpty(vm.NextArea);
        v.SetDirections(showDir ? $"Dirígete a: {vm.NextArea}" : "", showDir);

        v.ShowFooter(vm.Phase == TourUIPhase.Navigating);

        v.SetProgress(vm.ProgressNormalized);
        v.SetDistance(vm.DistanceMeters);
    }

    /// <summary>
    /// Shows the menu.
    /// </summary>
    private void ShowMenu() => menu.Show();
}
