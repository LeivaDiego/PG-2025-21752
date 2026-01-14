using UnityEngine;

/// <summary>
/// Represents a placed instance of an <see cref="AreaDefinition"/> in the scene.
/// Provides trigger-based enter/exit notifications for detecting when players
/// move into or out of this area.
/// </summary>
/// <remarks>
/// Requires BoxCollider component
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class AreaInstance : MonoBehaviour
{
    [SerializeField]
    private AreaDefinition definition;

    private BoxCollider _box;

    public AreaDefinition Definition => definition;

    public BoxCollider NavTarget => _box;

    /// <summary>
    /// Event invoked when something enters this area's trigger.
    /// </summary>
    public event System.Action<AreaInstance> Entered;

    /// <summary>
    /// Event invoked when something exits this area's trigger.
    /// </summary>
    public event System.Action<AreaInstance> Exited;

    /// <summary>
    /// Initializes and configures the BoxCollider as a trigger.
    /// </summary>
    private void Awake()
    {
        // Get the BoxCollider component reference
        _box = GetComponent<BoxCollider>();
        // Set the collider as trigger
        _box.isTrigger = true;
    }

    /// <summary>
    /// Raises the <see cref="Entered"/> event.
    /// Intended to be called by area detection logic.
    /// </summary>
    internal void RaiseEntered() => Entered?.Invoke(this);

    /// <summary>
    /// Raises the <see cref="Exited"/> event.
    /// Intended to be called by area detection logic.
    /// </summary>
    internal void RaiseExited() => Exited?.Invoke(this);
}
