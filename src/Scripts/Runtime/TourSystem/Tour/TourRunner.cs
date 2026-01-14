using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Coordinates the execution of a tour consisting of multiple floors,
/// handling scene loading/unloading, progress tracking, AR camera policy,
/// and communication with <see cref="FloorManager"/> instances.
/// </summary>
[DisallowMultipleComponent]
public sealed class TourRunner : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the <see cref="TourRunner"/> in the scene.
    /// </summary>
    public static TourRunner Instance { get; private set; }

    /// <summary>
    /// Invoked when a floor has been loaded and its <see cref="FloorManager"/> is ready.
    /// </summary>
    public event System.Action<FloorDefinition, FloorManager> FloorLoaded;

    /// <summary>
    /// Invoked just before a floor scene is unloaded.
    /// </summary>
    public event System.Action<FloorDefinition> FloorUnloaded;

    /// <summary>
    /// Invoked when the current tour has completed all floors.
    /// </summary>
    public event System.Action TourCompleted;

    [SerializeField]
    private TourDefinition expressTour;

    [SerializeField]
    private TourDefinition completeTour;

    [SerializeField]
    private ArrowSceneDefinition arrowSceneDef;

    [SerializeField]
    [Tooltip("If true, use AR mode for the tour.")]
    private bool useAR = false;

    [SerializeField, Tooltip("Disable floor scene cameras when AR is on.")]
    private bool disableFloorCamerasInAR = true;

    // Tour External references
    private TourDefinition currentTour;
    private int floorIndex = -1;
    private FloorManager activeFM;
    private string loadedScenePath;
    private Scene baseScene;

    // Tour Progress
    private int visitedAcrossTour;
    private int totalAcrossTour;

    // Tour state
    private bool isStopping;
    private bool waitingForUserToContinue;

    // Public Properties
    public int VisitedAcrossTour => visitedAcrossTour;
    public int TotalAcrossTour => totalAcrossTour;

    /// <summary>
    /// Gets the tour progress as a value between 0 and 1.
    /// </summary>
    public float Progress =>
        (totalAcrossTour > 0) ? (visitedAcrossTour / (float)totalAcrossTour) : 0f;

    public bool IsStopping => isStopping;
    public TourDefinition ExpressTour => expressTour;
    public TourDefinition CompleteTour => completeTour;
    public bool WaitingForUserToContinue => waitingForUserToContinue;
    public TourDefinition CurrentTour => currentTour;

    /// <summary>
    /// Initializes the singleton instance and caches the base scene.
    /// </summary>
    void Awake()
    {
        // Validate singleton instance
        if (Instance != null && Instance != this)
        {
            // Another instance exists, destroy this one
            Destroy(gameObject);
            return;
        }
        // Set the singleton instance
        Instance = this;
        // Activate the base scene
        baseScene = SceneManager.GetActiveScene();
        Debug.Log($"[TourRunner] Awake. Base scene: {baseScene.name}");
    }

    /// <summary>
    /// Selects the active tour definition and resets tour progress.
    /// </summary>
    /// <param name="tour">The tour definition to run.</param>
    public void SelectTour(TourDefinition tour)
    {
        // Reset state
        currentTour = tour;
        floorIndex = 0;
        visitedAcrossTour = 0;
        totalAcrossTour = (tour != null) ? tour.TotalAreasCount() : 0;
        var name = (tour != null) ? tour.TourName : "null";
        Debug.Log($"[TourRunner] Selected tour: {name} | Total areas: {totalAcrossTour}");
    }

    /// <summary>
    /// Begins running the selected tour, loading the first floor and (optionally) AR overlay.
    /// </summary>
    public void BeginTour()
    {
        // Validate tour selection
        if (
            currentTour == null
            || currentTour.OrderedFloors == null
            || currentTour.OrderedFloors.Count == 0
        )
        {
            Debug.LogError("[TourRunner] No tour/floors.");
            return;
        }
#if UNITY_EDITOR
        // Disable AR in Editor mode
        Debug.Log("[TourRunner] Running in Editor mode. AR features disabled.");
        useAR = false;
#endif
        // Load AR overlay scene if needed
        if (useAR)
        {
            // Load AR Arrow scene additively
            if (arrowSceneDef != null && !string.IsNullOrEmpty(arrowSceneDef.ScenePath))
            {
                // Check if already loaded
                var sc = SceneManager.GetSceneByPath(arrowSceneDef.ScenePath);
                if (!sc.IsValid() || !sc.isLoaded)
                {
                    // Not loaded yet, so load it
                    Debug.Log("[TourRunner] Loading AR Arrow scene additively...");
                    SceneManager.LoadSceneAsync(arrowSceneDef.ScenePath, LoadSceneMode.Additive);
                }
            }
            else
            {
                Debug.LogWarning("[TourRunner] ArrowSceneDefinition not assigned or empty path.");
            }
        }
        // Start loading the first floor
        StartCoroutine(LoadFloorAt(floorIndex));
    }

    /// <summary>
    /// Called by external UI or logic to continue the tour to the next floor,
    /// once the previous one has fully completed.
    /// </summary>
    public void ContinueToNextFloor()
    {
        // Ignore if not waiting
        if (!waitingForUserToContinue)
            return;
        // Proceed to load next floor
        waitingForUserToContinue = false;
        StartCoroutine(LoadFloorAt(floorIndex));
    }

    /// <summary>
    /// Stops the active tour, unloading any loaded floor scenes
    /// and optionally returning to a "home" state.
    /// </summary>
    /// <param name="returnToHome">Whether a home-state transition should occur.</param>
    public void StopTour(bool returnToHome)
    {
        // Ignore if already stopping
        isStopping = true;
        waitingForUserToContinue = false;

        // Proactively unhook before unload to stop late events
        if (activeFM)
        {
            activeFM.FloorCompleted -= OnFloorCompleted;
            activeFM.AreaConfirmed -= OnAreaConfirmed;
        }

        // Unload active floor scene if any
        if (!string.IsNullOrEmpty(loadedScenePath))
        {
            var prevFloor = currentTour?.OrderedFloors[floorIndex];
            if (prevFloor)
                FloorUnloaded?.Invoke(prevFloor);

            Debug.Log($"[TourRunner] Stopping tour. Unloading active scene: {loadedScenePath}");
            SceneManager.UnloadSceneAsync(loadedScenePath); // once
            loadedScenePath = null;
            activeFM = null;
        }

        // Reset state
        currentTour = null;
        floorIndex = -1;
        visitedAcrossTour = 0;
        totalAcrossTour = 0;

        isStopping = false;

        // Log completion
        if (returnToHome)
            Debug.Log("[TourRunner] Tour stopped. Returning to home state.");
    }

    /// <summary>
    /// Coroutine that loads and initializes the floor at the given index.
    /// Applies anchor maps, camera policy, and wires floor events.
    /// </summary>
    /// <param name="idx">Index of the floor in the current tour.</param>
    private IEnumerator LoadFloorAt(int idx)
    {
        // Get floor definition and validate
        var floor = currentTour.OrderedFloors[idx];
        if (!floor)
        {
            Debug.LogError("[TourRunner] Null floor asset.");
            yield break;
        }
        if (string.IsNullOrEmpty(floor.ScenePath))
        {
            Debug.LogError("[TourRunner] Floor.ScenePath empty.");
            yield break;
        }

        // Apply Anchor Map if available
        if (floor.TryGetAnchorMapText(out var json))
        {
#if UNITY_IOS && !UNITY_EDITOR
            // Apply anchor map to UWBLocator
            UWBLocator.SetAnchorMap(json);
            UWBLocator.Start();
#endif
            Debug.Log($"[TourRunner] Anchor map applied for floor '{floor.FloorName}'.");
        }
        else
        {
            Debug.LogError(
                $"[TourRunner] Floor '{floor.FloorName}' has no valid AnchorMap assigned."
            );
        }

        // Load floor scene additively
        var op = SceneManager.LoadSceneAsync(floor.ScenePath, LoadSceneMode.Additive);
        yield return op;

        // Validate scene loaded
        var scene = SceneManager.GetSceneByPath(floor.ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[TourRunner] Scene failed to load.");
            yield break;
        }

        // Cache loaded scene path
        loadedScenePath = floor.ScenePath;
        ApplyCameraPolicy(scene);

        // Find FloorManager in loaded scene
        activeFM = FindFirstObjectByType<FloorManager>(FindObjectsInactive.Include);
        if (!activeFM)
        {
            Debug.LogError("[TourRunner] FloorManager not found in scene.");
            yield break;
        }

        // Wire FloorManager events
        activeFM.FloorCompleted += OnFloorCompleted;
        activeFM.AreaConfirmed += OnAreaConfirmed;
        activeFM.GlobalVisited = visitedAcrossTour;
        activeFM.GlobalTotal = totalAcrossTour;

        // Notify listeners that floor is loaded
        FloorLoaded?.Invoke(currentTour.OrderedFloors[idx], activeFM);

        Debug.Log(
            $"[TourRunner] Floor loaded: {floor.FloorName}. FloorManager will wait for UserReady (R in Editor)."
        );
    }

    /// <summary>
    /// Event handler for when a floor has completed. Starts the unload-and-advance process.
    /// </summary>
    /// <param name="_">Floor Manager to unhook events from.</param>
    private void OnFloorCompleted(FloorManager _)
    {
        // Unhook events
        if (activeFM)
        {
            activeFM.FloorCompleted -= OnFloorCompleted;
            activeFM.AreaConfirmed -= OnAreaConfirmed;
        }
        // Start unload and advance coroutine
        StartCoroutine(UnloadAndAdvance());
    }

    /// <summary>
    /// Event handler for area confirmation. Updates global tour progress.
    /// </summary>
    /// <param name="_">Area Definition to unhook events from.</param>
    private void OnAreaConfirmed(AreaDefinition _)
    {
        // Update global progress
        visitedAcrossTour++;
        Debug.Log($"[TourRunner] Global progress: ({Progress * 100:F2}%)");
    }

    /// <summary>
    /// Coroutine that unloads the current floor scene and advances the floor index.
    /// When all floors are complete, raises <see cref="TourCompleted"/>.
    /// </summary>
    private IEnumerator UnloadAndAdvance()
    {
        // Unload current floor scene if any
        if (!string.IsNullOrEmpty(loadedScenePath))
        {
            Debug.Log($"[TourRunner] Unloading scene: {loadedScenePath}");
            // Notify listeners before unload
            var prevFloor = currentTour?.OrderedFloors[floorIndex];
            if (prevFloor)
            {
                // Invoke FloorUnloaded event
                FloorUnloaded?.Invoke(prevFloor);
            }
            // Unload scene asynchronously
            var op = SceneManager.UnloadSceneAsync(loadedScenePath);
            if (op != null)
                yield return op;

            // Clear references
            loadedScenePath = null;
            activeFM = null;
        }

        // Wait a frame to ensure unload completes
        yield return null;

        // Advance floor index
        floorIndex++;
        // Check if tour is complete
        if (currentTour == null || floorIndex >= currentTour.OrderedFloors.Count)
        {
            // Tour complete, raise event
            Debug.Log("[TourRunner] Tour complete.");
            TourCompleted?.Invoke();
            yield break;
        }

        // Wait for user to continue to next floor
        waitingForUserToContinue = true;
        Debug.Log("[TourRunner] Waiting for user to continue to next floor.");
    }

    /// <summary>
    /// Applies camera and audio listener policy for a newly loaded floor scene,
    /// respecting AR vs non-AR mode.
    /// </summary>
    /// <param name="floorScene">The floor scene to apply camera policy to.</param>
    private void ApplyCameraPolicy(Scene floorScene)
    {
        // When AR is off, just ensure floor cameras are enabled.
        if (!useAR)
        {
            EnableCamerasInScene(floorScene, enable: true);
            EnsureSingleAudioListener();
            return;
        }

        // AR is on: disable floor cameras if configured
        if (disableFloorCamerasInAR)
            EnableCamerasInScene(floorScene, enable: false);

        // Ensure only one AudioListener remains globally
        EnsureSingleAudioListener();
    }

    /// <summary>
    /// Enables or disables all cameras under the root GameObjects of a scene.
    /// </summary>
    /// <param name="scene">The scene whose cameras should be toggled.</param>
    /// <param name="enable">True to enable cameras; false to disable them.</param>
    private void EnableCamerasInScene(Scene scene, bool enable)
    {
        // Iterate through root GameObjects and toggle cameras
        foreach (var root in scene.GetRootGameObjects())
        {
            // Toggle all Camera components in children
            foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                cam.enabled = enable;
        }
    }

    /// <summary>
    /// Ensures there is a single active <see cref="AudioListener"/> in the project,
    /// preferring the one in the base scene and disabling the rest.
    /// </summary>
    private void EnsureSingleAudioListener()
    {
        // Collect listeners in base scene
        AudioListener baseSceneListener = null;
        baseSceneListener = FindFirstObjectByType<AudioListener>(FindObjectsInactive.Include);

        // Validate base scene listener
        if (baseSceneListener == null)
        {
            Debug.LogWarning("[TourRunner] No AudioListener found in base scene.");
            return;
        }

        // Disable all listeners not in base scene
        var all = FindObjectsByType<AudioListener>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        // Disable all listeners not in base scene
        foreach (var al in all)
        {
            if (al == baseSceneListener)
                continue;
            if (al.enabled)
            {
                al.enabled = false;
                Debug.Log($"[TourRunner] Disabled extra AudioListener on {al.gameObject.name}");
            }
        }
    }
}
