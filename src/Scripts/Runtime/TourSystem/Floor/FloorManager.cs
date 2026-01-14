using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the progression through a floor's areas, tracking visited areas,
/// handling entry confirmation, and coordinating movement and path rendering
/// for guiding the player from area to area.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(400)]
public class FloorManager : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField]
    private FloorDefinition floor;

    [Tooltip("Seconds the player must remain inside an area to confirm entry.")]
    [SerializeField]
    private float enterConfirmTime = 0.75f;

    // External references
    private AreaRegistry registry;
    private PathProvider pathProvider;
    private MovementAgent movementAgent;

    // Lists and sets
    private readonly HashSet<AreaDefinition> _visited = new();
    private readonly HashSet<AreaInstance> _inside = new();
    private readonly List<GameObject> _sequence = new();

    // Candidate related
    private int _currentIndex = -1;
    private AreaInstance _candidate;
    private float _candidateStart;
    private bool _started;

    /// <summary>
    /// Invoked when the floor is completed (no more areas to visit).
    /// </summary>
    public System.Action<FloorManager> FloorCompleted;

    /// <summary>
    /// Invoked when an area has been confirmed as entered (after linger time).
    /// </summary>
    public event System.Action<AreaDefinition> AreaConfirmed;

    /// <summary>
    /// Invoked when the system begins guiding the player to the next area.
    /// </summary>
    public event System.Action<AreaDefinition> GuidingToNext;

    // Floor state values
    public int CurrentIndex => _currentIndex;
    public bool Started => _started;

    /// <summary>
    /// Gets the <see cref="FloorDefinition"/> used by this manager.
    /// </summary>
    public FloorDefinition Floor => floor;

    /// <summary>
    /// Gets or sets the total number of areas visited across all floors (global metric).
    /// </summary>
    public int GlobalVisited { get; set; }

    /// <summary>
    /// Gets or sets the global total number of areas across all floors.
    /// </summary>
    public int GlobalTotal { get; set; }

    /// <summary>
    /// Initializes references, builds the area sequence, and prepares movement and path systems.
    /// </summary>
    private void Start()
    {
        // Check if Floor Definition is present
        if (!floor)
        {
            Debug.LogError("[FloorManager] Missing FloorDefinition.", this);
            enabled = false;
            return;
        }
        // Find the Area Registry Object in the scene, including inactive objects
        registry = registry
            ? registry
            : FindFirstObjectByType<AreaRegistry>(FindObjectsInactive.Include);

        // Find the Path Provider Object in the scene, including inactive objects
        pathProvider = pathProvider
            ? pathProvider
            : FindFirstObjectByType<PathProvider>(FindObjectsInactive.Include);

        // Find the Movement Agent Object in the scene, including inactive objects
        movementAgent = movementAgent
            ? movementAgent
            : FindFirstObjectByType<MovementAgent>(FindObjectsInactive.Include);

        // Check if any of the essential references are missing
        if (!registry || !pathProvider || !movementAgent)
        {
            Debug.LogError("[FloorManager] Missing registry/path/movement references.", this);
            enabled = false;
            return;
        }
        // Refresh the Area Registry
        registry.Refresh();

        // Build area sequence and wire event handlers
        BuildSequence();
        WireAreaEvents(true);

        // Iterate over each area present in the scene
        foreach (var go in registry.AllObjects)
            if (go)
                // Disable all areas
                go.SetActive(false);

        // Disable the Movement Agent, pause the Path Provider
        movementAgent.Enable(false);
        pathProvider.Paused = true;
        pathProvider.ClearTarget();
        Debug.Log($"[FloorManager] Floor Initialization Complete. Waiting for UserReady().");
    }

    /// <summary>
    /// Unsubscribes from area events when this manager is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        WireAreaEvents(false);
    }

    /// <summary>
    /// Handles developer shortcuts (Editor only) and confirms area entries after
    /// the required linger time has elapsed inside an area.
    /// </summary>s
    private void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            // If n key pressed on editor, call Next()
            Next();
        }
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            // If r key pressed on editor, call UserReady()
            UserReady();
        }
#endif
        if (!_started)
            return;

        // Check if the candidate is not empty
        if (_candidate != null)
        {
            // Check if there is any candidate in the active scene
            if (_inside.Contains(_candidate))
            {
                if (Time.time - _candidateStart >= enterConfirmTime)
                {
                    // If user is inside area enough time, confirm the area
                    ConfirmArea(_candidate);
                    _candidate = null; // reset the candidate
                }
            }
            else
            {
                _candidate = null; // reset the candidate if user left the area
            }
        }
    }

    /// <summary>
    /// Signals that the user is ready to start the floor. Enables movement,
    /// activates the first area, and immediately confirms it as entered.
    /// </summary>
    public void UserReady()
    {
        if (_started)
            return;
        // flag the floor as started
        _started = true;

        // Enable the Movement Agent
        movementAgent.Enable(true);

        // If there is any area left in sequence
        if (_sequence.Count > 0)
        {
            // Activate the next area in the sequence
            ActivateAreaIndex(0);
            // Auto Confirm the first area in the entire sequence
            var firstGO = _sequence[0];
            var firstPOI = firstGO.GetComponent<AreaInstance>();
            ConfirmArea(firstPOI);

            Debug.Log("[FloorManager] UserReady → guiding to first area.");
        }
        else
        {
            Debug.LogWarning("[FloorManager] No first area found.", this);
        }
    }

    /// <summary>
    /// Advances to the next area in the sequence, or completes the floor if no more areas remain.
    /// </summary>
    public void Next()
    {
        if (!_started)
            return;

        // Compute next area id
        int nextIdx = _currentIndex + 1;

        if (nextIdx >= _sequence.Count)
        {
            // If the next area id is higher than the sequence, complete floor
            CompleteFloor();
            return;
        }

        // Activate the next area in the sequence
        ActivateAreaIndex(nextIdx);

        // Set the next Game Object
        var nextGO = _sequence[nextIdx];

        if (!nextGO)
        {
            Debug.LogWarning("[FloorManager] Next area GameObject missing.", this);
            return;
        }
        // Get the AreaInstance component reference in scene
        var nextPOI = nextGO.GetComponent<AreaInstance>();
        GuidingToNext?.Invoke(nextPOI ? nextPOI.Definition : null);
        // Compute new path to next area
        pathProvider.SetTarget(nextGO);
        pathProvider.Paused = false;

        Debug.Log(
            nextIdx < _sequence.Count
                ? $"[FloorManager] Next → guiding to index {nextIdx} ({nextGO.name})."
                : "[FloorManager] Next → no more areas, completing floor."
        );
    }

    /// <summary>
    /// Builds the ordered sequence of area GameObjects for this floor using the registry.
    /// </summary>
    private void BuildSequence()
    {
        // Reset the sequene
        _sequence.Clear();
        // Iterate over each Game Object
        foreach (var go in registry.ForFloor(floor))
            _sequence.Add(go); // add to the sequence
        // Validate sequence is not empty
        if (_sequence.Count == 0)
            Debug.LogWarning("[FloorManager] Floor has zero resolved areas in this scene.", this);
    }

    /// <summary>
    /// Subscribes or unsubscribes this manager to all <see cref="AreaInstance"/> enter/exit events.
    /// </summary>
    /// <param name="on">True to subscribe, false to unsubscribe.</param>
    private void WireAreaEvents(bool on)
    {
        // Iterate over each Area in the Area Registry
        foreach (var go in registry.AllObjects)
        {
            // Skip if invalid GameObject
            if (!go)
                continue;
            // Get the Area Instance Game Object reference
            var ai = go.GetComponent<AreaInstance>();
            // Skip if invalid Area Instance
            if (!ai)
                continue;
            // Handle event subscriptions when area activated
            if (on)
            {
                ai.Entered += HandleEntered;
                ai.Exited += HandleExited;
            }
            else
            {
                ai.Entered -= HandleEntered;
                ai.Exited -= HandleExited;
            }
        }
    }

    /// <summary>
    /// Handles notification that the player has entered an area trigger.
    /// Begins tracking linger time for area confirmation.
    /// </summary>
    /// <param name="ai">The area instance entered.</param>
    private void HandleEntered(AreaInstance ai)
    {
        // Skip if floor hasnt started
        if (!_started)
            return;
        // Skip if no Area Instance nor Area Definition provided
        if (!ai || !ai.Definition)
            return;
        // Skip if Area already visited
        if (_visited.Contains(ai.Definition))
            return;
        // Start event timer
        _inside.Add(ai);
        _candidate = ai;
        _candidateStart = Time.time;
    }

    /// <summary>
    /// Handles notification that the player has exited an area trigger.
    /// Cancels any active candidate for confirmation if it matches.
    /// </summary>
    /// <param name="ai">The area instance exited.</param>
    private void HandleExited(AreaInstance ai)
    {
        if (!ai)
            return;
        _inside.Remove(ai);
        // Reset candidate if it matches the exited area
        if (_candidate == ai)
            _candidate = null;
    }

    /// <summary>
    /// Confirms that an area has been entered, marks it visited, updates pathing,
    /// and optionally activates the next area in the sequence.
    /// </summary>
    /// <param name="ai">The area instance being confirmed.</param>
    private void ConfirmArea(AreaInstance ai)
    {
        // Get the Area Definition
        var def = ai.Definition;
        // Skip if invalid
        if (def == null)
            return;

        // Mark as visited and update index
        _visited.Add(def);
        _currentIndex = floor.IndexOf(def);

        // Pause path computing and reset target
        pathProvider.Paused = true;
        pathProvider.ClearTarget();

        // Disable the visited area
        ai.gameObject.SetActive(false);

        // Invoke confirmation event
        AreaConfirmed?.Invoke(def);

        // Get next area id and activate the area
        int nextIdx = _currentIndex + 1;
        if (nextIdx < _sequence.Count)
            ActivateAreaIndex(nextIdx);

        Debug.Log($"[FloorManager] ENTERED area '{def.AreaName}'.");
    }

    /// <summary>
    /// Disables movement and path guidance, and invokes the floor completion callback.
    /// </summary>
    private void CompleteFloor()
    {
        movementAgent.Enable(false);
        pathProvider.Paused = true;
        pathProvider.ClearTarget();

        Debug.Log("[FloorManager] Floor completed. Waiting for TourRunner to unload scene.");
        FloorCompleted?.Invoke(this);
    }

    /// <summary>
    /// Gets the <see cref="AreaInstance"/> at the given index in the floor sequence.
    /// </summary>
    /// <param name="index">The index of the area in the sequence.</param>
    /// <returns>The area instance at that index, or null if invalid.</returns>
    private AreaInstance GetAreaInstanceAt(int index)
    {
        if (index < 0 || index >= _sequence.Count)
            return null;
        var go = _sequence[index];
        return go ? go.GetComponent<AreaInstance>() : null;
    }

    /// <summary>
    /// Deactivates all registered areas, then activates the area at the specified index.
    /// </summary>
    /// <param name="idx">The index of the area to activate.</param>
    private void ActivateAreaIndex(int idx)
    {
        // Iterate over each Area in Area Registry
        foreach (var go in registry.AllObjects)
            if (go)
                // Deactivate all registered areas
                go.SetActive(false);

        // Get the area instance reference and activate it
        var ai = GetAreaInstanceAt(idx);
        if (ai && ai.gameObject)
            ai.gameObject.SetActive(true);
    }
}
