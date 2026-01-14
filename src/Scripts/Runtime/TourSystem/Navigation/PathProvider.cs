using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Provides NavMesh-based path computation between the player and a target area,
/// tracking distance and raising events when the path is updated.
/// </summary>
[DisallowMultipleComponent]
public class PathProvider : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float sampleRadius = 2f;

    [SerializeField]
    private float recomputeThreshold = 0.01f;

    [Header("Optional")]
    [Tooltip("Assign an Area GameObject in the scene to start with (uses its BoxCollider center).")]
    [SerializeField]
    private GameObject initialTarget;

    [SerializeField]
    private Transform playerPosition;

    // Path related
    public bool Paused;
    public NavMeshPath CurrentPath { get; private set; }
    public float CurrentDistance { get; private set; }

    // Positioning related values
    private Vector3 _lastPlayerPos = Vector3.positiveInfinity;
    private Vector3 _lastTargetPos = Vector3.positiveInfinity;
    private bool _hasTargetPoint = false;
    private Vector3 _targetPoint;

    /// <summary>
    /// Event raised whenever a path is updated. The argument is the new path, or null if no path is available.
    /// </summary>
    public event Action<NavMeshPath> OnPathUpdated;

    /// <summary>
    /// Initializes the NavMeshPath and attempts to locate the player transform if not explicitly assigned.
    /// </summary>
    private void Awake()
    {
        // Create new path
        CurrentPath = new NavMeshPath();

        // Validate user position
        if (playerPosition == null)
        {
            // Find the MovementAgent Object in the scene
            var movement = FindFirstObjectByType<MovementAgent>(FindObjectsInactive.Include);
            if (movement != null)
            {
                // Update the user position to the transform of the MovementAgent
                playerPosition = movement.transform;
            }
            else
            {
                // If no MovementAgent found, fallback to player tagged GameObject
                var tagged = GameObject.FindWithTag("Player");
                if (tagged != null)
                    // Update the user position to the transform of the GameObject with the "Player" tag
                    playerPosition = tagged.transform;
            }

            // Check if playerPosition was successfully assigned
            if (playerPosition == null)
                Debug.LogWarning("[PathProvider] No player position found. Distance will stay 0.");
        }
    }

    /// <summary>
    /// Optionally sets an initial target if one has been configured.
    /// </summary>
    private void Start()
    {
        if (initialTarget)
            SetTarget(initialTarget);
    }

    /// <summary>
    /// Monitors player and target movement, recomputing the path when either moves
    /// beyond the configured threshold and raising <see cref="OnPathUpdated"/>.
    /// </summary>
    private void Update()
    {
        // Check if the MovementAgent is paused or has no target
        if (Paused || !_hasTargetPoint)
            return;

        // Compute coordinates of both user and target
        Vector3 p = playerPosition ? playerPosition.position : transform.position;
        Vector3 t = _targetPoint;

        // Apply recompute threshold
        float threshSq = recomputeThreshold * recomputeThreshold;
        // Check if both player and target positions are inside the threshold
        if (
            (p - _lastPlayerPos).sqrMagnitude < threshSq
            && (t - _lastTargetPos).sqrMagnitude < threshSq
        )
            return;

        // Update last positions
        _lastPlayerPos = p;
        _lastTargetPos = t;

        // Compute path from user to target
        if (TryComputePath(p, t, out var path))
        {
            // Update path
            CurrentPath = path;
            CurrentDistance = ComputePathDistance(CurrentPath);
            OnPathUpdated?.Invoke(CurrentPath);
        }
        else
        {
            // Update path as null value
            CurrentPath = null;
            CurrentDistance = 0f;
            OnPathUpdated?.Invoke(null);
        }
    }

    /// <summary>
    /// Sets the target area GameObject, using its <see cref="AreaInstance"/> or <see cref="BoxCollider"/>
    /// as the destination point, and triggers a path recomputation.
    /// </summary>
    /// <param name="areaGO">The GameObject representing the target area.</param>
    public void SetTarget(GameObject areaGO)
    {
        if (!areaGO)
            return;

        var ai = areaGO.GetComponent<AreaInstance>();
        var box = ai ? ai.NavTarget : areaGO.GetComponent<BoxCollider>();
        if (!box)
        {
            Debug.LogWarning("[PathProvider] Target GameObject has no AreaInstance/BoxCollider.");
            return;
        }

        _targetPoint = box.transform.TransformPoint(box.center);
        _hasTargetPoint = true;
        ForceRecompute();
    }

    /// <summary>
    /// Clears the current target and resets path and distance information.
    /// </summary>
    public void ClearTarget()
    {
        // Reset current values
        _hasTargetPoint = false;
        CurrentPath = null;
        CurrentDistance = 0f;
        OnPathUpdated?.Invoke(null);
    }

    /// <summary>
    /// Forces the next Update to recompute the path, regardless of threshold.
    /// </summary>
    public void ForceRecompute()
    {
        // Set last known positions to infinite value
        _lastPlayerPos = Vector3.positiveInfinity;
        _lastTargetPos = Vector3.positiveInfinity;
    }

    /// <summary>
    /// Attempts to compute a NavMesh path between two world positions, sampling
    /// both endpoints onto the NavMesh.
    /// </summary>
    /// <param name="from">The starting world position.</param>
    /// <param name="to">The target world position.</param>
    /// <param name="path">The resulting computed path, if successful.</param>
    /// <returns>True if a valid path was found; otherwise false.</returns>
    private bool TryComputePath(Vector3 from, Vector3 to, out NavMeshPath path)
    {
        path = new NavMeshPath();
        // Attempt to compute path between user and target positions
        if (!NavMesh.SamplePosition(from, out var fromHit, sampleRadius, NavMesh.AllAreas))
            return false;
        if (!NavMesh.SamplePosition(to, out var toHit, sampleRadius, NavMesh.AllAreas))
            return false;
        if (!NavMesh.CalculatePath(fromHit.position, toHit.position, NavMesh.AllAreas, path))
            return false;

        // If succesfull, update status as valid
        return path.status != NavMeshPathStatus.PathInvalid;
    }

    /// <summary>
    /// Calculates the total distance along a NavMesh path by summing the distances
    /// between its corner points.
    /// </summary>
    /// <param name="path">The path whose distance should be measured.</param>
    /// <returns>Total distance in world units, or 0 if the path is null or too short.</returns>
    private float ComputePathDistance(NavMeshPath path)
    {
        // Check if path is invalid
        if (path == null || path.corners.Length < 2)
            return 0f;

        float dist = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            // Compute distance from user to target.
            dist += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return dist;
    }
}
