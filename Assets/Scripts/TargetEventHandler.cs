using UnityEngine;
using Vuforia;

public class TargetEventHandler : MonoBehaviour
{
    [Tooltip("Must exactly match itemId in QuestManager.")]
    public string itemId;

    [Header("Scan VFX")]
    public ParticleSystem scanBurst;

    [Header("Anti-spam")]
    [Tooltip("The target must be out of sight for at least this long before it can count as a new scan.")]
    public float lostSecondsBeforeRescan = 2.0f;
    [Tooltip("Hard minimum time between two scans of this target.")]
    public float cooldownSeconds = 3.0f;

    private ObserverBehaviour observerBehaviour;
    private bool present;
    private float lostAt = -999f;
    private float lastFireAt = -999f;

    void Start()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();
        if (observerBehaviour == null)
        {
            Debug.LogError($"[TargetEventHandler] '{name}' has no ObserverBehaviour. Is it an Image Target?");
            return;
        }
        observerBehaviour.OnTargetStatusChanged += HandleStatus;
    }

    void OnDestroy()
    {
        if (observerBehaviour != null)
            observerBehaviour.OnTargetStatusChanged -= HandleStatus;
    }

    private void HandleStatus(ObserverBehaviour b, TargetStatus status)
    {
        bool isTracked = status.Status == Status.TRACKED;
        bool isHeld = isTracked || status.Status == Status.EXTENDED_TRACKED;

        if (!isHeld)
        {
            if (present) lostAt = Time.unscaledTime;
            present = false;
            return;
        }

        if (!isTracked) return;

        // In an online race, scans only count once the race has started.
        if (!MultiplayerGate.ScanningAllowed) return;

        bool wasPresent = present;
        present = true;
        if (wasPresent) return;

        float now = Time.unscaledTime;
        bool firstEver = lastFireAt < -900f;
        if (!firstEver && now - lostAt < lostSecondsBeforeRescan) return;
        if (!firstEver && now - lastFireAt < cooldownSeconds) return;

        if (QuestManager.Instance == null)
        {
            Debug.LogError("[TargetEventHandler] No QuestManager in scene.");
            return;
        }

        lastFireAt = now;
        QuestManager.Instance.RegisterScan(itemId);

        if (scanBurst != null) scanBurst.Play();
    }
}
