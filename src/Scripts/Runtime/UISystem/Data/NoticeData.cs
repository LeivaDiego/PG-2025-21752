using UnityEngine;

/// <summary>
/// Data for a notice pop-up in the UI system.
/// </summary>
[CreateAssetMenu(fileName = "NewNoticeData", menuName = "AR GUI/Notice Pop Up Data")]
public class NoticeData : ScriptableObject
{
    [SerializeField]
    private string noticeTitle;

    [SerializeField]
    private string noticeMessage;

    [SerializeField]
    private Sprite noticeJack;

    [SerializeField]
    private AudioClip noticeAudioClip;

    public string NoticeTitle => noticeTitle;
    public string NoticeMessage => noticeMessage;
    public Sprite NoticeJack => noticeJack;
    public AudioClip NoticeAudioClip => noticeAudioClip;
}
