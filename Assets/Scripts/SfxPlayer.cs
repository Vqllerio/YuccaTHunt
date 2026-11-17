using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance { get; private set; }

    public AudioClip clickClip;
    public AudioClip openClip;
    public AudioClip beginClip;
    public AudioClip victoryClip;

    [Range(0f, 1f)] public float clickVolume = 0.55f;
    [Range(0f, 1f)] public float openVolume = 0.5f;
    [Range(0f, 1f)] public float beginVolume = 0.8f;
    [Range(0f, 1f)] public float victoryVolume = 0.9f;

    AudioSource source;
    float lastClickTime = -1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Play(AudioClip clip, float volume)
    {
        if (clip != null && source != null) source.PlayOneShot(clip, volume);
    }

    public static void PlayClick()
    {
        if (Instance == null) return;
        if (Time.unscaledTime - Instance.lastClickTime < 0.05f) return;
        Instance.lastClickTime = Time.unscaledTime;
        Instance.Play(Instance.clickClip, Instance.clickVolume);
    }

    public static void PlayOpen(float scale = 1f)
    {
        if (Instance != null) Instance.Play(Instance.openClip, Instance.openVolume * scale);
    }

    public static void PlayBegin()
    {
        if (Instance != null) Instance.Play(Instance.beginClip, Instance.beginVolume);
    }

    public static void PlayVictory()
    {
        if (Instance != null) Instance.Play(Instance.victoryClip, Instance.victoryVolume);
    }
}
