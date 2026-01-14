using UnityEngine;

/// <summary>
/// Data for an onboarding slide in the UI system.
/// </summary>
[CreateAssetMenu(fileName = "NewOnboardingSlide", menuName = "AR GUI/Onboarding Slide")]
public class OnboardingSlide : ScriptableObject
{
    [SerializeField]
    private string title;

    [SerializeField, TextArea(2, 5)]
    private string description;

    [SerializeField]
    private Sprite art;

    [SerializeField]
    private AudioClip narration;
    private string buttonText;
    public string Title => title;
    public string Description => description;
    public Sprite Art => art;
    public string ButtonText => buttonText;
    public AudioClip Narration => narration;
}
