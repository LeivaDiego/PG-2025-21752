using UnityEngine;

/// <summary>
/// Selects and manages the appropriate movement system depending on platform:
/// - KeyboardPositioning in the Unity Editor.
/// - UWBPositioning on iOS device builds.
/// Ensures only one movement method is enabled at runtime.
/// </summary>
/// <remarks>
/// Requires both Positioning Controllers
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(KeyboardPositioning))]
[RequireComponent(typeof(UWBPositioning))]
public class MovementAgent : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Keyboard movement (Editor only)")]
    [SerializeField]
    private KeyboardPositioning editorMover;

    [Tooltip("UWB positioning (iOS device only)")]
    [SerializeField]
    private UWBPositioning uwbMover;

    /// <summary>
    /// Indicates whether the active movement system is enabled
    /// based on platform and current configuration.
    /// </summary>
    public bool IsEnabled
    {
        get
        {
#if UNITY_EDITOR && !UNITY_IOS
            // Set KeyboardPositioning on Editor build
            return editorMover && editorMover.enabled;
#elif UNITY_IOS && !UNITY_EDITOR
            // Set UWBPositioning on iOS build
            return uwbMover && uwbMover.enabled;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Automatically assigns component references when the script is added or reset.
    /// </summary>
    private void Reset()
    {
        editorMover = GetComponent<KeyboardPositioning>();
        uwbMover = GetComponent<UWBPositioning>();
    }

    /// <summary>
    /// Ensures both movement systems start disabled.
    /// The correct one will be enabled explicitly at runtime.
    /// </summary>
    private void Awake()
    {
        SafeEnable(editorMover, false);
        SafeEnable(uwbMover, false);
    }

    /// <summary>
    /// Handles cleanup when the agent is disabled, stopping active movement systems
    /// according to the platform.
    /// </summary>
    private void OnDisable()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (uwbMover != null)
            uwbMover.StopTracking();
#elif UNITY_EDITOR && !UNITY_IOS
        if (editorMover != null)
            editorMover.enabled = false;
#else
        SafeEnable(uwbMover, false);
        SafeEnable(editorMover, false);
#endif
    }

    /// <summary>
    /// Enables or disables the appropriate movement method based on the platform.
    /// Keyboard movement is used in Editor, UWB positioning is used on iOS devices.
    /// </summary>
    public void Enable(bool on)
    {
#if UNITY_EDITOR
        // Enable KeyboardPositioning on Editor build
        SafeEnable(editorMover, on);
        Debug.Log("[MovementAgent] Keyboard Control ON (Editor)");
#elif UNITY_IOS && !UNITY_EDITOR
        // Enable UWBPositioning on iOS build, always
        SafeEnable(uwbMover, true);
        Debug.Log("[MovementAgent] UWB Positioning ON (iOS device)");
#else
        // On any other platform, disable both positioning controllers
        SafeEnable(uwbMover, false);
        SafeEnable(editorMover, false);
        Debug.Log("[MovementAgent] No movement enabled (unsupported platform)");
#endif
    }

    /// <summary>
    /// Safely enables or disables a behaviour component if it exists.
    /// </summary>
    /// <param name="b">The behaviour to toggle.</param>
    /// <param name="on">True to enable, false to disable.</param>
    private static void SafeEnable(Behaviour b, bool on)
    {
        if (b == null)
            return;
        b.enabled = on;
    }
}
