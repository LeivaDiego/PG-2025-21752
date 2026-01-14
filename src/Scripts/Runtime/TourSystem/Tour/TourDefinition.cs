using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Definition of a tour in the tour system.
/// </summary>
[CreateAssetMenu(fileName = "NewTourDefinition", menuName = "AR Tour/Tour Definition")]
public class TourDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Name of the tour")]
    [SerializeField]
    private string tourName;

    [Header("Floor Order (first -> last)")]
    [Tooltip("List of floors in the order they should be visited in this tour.")]
    [SerializeField]
    private List<FloorDefinition> orderedFloors = new();

    public string TourName => tourName;

    /// <summary>
    /// Gets the list of floors in the order they should be visited in this tour.
    /// </summary>
    public IReadOnlyList<FloorDefinition> OrderedFloors => orderedFloors;

    /// <summary>
    /// Gets the index of the given floor in the ordered list.
    /// </summary>
    /// <param name="floor">The floor to find.</param>
    /// <returns>The index of the floor, or -1 if not found.</returns>
    public int IndexOf(FloorDefinition floor) => orderedFloors?.IndexOf(floor) ?? -1;

    /// <summary>
    /// Gets the floor that comes after the given one in the ordered list.
    /// </summary>
    /// <param name="current">The current floor.</param>
    /// <returns>The next floor in the list, or null if there is none.</returns>
    public FloorDefinition GetNextAfter(FloorDefinition current)
    {
        var i = IndexOf(current);
        if (i < 0)
            return null;
        var ni = i + 1;
        return ni < orderedFloors.Count ? orderedFloors[ni] : null;
    }

    /// <summary>
    /// Gets the total number of areas across all floors in this tour.
    /// </summary>
    public int TotalAreasCount()
    {
        int n = 0;
        foreach (var f in orderedFloors)
            if (f != null && f.OrderedAreas != null)
                n += f.OrderedAreas.Count;
        return n;
    }

    /// <summary>
    /// Enumerates all areas across all floors in this tour.
    /// </summary>
    public IEnumerable<AreaDefinition> EnumerateAllAreas()
    {
        foreach (var f in orderedFloors)
            if (f != null && f.OrderedAreas != null)
                foreach (var a in f.OrderedAreas)
                    if (a != null)
                        yield return a;
    }
}
