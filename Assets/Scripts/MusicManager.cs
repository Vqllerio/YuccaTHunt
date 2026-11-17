using UnityEngine;

/// <summary>
/// Layered background music. All stems start in sync and loop together. Layers fade in as
/// the player restores memories. On quest completion the stems fade out, a fanfare plays,
/// and the ending theme takes over. Music ducks while the settings panel is open or the game is paused.
/// </summary>
public class MusicManager : MonoBehaviour
{
    [Header("Layered stems")]
    public AudioClip pad;
    public AudioClip bass;
    public AudioClip arp;
    public AudioClip drums;
    public AudioClip glitch;

    [Tooltip("Plays after the quest is completed.")]
    public AudioClip ending;

    [Tooltip("Fallback single loop, used only when no stems are assigned.")]
    public AudioClip clip;

    [Header("Levels")]
    [Range(0f, 1f)] public float volume = 0.5f;
    [Range(0f, 1f)] public float endingVolume = 0.6f;
    [Range(0f, 1f)] public float duckedMultiplier = 0.3f;

    [Header("Layer thresholds (fraction of memories restored)")]
    [Range(0f, 1f)] public float bassAt = 0.01f;
    [Range(0f, 1f)] public float arpAt = 0.34f;
    [Range(0f, 1f)] public float drumsAt = 0.5f;
    [Range(0f, 1f)] public float glitchAt = 0.75f;

    [Header("Timing")]
    public float fadeInSeconds = 2.5f;
    public float layerFadeSeconds = 2.0f;
    public float duckSpeed = 3f;
    public float endingDelaySeconds = 3.2f;
    public float endingFadeSeconds = 3.0f;

    [Tooltip("Music gets quieter while this object is active (the settings menu).")]
    public GameObject settingsPanel;

    class Layer
    {
        public AudioSource src;
        public float threshold;
        public float level;
        public float unlocked;
    }

    Layer[] layers;
    AudioSource endingSource;
    AudioSource singleSource;
    QuestManager qm;

    float fadeIn;
    float duck = 1f;
    bool completed;
    float completedAt;
    float stemsOut = 1f;
    float endingLevel;
    bool endingStarted;

    void Awake()
    {
        var defs = new (AudioClip c, float th, float lv)[]
        {
            (pad, 0f, 1f),
            (bass, bassAt, 1f),
            (arp, arpAt, 0.85f),
            (drums, drumsAt, 0.9f),
            (glitch, glitchAt, 0.9f),
        };

        var list = new System.Collections.Generic.List<Layer>();
        foreach (var d in defs)
        {
            if (d.c == null) continue;
            var l = new Layer { src = NewSource(d.c, true), threshold = d.th, level = d.lv };
            l.unlocked = d.th <= 0f ? 1f : 0f;
            list.Add(l);
        }
        layers = list.ToArray();

        if (layers.Length == 0 && clip != null)
            singleSource = NewSource(clip, true);

        if (ending != null) endingSource = NewSource(ending, true);
    }

    AudioSource NewSource(AudioClip c, bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.clip = c;
        s.loop = loop;
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        s.volume = 0f;
        return s;
    }

    void Start()
    {
        double t0 = AudioSettings.dspTime + 0.3;
        foreach (var l in layers) l.src.PlayScheduled(t0);
        if (singleSource != null) singleSource.PlayScheduled(t0);
        TryBind();
    }

    void TryBind()
    {
        if (qm != null) return;
        qm = QuestManager.Instance;
        if (qm != null) qm.OnQuestCompleted += OnCompleted;
    }

    void OnDestroy()
    {
        if (qm != null) qm.OnQuestCompleted -= OnCompleted;
    }

    void OnCompleted()
    {
        if (completed) return;
        completed = true;
        completedAt = Time.unscaledTime;
        SfxPlayer.PlayVictory();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        TryBind();

        fadeIn = Mathf.Min(1f, fadeIn + dt / Mathf.Max(0.01f, fadeInSeconds));

        bool ducked = Time.timeScale <= 0f ||
                      (settingsPanel != null && settingsPanel.activeInHierarchy);
        float duckTarget = ducked ? duckedMultiplier : 1f;
        duck = Mathf.Lerp(duck, duckTarget, 1f - Mathf.Exp(-duckSpeed * dt));

        float progress = 0f;
        if (qm != null && qm.TotalItems > 0)
            progress = (float)qm.ScannedCount / qm.TotalItems;

        if (completed)
            stemsOut = Mathf.MoveTowards(stemsOut, 0f, dt / 1.6f);

        float master = volume * duck * fadeIn * stemsOut;

        foreach (var l in layers)
        {
            float want = progress >= l.threshold ? 1f : 0f;
            l.unlocked = Mathf.MoveTowards(l.unlocked, want, dt / Mathf.Max(0.05f, layerFadeSeconds));
            l.src.volume = master * l.level * Smooth(l.unlocked);
        }

        if (singleSource != null) singleSource.volume = master;

        UpdateEnding(dt);
    }

    void UpdateEnding(float dt)
    {
        if (endingSource == null) return;

        if (completed && !endingStarted && Time.unscaledTime - completedAt >= endingDelaySeconds)
        {
            endingStarted = true;
            endingSource.Play();
        }

        float want = endingStarted ? 1f : 0f;
        endingLevel = Mathf.MoveTowards(endingLevel, want, dt / Mathf.Max(0.05f, endingFadeSeconds));
        endingSource.volume = endingVolume * duck * Smooth(endingLevel);
    }

    static float Smooth(float x) { return x * x * (3f - 2f * x); }
}
