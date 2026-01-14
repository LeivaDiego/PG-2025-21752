using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maintains a mapping between <see cref="AreaDefinition"/> assets and their
/// instantiated <see cref="AreaInstance"/> GameObjects found in the scene.
/// Supports lookup, validation, and floor-based iteration.
/// </summary>
[DisallowMultipleComponent]
public class AreaRegistry : MonoBehaviour
{
    // Dictionary with all the AreaDefinition to GameObject mappings
    private readonly Dictionary<AreaDefinition, GameObject> _byDef = new();

    /// <summary>
    /// Gets all registered area GameObjects currently tracked by the registry.
    /// </summary>
    public IReadOnlyCollection<GameObject> AllObjects => _byDef.Values;

    /// <summary>
    /// Scans the scene for all <see cref="AreaInstance"/> objects (including inactive ones)
    /// and rebuilds the mapping from <see cref="AreaDefinition"/> to GameObject.
    /// </summary>
    public void Refresh()
    {
        // Find any objects present in the scene of AreaInstance type
        var found = FindObjectsByType<AreaInstance>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        // Clear dictionary
        _byDef.Clear();

        // Iterate over each AreaInstance found in scene
        foreach (var ai in found)
        {
            // If the AreaInstance is invalid or missing a definition, skip it
            if (!ai || !ai.Definition)
                continue;

            // Check for duplicate AreaDefinition entries
            if (_byDef.TryGetValue(ai.Definition, out var existing) && existing != ai.gameObject)
            {
                Debug.LogWarning(
                    $"[AreaRegistry] Duplicate AreaDefinition '{ai.Definition.name}'. Using first.",
                    ai
                );
                continue;
            }
            // Add the area instance to the Dictionary
            _byDef[ai.Definition] = ai.gameObject;
        }
    }

    /// <summary>
    /// Attempts to retrieve the instantiated area GameObject for the given definition.
    /// </summary>
    /// <param name="def">The area definition to look up.</param>
    /// <param name="areaGO">The corresponding area GameObject if found.</param>
    /// <returns>True if found; otherwise false.</returns>
    public bool TryGet(AreaDefinition def, out GameObject areaGO) =>
        _byDef.TryGetValue(def, out areaGO);

    /// <summary>
    /// Returns the ordered list of area GameObjects for a specified floor,
    /// logging warnings for any missing instances.
    /// </summary>
    /// <param name="floor">The floor containing an ordered list of area definitions.</param>
    /// <returns>An enumerable sequence of existing area GameObjects.</returns>
    public IEnumerable<GameObject> ForFloor(FloorDefinition floor)
    {
        // Iterate over each Area Definition in the Floor Definition area list
        foreach (var def in floor.OrderedAreas)
        {
            // If not present, skip it
            if (!def)
                continue;
            // If present, enable the GameObject
            if (_byDef.TryGetValue(def, out var go))
                yield return go;
            else
                Debug.LogWarning(
                    $"[AreaRegistry] Missing AreaInstance for '{def.name}' in this scene.",
                    this
                );
        }
    }
}
