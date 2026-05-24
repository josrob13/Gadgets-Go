using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton that owns the single AudioSource used for all dialogue voices.
/// Guarantees no overlap: PlayVoice always stops any current clip before starting a new one.
/// Supports 3D spatial audio positioning (attach to speaker transform each call),
/// clean fade-out on stop, and pause/resume for the face-tracking discomfort system.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DialogueAudioManager : MonoBehaviour
{
    public static DialogueAudioManager Instance { get; private set; }

    [Header("Voice Source")]
    [SerializeField] private AudioSource voiceSource;

    [Header("3D Spatial Audio (VR)")]
    [Tooltip("0 = fully 2D (omnidirectional), 1 = fully 3D. 0.85 keeps voices audible from any angle while still feeling spatially grounded.")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.85f;

    [Tooltip("Within this distance the voice plays at full volume.")]
    [SerializeField] private float minDistance = 1f;

    [Tooltip("Beyond this distance the voice fades to silence.")]
    [SerializeField] private float maxDistance = 6f;

    [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    [Header("Voice Settings")]
    [SerializeField, Range(0f, 1f)] private float voiceVolume = 1f;

    [Tooltip("Duration in seconds of the fade-out when StopVoice(fade:true) is called. Keep short to feel responsive.")]
    [SerializeField] private float fadeOutDuration = 0.12f;

    private Coroutine _fadeCoroutine;
    private bool _isPaused;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitialiseAudioSource();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitialiseAudioSource()
    {
        if (voiceSource == null)
            voiceSource = GetComponent<AudioSource>();

        voiceSource.playOnAwake  = false;
        voiceSource.loop         = false;
        voiceSource.dopplerLevel = 0f;   // NPCs are stationary — no doppler artefacts
        voiceSource.spatialBlend = spatialBlend;
        voiceSource.rolloffMode  = rolloffMode;
        voiceSource.minDistance  = minDistance;
        voiceSource.maxDistance  = maxDistance;
        voiceSource.volume       = voiceVolume;
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Stops any playing voice and starts <paramref name="clip"/>.
    /// If <paramref name="speakerTransform"/> is provided the AudioSource is
    /// repositioned there so the voice feels spatially anchored to the NPC.
    /// Passing a null clip silently stops whatever was playing.
    /// </summary>
    public void PlayVoice(AudioClip clip, Transform speakerTransform = null)
    {
        CancelFade();

        if (clip == null)
        {
            voiceSource.Stop();
            _isPaused = false;
            return;
        }

        if (speakerTransform != null)
            transform.position = speakerTransform.position;

        voiceSource.Stop();
        voiceSource.clip   = clip;
        voiceSource.volume = voiceVolume;
        voiceSource.Play();
        _isPaused = false;
    }

    /// <summary>
    /// Stops the current voice.
    /// When <paramref name="fade"/> is true a short fade-out is applied so the
    /// cut doesn't feel abrupt (e.g. when moving to the next dialogue node).
    /// </summary>
    public void StopVoice(bool fade = true)
    {
        if (!voiceSource.isPlaying && !_isPaused)
            return;

        CancelFade();

        if (fade && voiceSource.isPlaying)
        {
            _fadeCoroutine = StartCoroutine(FadeOutAndStop());
        }
        else
        {
            voiceSource.Stop();
            voiceSource.volume = voiceVolume;
            _isPaused = false;
        }
    }

    /// <summary>
    /// Pauses the current voice mid-clip (called on face-tracking discomfort).
    /// Playback position is preserved so ResumeVoice picks up exactly where it stopped.
    /// </summary>
    public void PauseVoice()
    {
        CancelFade();
        if (voiceSource.isPlaying)
        {
            voiceSource.Pause();
            _isPaused = true;
        }
    }

    /// <summary>
    /// Resumes a paused voice clip (called when face-tracking discomfort clears).
    /// </summary>
    public void ResumeVoice()
    {
        if (_isPaused)
        {
            voiceSource.UnPause();
            _isPaused = false;
        }
    }

    /// <summary>True while a voice is actively playing or paused mid-clip.</summary>
    public bool IsPlaying => voiceSource.isPlaying || _isPaused;

    // ─── Internals ────────────────────────────────────────────────────────────

    private void CancelFade()
    {
        if (_fadeCoroutine == null) return;
        StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = null;
        voiceSource.volume = voiceVolume;
    }

    private IEnumerator FadeOutAndStop()
    {
        float startVolume = voiceSource.volume;
        float elapsed     = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed            += Time.deltaTime;
            voiceSource.volume  = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        voiceSource.Stop();
        voiceSource.volume = voiceVolume;
        _fadeCoroutine     = null;
    }
}
