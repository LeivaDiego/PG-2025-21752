using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized audio controller responsible for narration playback,
/// fading, and sequencing audio clips across the application.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class AudioDirector : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the <see cref="AudioDirector"/>.
    /// </summary>
    public static AudioDirector Instance { get; private set; }

    [Header("Fades")]
    [Range(0f, 5f)]
    [Tooltip("Default fade-in duration in seconds")]
    [SerializeField]
    float defaultFadeIn = 0.0f;

    [Range(0f, 5f)]
    [Tooltip("Default fade-out duration in seconds")]
    [SerializeField]
    float defaultFadeOut = 0.25f;

    private AudioSource src;
    private Coroutine routine;
    private uint token;

    /// <summary>
    /// Ensures the singleton instance, prepares audio infrastructure,
    /// and applies volume preferences.
    /// </summary>
    void Awake()
    {
        // Singleton enforcement
        if (Instance && Instance != this)
        {
            // There can be only one AudioDirector
            Destroy(gameObject);
            return;
        }
        // Assign singleton instance
        Instance = this;
        // Persist across scenes
        EnsureAudioInfrastructure();
        ApplyVolumeFromPrefs();
        Debug.Log("[AudioDirector] Ready");
    }

    /// <summary>
    /// Ensures the necessary audio components (listener and source)
    /// exist and are configured on this GameObject.
    /// </summary>
    void EnsureAudioInfrastructure()
    {
        // Ensure AudioListener
        var listener = FindFirstObjectByType<AudioListener>();
        if (!listener)
        {
            // No AudioListener found, add one to this GameObject
            gameObject.AddComponent<AudioListener>();
            Debug.Log("[AudioDirector] Added AudioListener to Audio GO");
        }
        else if (!listener.enabled)
        {
            // Enable existing AudioListener
            listener.enabled = true;
        }
        // Ensure AudioSource
        src = GetComponent<AudioSource>();
        if (!src)
        {
            // No AudioSource found, add one to this GameObject
            src = gameObject.AddComponent<AudioSource>();
            Debug.Log("[AudioDirector] Added AudioSource to Audio GO");
        }
        // Configure AudioSource for narration
        ConfigureNarrationSource(src);
    }

    /// <summary>
    /// Configures the supplied <see cref="AudioSource"/> with
    /// appropriate settings for narration playback.
    /// </summary>
    /// <param name="s">The audio source to configure.</param>
    static void ConfigureNarrationSource(AudioSource s)
    {
        // Set narration-specific audio source settings
        s.playOnAwake = false;
        s.loop = false;
        s.spatialBlend = 0f;
        s.dopplerLevel = 0f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 1f;
        s.maxDistance = 10f;
        s.volume = 1f;
        s.bypassListenerEffects = false;
        s.bypassEffects = false;
        s.bypassReverbZones = true;
    }

    /// <summary>
    /// Applies the global audio volume from stored preferences.
    /// </summary>
    public static void ApplyVolumeFromPrefs()
    {
        AudioListener.volume = AppPrefs.LoadVolume() / 100f;
    }

    /// <summary>
    /// Plays a single <see cref="AudioClip"/> with optional fade-in and fade-out of any currently playing clip.
    /// </summary>
    /// <param name="clip">The clip to play.</param>
    /// <param name="fadeIn">Fade-in duration in seconds.</param>
    /// <param name="fadeOutPrev">Fade-out duration for any currently playing clip.</param>
    public void Play(AudioClip clip, float fadeIn = -1f, float fadeOutPrev = -1f)
    {
        // Validate clip
        if (!clip)
            return;
        // Use default fades if negative values provided
        fadeIn = fadeIn < 0 ? defaultFadeIn : fadeIn;
        fadeOutPrev = fadeOutPrev < 0 ? defaultFadeOut : fadeOutPrev;
        token++;
        // Start or swap to the new clip
        StartOrSwap(new[] { clip }, fadeIn, fadeOutPrev);
    }

    /// <summary>
    /// Plays a sequence of <see cref="AudioClip"/> instances in order,
    /// with gaps and optional fades between them.
    /// </summary>
    /// <param name="clips">The ordered list of clips to play.</param>
    /// <param name="gapSeconds">Gap in seconds between clips.</param>
    /// <param name="fadeIn">Fade-in duration in seconds.</param>
    /// <param name="fadeOutPrev">Fade-out duration of previously playing clip.</param>
    public void PlaySequence(
        IReadOnlyList<AudioClip> clips,
        float gapSeconds = 0.05f,
        float fadeIn = -1f,
        float fadeOutPrev = -1f
    )
    {
        // Validate clips
        if (clips == null || clips.Count == 0)
            return;
        // Use default fades if negative values provided
        fadeIn = fadeIn < 0 ? defaultFadeIn : fadeIn;
        fadeOutPrev = fadeOutPrev < 0 ? defaultFadeOut : fadeOutPrev;
        token++;
        // Stop any existing routine
        if (routine != null)
            StopCoroutine(routine);
        // Start new sequence coroutine
        routine = StartCoroutine(CoSequence(clips, gapSeconds, fadeIn, fadeOutPrev, token));
    }

    /// <summary>
    /// Stops any currently playing audio, optionally fading it out.
    /// </summary>
    /// <param name="fadeOut">Fade-out duration in seconds.</param>
    public void Stop(float fadeOut = -1f)
    {
        // Use default fade-out if negative value provided
        fadeOut = fadeOut < 0 ? defaultFadeOut : fadeOut;
        token++;
        // Stop any existing routine
        if (routine != null)
            StopCoroutine(routine);
        // Start fade-out coroutine
        routine = StartCoroutine(CoFadeOut(src, fadeOut));
    }

    /// <summary>
    /// Starts playback of a set of clips, fading out any existing audio first.
    /// </summary>
    /// <param name="clips">The clips to swap to.</param>
    /// <param name="fadeIn">Fade-in duration in seconds.</param>
    /// <param name="fadeOutPrev">Fade-out duration for the previous audio.</param>
    void StartOrSwap(IReadOnlyList<AudioClip> clips, float fadeIn, float fadeOutPrev)
    {
        // Stop any existing routine
        if (routine != null)
            StopCoroutine(routine);
        // Start new swap coroutine
        routine = StartCoroutine(CoSwapTo(clips, fadeIn, fadeOutPrev, token));
    }

    /// <summary>
    /// Coroutine that fades out the current audio and then swaps to the provided clips in sequence.
    /// </summary>
    /// <param name="clips">Clips to play.</param>
    /// <param name="fadeIn">Fade-in duration.</param>
    /// <param name="fadeOutPrev">Fade-out duration for the previous audio.</param>
    /// <param name="tk">Token used to cancel stale coroutine instances.</param>
    IEnumerator CoSwapTo(IReadOnlyList<AudioClip> clips, float fadeIn, float fadeOutPrev, uint tk)
    {
        // Fade out any currently playing audio
        yield return CoFadeOut(src, fadeOutPrev);

        // Check token validity
        if (tk != token)
            yield break;

        // Play each clip in sequence
        for (int i = 0; i < clips.Count; i++)
        {
            // Check token validity
            var clip = clips[i];
            if (!clip)
                continue;
            // Play clip with fade-in
            src.clip = clip;
            src.volume = 0f;
            src.Play();
            // Fade in to full volume
            yield return CoFadeTo(src, 1f, fadeIn);
            // Wait for clip to finish
            while (src.isPlaying && tk == token)
                yield return null;
            // Check token validity
            if (tk != token)
                yield break;
        }
    }

    /// <summary>
    /// Coroutine that plays a sequence of clips with optional gaps and fades between them.
    /// </summary>
    /// <param name="clips">Clips to play.</param>
    /// <param name="gap">Gap in seconds between clips.</param>
    /// <param name="fadeIn">Fade-in duration.</param>
    /// <param name="fadeOutPrev">Fade-out duration before the sequence starts.</param>
    /// <param name="tk">Token used to cancel stale coroutine instances.</param>
    IEnumerator CoSequence(
        IReadOnlyList<AudioClip> clips,
        float gap,
        float fadeIn,
        float fadeOutPrev,
        uint tk
    )
    {
        // Fade out any currently playing audio
        yield return CoFadeOut(src, fadeOutPrev);
        // Iterate through clips
        for (int i = 0; i < clips.Count; i++)
        {
            // Check token validity
            if (tk != token)
                yield break;
            // Get current clip
            var c = clips[i];
            if (!c)
                continue;
            // Play clip with fade-in
            src.clip = c;
            src.volume = 0f;
            src.Play();
            yield return CoFadeTo(src, 1f, fadeIn);
            // Wait for clip to finish
            while (src.isPlaying && tk == token)
                yield return null;
            // Wait for gap if not the last clip
            if (i < clips.Count - 1 && gap > 0f && tk == token)
                yield return new WaitForSeconds(gap);
        }
        // Fade out at the end of the sequence
        yield return CoFadeOut(src, defaultFadeOut);
    }

    /// <summary>
    /// Coroutine that fades out the given <see cref="AudioSource"/> over a duration,
    /// then stops it and resets its volume.
    /// </summary>
    /// <param name="s">The audio source to fade out.</param>
    /// <param name="dur">Fade-out duration in seconds.</param>
    static IEnumerator CoFadeOut(AudioSource s, float dur)
    {
        if (!s.isPlaying || dur <= 0f)
        {
            // Stop immediately if not playing or duration is zero
            s.Stop();
            s.volume = 1f;
            yield break;
        }
        // Perform fade-out
        float start = s.volume,
            t = 0f;
        while (t < dur)
        {
            // Increment time
            t += Time.unscaledDeltaTime;
            // Lerp volume down to zero
            s.volume = Mathf.Lerp(start, 0f, t / dur);
            yield return null;
        }
        // Stop and reset volume
        s.Stop();
        s.volume = 1f;
    }

    /// <summary>
    /// Coroutine that fades the volume of the given <see cref="AudioSource"/>
    /// to the target value over the specified duration.
    /// </summary>
    /// <param name="s">The audio source to fade.</param>
    /// <param name="target">Target volume value.</param>
    /// <param name="dur">Fade duration in seconds.</param>
    static IEnumerator CoFadeTo(AudioSource s, float target, float dur)
    {
        // Immediate set if duration is zero or negative
        if (dur <= 0f)
        {
            s.volume = target;
            yield break;
        }
        // Perform fade
        float start = s.volume,
            t = 0f;
        while (t < dur)
        {
            // Increment time
            t += Time.unscaledDeltaTime;
            // Lerp volume towards target
            s.volume = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        // Ensure final volume is set
        s.volume = target;
    }
}
