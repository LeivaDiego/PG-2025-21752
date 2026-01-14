using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Defines a scene that can be loaded by an AR arrow.
/// </summary>
[CreateAssetMenu(fileName = "NewArrowSceneDefinition", menuName = "AR Tour/Arrow Scene")]
public sealed class ArrowSceneDefinition : ScriptableObject
{
    [SerializeField, Tooltip("Path to the scene asset in the build (read-only).")]
    private string scenePath;

    // Public getter for the scene path.
    public string ScenePath => scenePath;

#if UNITY_EDITOR
    [SerializeField, Tooltip("Assign the scene asset; its path is stored into 'scenePath'.")]
    private SceneAsset sceneAsset;

    /// <summary>
    /// Called when the script is loaded or a value changes in the inspector (Editor only).
    /// </summary>
    private void OnValidate()
    {
        // Update the scene path if the scene asset is assigned.
        if (sceneAsset == null)
            return;
        // Get the asset path and update if it has changed.
        var path = AssetDatabase.GetAssetPath(sceneAsset);
        if (scenePath != path)
        {
            // Update the stored scene path.
            scenePath = path;
            EditorUtility.SetDirty(this);
        }
    }
#endif
}
