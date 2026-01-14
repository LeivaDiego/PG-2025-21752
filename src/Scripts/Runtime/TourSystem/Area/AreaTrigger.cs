using UnityEngine;

/// <summary>
/// Provides trigger-based detection for when the player enters or exits an area.
/// Wraps an <see cref="AreaInstance"/> and raises its enter/exit events when
/// a collider tagged "Player" crosses the trigger boundary.
/// </summary>
[RequireComponent(typeof(BoxCollider), typeof(AreaInstance))]
public class AreaTrigger : MonoBehaviour
{
    private AreaInstance _area;

    /// <summary>
    /// Initializes component references and ensures the BoxCollider operates as a trigger.
    /// </summary>
    private void Awake()
    {
        // Get the AreaInstance component reference
        _area = GetComponent<AreaInstance>();
        // Set the BoxCollider as trigger
        GetComponent<BoxCollider>().isTrigger = true;
    }

    /// <summary>
    /// Handles player entry into the area's trigger volume.
    /// Raises the <see cref="AreaInstance.Entered"/> event.
    /// </summary>
    /// <param name="other">The collider entering the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Check if theres another collier with the player tag present
        if (!other.CompareTag("Player"))
            return;
        Debug.Log("[AreaTrigger] Player entered area: " + _area.Definition.name);
        // Raise the entered event
        _area.RaiseEntered();
    }

    /// <summary>
    /// Handles player exit from the area's trigger volume.
    /// Raises the <see cref="AreaInstance.Exited"/> event.
    /// </summary>
    /// <param name="other">The collider exiting the trigger.</param>
    private void OnTriggerExit(Collider other)
    {
        // Check if theres another collier with the player tag present
        if (!other.CompareTag("Player"))
            return;
        Debug.Log("[AreaTrigger] Player exited area: " + _area.Definition.name);
        // Raise the exited event
        _area.RaiseExited();
    }
}
