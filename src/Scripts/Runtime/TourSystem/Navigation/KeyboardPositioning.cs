using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Provides simple keyboard-driven movement for testing player navigation
/// in environments without UWB positioning.
/// Enabled only when not running inside the Unity Editor.
/// </summary>
/// <remarks>
/// Requires a Rigidbody component
/// </remakrs>
[RequireComponent(typeof(Rigidbody))]
public class KeyboardPositioning : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Speed in meters/second")]
    [SerializeField]
    private float moveSpeed = 2f;

    [Header("Target")]
    [Tooltip("The player object to move")]
    [SerializeField]
    private Transform target;

    /// <summary>
    /// Initializes the movement target and configures the Rigidbody.
    /// Disables the component in the Unity Editor, since this controller
    /// is intended only for Editor builds.
    /// </summary>
    private void Awake()
    {
#if UNITY_EDITOR
        Debug.Log("[KeyboardPositioning] Enabled in non-Editor build.");
        enabled = false;
#else
        // Apply new target
        if (target == null)
            target = transform;
        // Get the Rigidbody component reference
        var rb = GetComponent<Rigidbody>();
        // Configure the Rigidbody component
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        gameObject.tag = "Player";
#endif
    }

    /// <summary>
    /// Reads WASD/arrow key input and moves the target accordingly.
    /// Does nothing if no keyboard is detected or no movement keys are pressed.
    /// </summary>
    private void Update()
    {
        if (Keyboard.current == null)
            return;

        int h = 0;
        int v = 0;

        // Checks for WASD/Arrow keys press event to move the user
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            h -= 1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            h += 1;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            v -= 1;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            v += 1;

        if (h == 0 && v == 0)
            return;

        // Computes a normalized coordinate based on keyboard movement
        Vector3 dir = new Vector3(h, 0f, v).normalized;
        // Updates user position
        target.position += moveSpeed * Time.deltaTime * dir;
    }
}
