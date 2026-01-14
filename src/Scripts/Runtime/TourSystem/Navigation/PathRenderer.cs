using UnityEngine;

/// <summary>
/// Renders a visual line along the current NavMesh path provided by a <see cref="PathProvider"/>.
/// The path is flattened to a constant Y-level and updated every frame.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(PathProvider))]
public class PathRenderer : MonoBehaviour
{
    [SerializeField, Range(0f, 0.05f)]
    private float yOffset = 0.015f;

    [SerializeField, Range(0.01f, 1f)]
    private float width = 0.05f;

    // External References to other components
    private PathProvider provider;
    private LineRenderer line;

    /// <summary>
    /// Automatically assigns the <see cref="PathProvider"/> component when resetting the script.
    /// </summary>
    private void Reset()
    {
        provider = GetComponent<PathProvider>();
    }

    /// <summary>
    /// Initializes component references and configures the <see cref="LineRenderer"/>.
    /// </summary>
    private void Awake()
    {
        provider = GetComponent<PathProvider>();
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.widthMultiplier = width;
        line.positionCount = 0;
    }

    /// <summary>
    /// Updates the displayed path each frame. Clears the line if no valid path exists
    /// or if the provider is paused.
    /// </summary>
    private void LateUpdate()
    {
        // Check if there is an active provider
        if (!provider || provider.Paused)
        {
            // Clear the current path points
            Clear();
            return;
        }

        // Get the current path from the PathProvider
        var path = provider.CurrentPath;
        var corners = path?.corners;
        // Check if it has enough valid poiints
        if (corners == null || corners.Length < 2)
        {
            // Clear the current path points if there are not enough corners
            Clear();
            return;
        }

        // Compute the base Y position for the path line
        float baseY = provider.transform.position.y + yOffset;
        // Compute the new path vector
        var pts = new Vector3[corners.Length];
        // Iterate over each point
        for (int i = 0; i < corners.Length; i++)
        {
            // Assign current path point to a flattened position with the base Y offset
            pts[i] = new Vector3(corners[i].x, baseY, corners[i].z);
        }

        // Draw the computed path
        line.widthMultiplier = width;
        line.positionCount = pts.Length;
        line.SetPositions(pts);
    }

    /// <summary>
    /// Clears all points from the <see cref="LineRenderer"/> if any exist.
    /// </summary>
    private void Clear()
    {
        if (line.positionCount != 0)
            line.positionCount = 0;
    }
}
