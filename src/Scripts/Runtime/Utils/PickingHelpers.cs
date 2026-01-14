using UnityEngine.UIElements;

/// <summary>
/// Utility class for configuring picking modes on UI elements.
/// </summary>
public static class UIPickingUtils
{
    /// <summary>
    /// Configures the picking mode for a root VisualElement and all its children.
    /// </summary>
    /// <param name="root">The root VisualElement whose picking mode will be set.</param>
    /// <param name="mode">The picking mode to apply.</param>
    public static void ConfigureTreePickingMode(VisualElement root, PickingMode mode)
    {
        // If root is null, do nothing
        if (root == null)
            return;
        // Set the picking mode for the root and all its child elements
        root.pickingMode = mode;
        root.Query<VisualElement>().ForEach(e => e.pickingMode = mode);
    }

    /// <summary>
    /// Sets the VisualElement to ignore picking (pass-through).
    /// </summary>
    /// <param name="ve">The VisualElement to configure.</param>
    public static void SetPassThrough(this VisualElement ve)
    {
        // If the VisualElement is null, do nothing
        if (ve != null)
            // Set picking mode to Ignore
            ve.pickingMode = PickingMode.Ignore;
    }

    /// <summary>
    /// Sets the VisualElement to be pickable (respond to input).
    /// </summary>
    /// <param name="ve">The VisualElement to configure.</param>
    public static void SetPickable(this VisualElement ve)
    {
        // If the VisualElement is null, do nothing
        if (ve != null)
            // Set picking mode to Position
            ve.pickingMode = PickingMode.Position;
    }
}
