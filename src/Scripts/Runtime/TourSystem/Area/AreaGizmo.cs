using UnityEngine;

/// <summary>
/// Draws a colored gizmo representing the bounds of a BoxCollider,
/// supporting both wireframe and solid rendering in edit mode and optionally in play mode.
/// </summary>
/// <remarks>
/// Requires a BoxCollider component
/// </remarks>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class AreaGizmo : MonoBehaviour
{
    [Header("Gizmo Settings")]
    [Tooltip("Show gizmo in play mode")]
    public bool showInPlayMode = true;

    [Tooltip("Draw solid cube")]
    public bool drawSolid = true;

    [Tooltip("Draw wireframe cube")]
    public bool drawWire = true;

    [Header("Area Color Settings")]
    [Tooltip("Base color for gizmo (alpha ignored)")]
    public Color baseColor = new(0f, 1f, 0f, 1f);

    [Range(0f, 1f)]
    [Tooltip("Alpha value for solid cube")]
    public float solidAlpha = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Alpha value for wireframe cube")]
    public float wireAlpha = 1.0f;

    /// <summary>
    /// Draws the gizmo representing this area's BoxCollider, using the configured
    /// color, alpha, and rendering style.
    /// Executed both in edit mode and play mode depending on settings.
    /// </summary>
    void OnDrawGizmos()
    {
        // Check if rendering in play mode is enabled
        if (Application.isPlaying && !showInPlayMode)
            return;

        // Get the BoxCollider component
        var bc = GetComponent<BoxCollider>();

        if (!bc)
            return;

        // Compute box matrix
        var m = Matrix4x4.TRS(
            bc.transform.TransformPoint(bc.center),
            bc.transform.rotation,
            Vector3.Scale(bc.transform.lossyScale, bc.size)
        );

        // Apply gizmo matrix and color
        var prevM = Gizmos.matrix;
        var prevC = Gizmos.color;
        Gizmos.matrix = m;

        if (drawSolid)
        {
            // Draw box faces as solid surface
            var c = baseColor;
            c.a = solidAlpha;
            Gizmos.color = c;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }

        if (drawWire)
        {
            // Draw box edges as wireframe
            var c = baseColor;
            c.a = wireAlpha;
            Gizmos.color = c;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        // Setup gizmo color and matrix for display
        Gizmos.color = prevC;
        Gizmos.matrix = prevM;
    }
}
