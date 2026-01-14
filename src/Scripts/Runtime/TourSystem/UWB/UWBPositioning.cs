using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handles continuous UWB-based positioning of a target transform,
/// including polling, NavMesh clamping, signal-loss detection,
/// and optional smooth movement.
/// </summary>
/// <remarks>
/// This class makes use of the UWBLocator class directly
/// </remarks>
[RequireComponent(typeof(Rigidbody))]
public class UWBPositioning : MonoBehaviour
{
    [Header("Polling")]
    [Tooltip("How often poll for a new position (0 = every frame. >0 = seconds).")]
    [SerializeField]
    private float pollIntervalSeconds = 0.25f;

    [Header("NavMesh Clamp")]
    [Tooltip("Radius to sample the NavMesh for valid positions.")]
    [SerializeField]
    private float navmeshSampleRadius = 2.0f;

    [Tooltip("Maximum radius to sample the NavMesh.")]
    [SerializeField]
    private float navmeshMaxSampleRadius = 10.0f;

    [Tooltip("Growth factor for NavMesh sampling radius (e.g. 2.0 = double each step).")]
    [SerializeField]
    private float navmeshRadiusGrowth = 2.0f;

    [Header("Movement")]
    [Tooltip("Whether to smoothly move towards the target position.")]
    [SerializeField]
    private bool smoothMove = false;

    [Tooltip("Speed of smoothing (higher = snappier).")]
    [SerializeField]
    private float smoothSpeed = 5f;

    [Header("Target")]
    [Tooltip("The player object to move")]
    [SerializeField]
    private Transform target;

    [Header("Signal Loss")]
    [Tooltip("How many consecutive nulls before declaring connection lost.")]
    [SerializeField]
    private int lostConnectionThreshold = 50;

    // Target related
    private Coroutine pollRoutine;
    private Vector3 currentGoal;
    private bool hasGoal = false;

    // Connection related
    bool connected = false;
    private int consecutiveNulls = 0;
    private bool lossDeclared = false;

    /// <summary>
    /// Event invoked when the UWB connection state changes
    /// </summary>
    public event Action<bool> OnConnectionStatusChanged;

    /// <summary>
    /// Initializes rigidbody and disables component when not running on iOS.
    /// </summary>
    private void Awake()
    {
#if UNITY_IOS && !UNITY_EDITOR
        // Validate target
        if (target == null)
            target = transform;

        // Configure Rigidbody component
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        gameObject.tag = "Player";
#else
        // Disable UWB usage on unsupported platform
        Debug.Log("[UWBPositioning] Disabled in non-iOS build.");
        enabled = false;
#endif
    }

    /// <summary>
    /// Automatically starts tracking when enabled (iOS only).
    /// </summary>
    private void OnEnable()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (pollRoutine == null)
            StartTracking();
#endif
    }

    /// <summary>
    /// Stops tracking on disable.
    /// </summary>
    private void OnDisable()
    {
        StopTracking();
    }

    /// <summary>
    /// Ensures tracking stops when destroyed.
    /// </summary>
    private void OnDestroy()
    {
        StopTracking();
    }

    /// <summary>
    /// Handles smooth movement interpolation when enabled.
    /// </summary>
    private void Update()
    {
        // Check if smooth movement is enabled
        if (smoothMove && hasGoal)
        {
            if (smoothSpeed <= 0f)
                return;

            // Calculate the next position towards the current goal
            Vector3 next = Vector3.MoveTowards(
                target.position,
                currentGoal,
                smoothSpeed * Time.deltaTime
            );
            // Update the position smoothly
            target.position = next;
            if ((next - currentGoal).sqrMagnitude < 0.0001f)
                hasGoal = false;
        }
    }

    /// <summary>
    /// Starts the UWB polling loop if not already running.
    /// </summary>
    public void StartTracking()
    {
        if (pollRoutine != null)
            return;
        Debug.Log("[UWBPositioning] Starting UWB tracking.");
        pollRoutine = StartCoroutine(PollLoop());
    }

    /// <summary>
    /// Stops the UWB polling loop if running.
    /// </summary>
    public void StopTracking()
    {
        // Check if there is any active coroutine
        if (pollRoutine == null)
            return;

        // Stop the coroutine
        Debug.Log("[UWBPositioning] Stopping UWB tracking.");
        StopCoroutine(pollRoutine);
        pollRoutine = null;
    }

    /// <summary>
    /// Main polling loop for retrieving UWB coordinates at configured intervals.
    /// </summary>
    private IEnumerator PollLoop()
    {
        Debug.Log("[UWBPositioning] Starting PollLoop.");
        // Check if there is a polling interval
        if (pollIntervalSeconds <= 0f)
        {
            while (true)
            {
                // Get the UWB coordinate
                TryStep();
                yield return null;
            }
        }
        else
        {
            // Get wait period before next step
            var wait = new WaitForSeconds(pollIntervalSeconds);
            while (true)
            {
                // Get the UWB coordinate
                TryStep();
                // Wait the specified seconds
                yield return wait;
            }
        }
    }

    /// <summary>
    /// Attempts to retrieve a UWB position, apply NavMesh clamping,
    /// update movement, and handle connection status.
    /// </summary>
    private void TryStep()
    {
        // Call the UWBLocator position method
        if (!UWBLocator.TryGetPosition(out var uwbWorld))
        {
            // If the position is invalid, register it
            Debug.LogWarning("[UWBPositioning] Failed to get UWB position.");
            RegisterNullFailure();
            return;
        }

        // Fresh reading
        consecutiveNulls = 0;

        // Check if the UWBLocator is currently disconnected
        if (!connected || lossDeclared)
        {
            // Set state to connected
            connected = true;
            lossDeclared = false;
            // Invoke connection status changed
            OnConnectionStatusChanged?.Invoke(true);
            Debug.Log("[UWBPositioning] UWB connected.");
        }

        // Compute clamped coordinate with the defined values
        Vector3 clamped = ClampToNavmesh(
            uwbWorld,
            navmeshSampleRadius,
            navmeshMaxSampleRadius,
            navmeshRadiusGrowth
        );

        if (smoothMove)
        {
            // If smooth movement enabled, update current target
            currentGoal = clamped;
            hasGoal = true;
        }
        else
        {
            // Move user to clamped position
            target.position = clamped;
            hasGoal = false;
        }
    }

    /// <summary>
    /// Registers a failure to retrieve UWB coordinates and checks for signal loss.
    /// </summary>
    private void RegisterNullFailure()
    {
        consecutiveNulls++;
        // Stale counter should not accumulate across nulls
        CheckForLoss("null");
    }

    /// <summary>
    /// Evaluates whether enough failures have occurred to declare signal loss.
    /// </summary>
    private void CheckForLoss(string reason)
    {
        // Count-based triggers
        bool hitNulls = consecutiveNulls >= Mathf.Max(1, lostConnectionThreshold);

        // Check if the UWBLocator is connected and there are null hits
        if (!lossDeclared && hitNulls)
        {
            // Declate connection loss
            lossDeclared = true;
            if (connected)
            {
                connected = false;
                OnConnectionStatusChanged?.Invoke(false);
            }
            Debug.LogWarning(
                $"[UWBPositioning] UWB connection lost ({reason}). Monitoring for recovery."
            );
        }
    }

    /// <summary>
    /// Attempts to find the nearest valid NavMesh position around a desired point,
    /// expanding the search radius until successful or max radius reached.
    /// </summary>
    /// <param name="desired">Desired world position.</param>
    /// <param name="startRadius">Initial sampling radius.</param>
    /// <param name="maxRadius">Maximum allowed radius.</param>
    /// <param name="growth">Radius growth multiplier.</param>
    /// <returns>Valid NavMesh position or the desired position if none found.</returns>
    private static Vector3 ClampToNavmesh(
        Vector3 desired,
        float startRadius,
        float maxRadius,
        float growth
    )
    {
        // Compute max values
        float r = Mathf.Max(0.01f, startRadius);
        float cap = Mathf.Max(r, maxRadius);
        float g = Mathf.Max(1.01f, growth);

        while (r <= cap)
        {
            // Check if received coordinate is inside navmesh, expanding radius if not
            if (NavMesh.SamplePosition(desired, out var hit, r, NavMesh.AllAreas))
                return hit.position;
            r *= g;
        }

        Debug.LogWarning(
            "[UWBPositioning] No NavMesh found within max radius. Using raw coordinate."
        );

        return desired;
    }
}
