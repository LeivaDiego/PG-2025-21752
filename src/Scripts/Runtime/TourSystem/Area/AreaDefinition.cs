using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines metadata and presentation content for a specific AR Tour area,
/// including text, audio, images, and UI configuration.
/// </summary>
/// <remarks>
/// Creates an editor menu for ease data asset creation
/// </remarks>
[CreateAssetMenu(fileName = "NewAreaDefinition", menuName = "AR Tour/Area Definition")]
public class AreaDefinition : ScriptableObject
{
    [Header("Area Info")]
    [Tooltip("Name of the area")]
    [SerializeField]
    private string areaName;

    [Header("Content References")]
    [Tooltip("Text containing area description")]
    [SerializeField]
    private string areaText;

    [Tooltip("Audio clips for the area")]
    [SerializeField]
    private List<AudioClip> audioClips;

    [Tooltip("Image file representing the area (for reference only)")]
    [SerializeField]
    private Sprite areaImage;

    [Header("UI Configuration")]
    [Tooltip("Icon representing the area in the UI")]
    [SerializeField]
    private Sprite areaIcon;

    [Tooltip("Whether to show area info in the UI")]
    [SerializeField]
    private bool showInfo = true;

    // Pulbic references to get the stored values
    public string AreaName => areaName;

    public string AreaText => areaText;

    public List<AudioClip> AudioClips => audioClips;

    public Sprite AreaImage => areaImage;

    public bool ShowInfo => showInfo;

    public Sprite AreaIcon => areaIcon;
}
