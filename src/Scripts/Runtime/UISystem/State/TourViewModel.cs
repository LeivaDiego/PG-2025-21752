using System;
using UnityEngine;

/// <summary>
/// Represents the different UI phases of a tour.
/// </summary>
public enum TourUIPhase
{
    WaitingForConnection,
    ConnectionLost,
    ReadyPrompt,
    Navigating,
    InAreaInfo,
    FloorTransition,
    TourComplete,
}

/// <summary>
/// View-model containing UI-facing state for a running tour.
/// Responsible for exposing read-only properties and raising events when state changes.
/// </summary>
public sealed class TourViewModel
{
    // Public read-only properties
    public string CurrentArea { get; private set; } = "";
    public string NextArea { get; private set; } = "";
    public float ProgressNormalized { get; private set; } = 0f;
    public float DistanceMeters { get; private set; } = 0f;
    public bool Connected { get; private set; } = false;
    public bool Paused { get; private set; } = true;
    public bool HasBegunTour { get; private set; } = false;
    public TourUIPhase Phase { get; private set; } = TourUIPhase.WaitingForConnection;

    // Definitions for current and next areas
    public AreaDefinition CurrentAreaDef { get; private set; }
    public AreaDefinition NextAreaDef { get; private set; }
    public FloorDefinition CurrentFloor { get; private set; }

    /// <summary>
    /// Event raised when any state changes.
    /// </summary>
    public event Action Changed;

    /// <summary>
    /// Event raised when the user enters a new area.
    /// </summary>
    public event Action<AreaDefinition> EnteredArea;

    /// <summary>
    /// Event raised when guiding to a new area.
    /// </summary>
    public event Action<AreaDefinition> GuidingTo;

    /// <summary>
    /// Event raised when a new floor begins.
    /// </summary>
    public event Action<FloorDefinition> FloorBegan;

    /// <summary>
    /// Event raised when a floor ends.
    /// </summary>
    public event Action<FloorDefinition> FloorEnded;

    /// <summary>
    /// Event raised when the tour is completed.
    /// </summary>
    public event Action TourCompletedEvent;

    /// <summary>
    /// Event raised when the connection is lost.
    /// </summary>
    public event Action ConnectionLost;

    /// <summary>
    /// Event raised when the connection is restored.
    /// </summary>
    public event Action ConnectionRestored;

    /// <summary>
    /// Sets the current UI phase of the tour.
    /// Raises <see cref="Changed"/> if the phase changes.
    /// </summary>
    /// <param name="phase">The new tour UI phase.</param>
    public void SetPhase(TourUIPhase p)
    {
        // Only update if the phase is different
        if (Phase == p)
            return;
        // Update phase and notify listeners
        Phase = p;
        Changed?.Invoke();
    }

    /// <summary>
    /// Sets the connection state for the tour.
    /// Raises <see cref="ConnectionRestored"/> or <see cref="ConnectionLost"/> accordingly,
    /// and always raises <see cref="Changed"/> if the value changes.
    /// </summary>
    /// <param name="connected">Whether the system is connected.</param>
    public void SetConnected(bool on)
    {
        // Only update if the connection state is different
        if (Connected == on)
            return;
        // Update connection state and notify listeners
        Connected = on;
        if (on)
            // Notify about restored connection
            ConnectionRestored?.Invoke();
        else
            // Notify about lost connection
            ConnectionLost?.Invoke();
        // Notify about state change
        Changed?.Invoke();
    }

    /// <summary>
    /// Sets the paused state of the tour.
    /// Raises <see cref="Changed"/> if the value changes.
    /// </summary>
    /// <param name="paused">Whether the tour is paused.</param>
    public void SetPaused(bool paused)
    {
        // Only update if the paused state is different
        if (Paused == paused)
            return;
        // Update paused state and notify listeners
        Paused = paused;
        Changed?.Invoke();
    }

    /// <summary>
    /// Sets the distance remaining in meters.
    /// Clamps the value to be non-negative.
    /// Raises <see cref="Changed"/> if the value changes.
    /// </summary>
    /// <param name="meters">The new distance in meters.</param>
    public void SetDistance(float meters)
    {
        // Clamp to non-negative values
        meters = Mathf.Max(0f, meters);
        // Only update if the distance is different
        if (Mathf.Approximately(DistanceMeters, meters))
            return;
        // Update distance and notify listeners
        DistanceMeters = meters;
        Changed?.Invoke();
    }

    /// <summary>
    /// Sets the normalized progress value in the range [0, 1].
    /// Raises <see cref="Changed"/> if the value changes.
    /// </summary>
    /// <param name="progressValue">The normalized progress value.</param>
    public void SetProgress(float progressValue)
    {
        // Clamp to the range [0, 1]
        progressValue = Mathf.Clamp01(progressValue);
        // Only update if the progress is different
        if (Mathf.Approximately(ProgressNormalized, progressValue))
            return;
        // Update progress and notify listeners
        ProgressNormalized = progressValue;
        Changed?.Invoke();
    }

    /// <summary>
    /// Updates the normalized progress based on visited and total counts.
    /// Delegates to <see cref="SetProgress(float)"/>.
    /// </summary>
    /// <param name="visited">Number of visited items.</param>
    /// <param name="total">Total number of items.</param>
    public void UpdateProgress(int visited, int total)
    {
        // Calculate normalized progress and update
        float v = (total > 0) ? visited / (float)total : 0f;
        SetProgress(v);
    }

    /// <summary>
    /// Sets the current floor definition.
    /// Raises <see cref="FloorBegan"/> for the new floor, and <see cref="Changed"/> if the floor changes.
    /// </summary>
    /// <param name="floor">The floor that has begun.</param>
    public void SetCurrentFloor(FloorDefinition f)
    {
        // Only update if the floor is different
        if (CurrentFloor == f)
            return;
        // Update current floor and notify listeners
        CurrentFloor = f;
        FloorBegan?.Invoke(f);
        Changed?.Invoke();
    }

    /// <summary>
    /// Notifies that the current floor has ended.
    /// Raises <see cref="FloorEnded"/> and <see cref="Changed"/>.
    /// </summary>
    /// <param name="floor">The floor that has ended.</param>
    public void NotifyFloorEnded(FloorDefinition f)
    {
        // Notify listeners that the floor has ended
        FloorEnded?.Invoke(f);
        Changed?.Invoke();
    }

    /// <summary>
    /// Notifies that guidance is being given toward the specified area.
    /// Updates <see cref="NextAreaDef"/> and <see cref="NextArea"/>,
    /// and raises <see cref="GuidingTo"/> and <see cref="Changed"/>.
    /// </summary>
    /// <param name="next">The area being guided to.</param>
    public void NotifyGuidingTo(AreaDefinition next)
    {
        // Update next area definition and name
        NextAreaDef = next;
        NextArea = next ? next.AreaName : "";
        // Notify listeners about guiding to new area
        GuidingTo?.Invoke(next);
        Changed?.Invoke();
    }

    /// <summary>
    /// Notifies that an area has been entered.
    /// Updates current area state, progress, and raises <see cref="EnteredArea"/> and <see cref="Changed"/>.
    /// </summary>
    /// <param name="definition">The area that was entered.</param>
    /// <param name="visited">Visited count for progress.</param>
    /// <param name="total">Total count for progress.</param>
    public void NotifyEnteredArea(AreaDefinition def, int visited, int total)
    {
        // Update current area definition and name
        CurrentAreaDef = def;
        CurrentArea = def ? def.AreaName : "";
        // Update progress based on visited and total
        UpdateProgress(visited, total);
        // Notify listeners about entered area
        EnteredArea?.Invoke(def);
        Changed?.Invoke();
    }

    /// <summary>
    /// Notifies that the tour has been completed.
    /// Sets the phase to <see cref="TourUIPhase.TourComplete"/>,
    /// raises <see cref="TourCompletedEvent"/> and <see cref="Changed"/>.
    /// </summary>
    public void NotifyTourCompleted()
    {
        // Update phase and notify listeners
        SetPhase(TourUIPhase.TourComplete);
        TourCompletedEvent?.Invoke();
        Changed?.Invoke();
    }

    /// <summary>
    /// Marks that the tour has begun.
    /// Raises <see cref="Changed"/> if the value transitions from false to true.
    /// </summary>
    public void MarkTourBegan()
    {
        // Only update if the tour hasn't begun yet
        if (HasBegunTour)
            return;
        // Update state and notify listeners
        HasBegunTour = true;
        Changed?.Invoke();
    }

    /// <summary>
    /// Resets the entire view-model state back to its initial values.
    /// Raises <see cref="Changed"/> after resetting.
    /// </summary>
    public void ResetAll()
    {
        // Reset all state to initial values
        CurrentArea = NextArea = "";
        CurrentAreaDef = NextAreaDef = null;
        CurrentFloor = null;
        ProgressNormalized = 0f;
        DistanceMeters = 0f;
        Connected = false;
        Paused = true;
        HasBegunTour = false;
        // Reset phase to waiting for connection
        Phase = TourUIPhase.WaitingForConnection;
        // Notify listeners about state change
        Changed?.Invoke();
    }
}
