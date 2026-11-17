using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class EntranceTrigger : MonoBehaviour
{
    [Header("References")]
    public GameObject introPanel;
    public Button beginButton;
    public AudioSource introAudio;

    [Header("Options")]
    [Tooltip("If > 0, the intro auto-closes after this many seconds. " +
             "0 = disabled (player must tap Begin).")]
    public float autoCloseSeconds = 0f;

    [Tooltip("The poster must be out of view this long before the entrance counts as lost.")]
    public float lostGraceSeconds = 0.8f;

    // ── Events ─────────────────────────────────────────────
    public event System.Action OnIntroClosed;
    public event System.Action OnEntranceTracked;
    public event System.Action OnEntranceLost;

    // ── Public read-only state (for other systems to query) ─
    public bool IsEntranceTracked { get; private set; }
    public bool HasIntroBeenClosed { get; private set; }

    // ── Private ────────────────────────────────────────────
    private ObserverBehaviour observerBehaviour;
    private bool hasPlayedIntro;
    private bool lostPending;
    private float lostAt;

    void Start()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();
        if (observerBehaviour == null)
        {
            Debug.LogError($"[EntranceTrigger] '{name}' has no ObserverBehaviour. " +
                           "This script must be on the Image Target GameObject.");
            return;
        }

        observerBehaviour.OnTargetStatusChanged += HandleStatus;

        if (introPanel != null) introPanel.SetActive(false);

        if (beginButton != null)
            beginButton.onClick.AddListener(CloseIntro);
    }

    void OnDestroy()
    {
        if (observerBehaviour != null)
            observerBehaviour.OnTargetStatusChanged -= HandleStatus;

        if (beginButton != null)
            beginButton.onClick.RemoveListener(CloseIntro);
    }

    void Update()
    {
        if (lostPending && Time.unscaledTime - lostAt >= lostGraceSeconds)
        {
            lostPending = false;
            if (IsEntranceTracked)
            {
                IsEntranceTracked = false;
                OnEntranceLost?.Invoke();
            }
        }
    }

    private void HandleStatus(ObserverBehaviour b, TargetStatus status)
    {
        // Only a real, live detection counts. EXTENDED_TRACKED is a guess from device motion
        // and is what made the companion float after the poster was put away.
        bool tracked = status.Status == Status.TRACKED;

        if (tracked)
        {
            lostPending = false;
            if (!IsEntranceTracked)
            {
                IsEntranceTracked = true;
                OnEntranceTracked?.Invoke();
            }
        }
        else if (IsEntranceTracked && !lostPending)
        {
            lostPending = true;
            lostAt = Time.unscaledTime;
        }

        // Intro fires only once
        if (!tracked || hasPlayedIntro) return;

        hasPlayedIntro = true;
        if (introPanel != null) introPanel.SetActive(true);
        if (introAudio != null) introAudio.Play();

        if (autoCloseSeconds > 0f)
        {
            CancelInvoke(nameof(CloseIntro));
            Invoke(nameof(CloseIntro), autoCloseSeconds);
        }
    }

    /// <summary>Closes the intro panel. Safe to call multiple times.</summary>
    public void CloseIntro()
    {
        CancelInvoke(nameof(CloseIntro));

        if (introPanel != null) introPanel.SetActive(false);

        // Only fire the event once, even if Begin is tapped repeatedly
        if (HasIntroBeenClosed) return;

        HasIntroBeenClosed = true;
        SfxPlayer.PlayBegin();
        OnIntroClosed?.Invoke();
    }
}
