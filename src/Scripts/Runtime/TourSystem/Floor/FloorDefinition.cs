using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Definition of a floor in the tour system.
/// </summary>
[CreateAssetMenu(fileName = "NewFloorDefinition", menuName = "AR Tour/Floor Definition")]
public class FloorDefinition : ScriptableObject
{
    [Header("Floor Info")]
    [Tooltip("Name of the floor")]
    [SerializeField]
    private string floorName;

    [Tooltip("Text to display when transitioning from this floor")]
    [SerializeField]
    private string transitionText;

    [Header("Content References")]
    [Tooltip("List of areas in the order they should be visited on this floor.")]
    [SerializeField]
    private List<AreaDefinition> orderedAreas = new();

    [Tooltip("JSON file with anchors for this floor (TextAsset).")]
    [SerializeField]
    private TextAsset anchorMapJson;

    [Tooltip("Path to the scene asset in the build (read-only).")]
    [SerializeField]
    private string scenePath;

#if UNITY_EDITOR
    [Tooltip("Assign the scene asset; its path is stored into 'scenePath'.")]
    [SerializeField]
    private SceneAsset sceneAsset;

    /// <summary>
    /// Validates the scene asset and updates the scene path accordingly.
    /// </summary>
    private void OnValidate()
    {
        if (sceneAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            if (scenePath != path)
            {
                scenePath = path;
                EditorUtility.SetDirty(this);
            }
        }
    }
#endif

    public string FloorName => floorName;
    public IReadOnlyList<AreaDefinition> OrderedAreas => orderedAreas;
    public TextAsset AnchorMapJson => anchorMapJson;
    public string ScenePath => scenePath;

    public string TransitionText => transitionText;

    public int IndexOf(AreaDefinition area) => orderedAreas?.IndexOf(area) ?? -1;

    /// <summary>
    /// Gets the area that comes after the given one in the ordered list.
    /// </summary>
    /// <param name="current">The current area.</param>
    /// <returns>The next area in the list, or null if there is none.</returns>
    public AreaDefinition GetNextAfter(AreaDefinition current)
    {
        var i = IndexOf(current);
        if (i < 0)
            return null;
        var next = i + 1;
        return next < orderedAreas.Count ? orderedAreas[next] : null;
    }

    /// <summary>
    /// Tries to parse the anchor map JSON into an object of type T.
    /// </summary>
    /// <param name="map">The output parsed map object.</param>
    /// <returns>True if parsing was successful; otherwise, false.</returns>
    public bool TryGetAnchorMap<T>(out T map)
    {
        map = default;
        if (anchorMapJson == null || string.IsNullOrEmpty(anchorMapJson.text))
            return false;
        try
        {
            map = JsonUtility.FromJson<T>(anchorMapJson.text);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to get the raw JSON text of the anchor map.
    /// </summary>
    /// <param name="json">The output JSON text.</param>
    /// <returns>True if the JSON text is available and not empty; otherwise, false.</returns>
    public bool TryGetAnchorMapText(out string json)
    {
        json = anchorMapJson != null ? anchorMapJson.text : null;
        return !string.IsNullOrEmpty(json);
    }
}
