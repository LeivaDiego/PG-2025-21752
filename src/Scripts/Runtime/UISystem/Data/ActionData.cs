using UnityEngine;

/// <summary>
/// Data for an action pop-up in the UI system.
/// </summary>
[CreateAssetMenu(fileName = "NewActionData", menuName = "AR GUI/Action Pop Up Data")]
public class ActionData : ScriptableObject
{
    [SerializeField]
    private string actionTitle;

    [SerializeField]
    private string actionDescription;

    [SerializeField]
    private Sprite actionJack;

    [SerializeField]
    private string actionBtnText;

    [SerializeField]
    private AudioClip actionAudioClip;

    // Public getters
    public string ActionTitle => actionTitle;
    public string ActionDescription => actionDescription;
    public Sprite ActionJack => actionJack;
    public string ActionBtnText => actionBtnText;
    public AudioClip ActionAudioClip => actionAudioClip;
}
