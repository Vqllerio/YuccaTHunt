using UnityEngine;

public class ScanAudio : MonoBehaviour
{
    public AudioClip successClip;
    [Range(0f, 1f)] public float volume = 0.8f;

    [Header("Progress feedback")]
    [Tooltip("Each restored memory plays the chime this many semitones higher.")]
    public float pitchStepSemitones = 2f;
    [Tooltip("Ignore scan sounds closer together than this.")]
    public float minSecondsBetween = 0.4f;

    private AudioSource source;
    private QuestManager qm;
    private float lastPlayed = -999f;

    void Start()
    {
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        qm = QuestManager.Instance;
        if (qm != null) qm.OnItemScanned += PlaySuccess;
    }

    void OnDestroy()
    {
        if (qm != null) qm.OnItemScanned -= PlaySuccess;
    }

    void PlaySuccess(QuestItem item)
    {
        if (successClip == null) return;
        if (Time.unscaledTime - lastPlayed < minSecondsBetween) return;

        // The last memory is celebrated by the victory fanfare instead.
        if (qm != null && qm.TotalItems > 0 && qm.ScannedCount >= qm.TotalItems) return;

        lastPlayed = Time.unscaledTime;

        int step = qm != null ? Mathf.Max(0, qm.ScannedCount - 1) : 0;
        source.pitch = Mathf.Pow(2f, step * pitchStepSemitones / 12f);
        source.PlayOneShot(successClip, volume);
    }
}
