using UnityEngine;

/// <summary>
/// Audio atlas containing clips for various system events.
/// </summary>
[CreateAssetMenu(fileName = "NewAudioAtlas", menuName = "AR Audio/Audio Atlas")]
public sealed class AudioAtlas : ScriptableObject
{
    public AudioClip tourStart;
    public AudioClip floorReady;
    public AudioClip connecting;
    public AudioClip connectionLost;
    public AudioClip navigating;
    public AudioClip settingsPreview;
    public AudioClip tourComplete;
}
