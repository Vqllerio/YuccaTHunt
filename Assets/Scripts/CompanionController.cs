using UnityEngine;

public class CompanionController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The root of the companion Yucca. Will be shown/hidden automatically.")]
    public GameObject companionRoot;

    [Tooltip("Leave empty — will auto-find the EntranceTrigger in the scene.")]
    public EntranceTrigger entranceTrigger;

    // ── State ──────────────────────────────────────────────
    private bool introClosed;
    private bool entranceVisible;

    void Start()
    {
        if (companionRoot == null)
        {
            Debug.LogError("[CompanionController] companionRoot is not assigned.");
            return;
        }

        // Start hidden
        companionRoot.SetActive(false);

        // Auto-find the entrance trigger if not wired
        if (entranceTrigger == null)
            entranceTrigger = FindObjectOfType<EntranceTrigger>();

        if (entranceTrigger == null)
        {
            // No entrance in scene — show companion immediately (solo mode fallback)
            Debug.LogWarning("[CompanionController] No EntranceTrigger found. " +
                             "Companion will show immediately.");
            introClosed = true;
            UpdateVisibility();
            return;
        }

        entranceTrigger.OnIntroClosed      += HandleIntroClosed;
        entranceTrigger.OnEntranceTracked  += HandleEntranceTracked;
        entranceTrigger.OnEntranceLost     += HandleEntranceLost;

        // Sync initial state in case events already fired before we subscribed
        introClosed     = entranceTrigger.HasIntroBeenClosed;
        entranceVisible = entranceTrigger.IsEntranceTracked;

        UpdateVisibility();
    }

    void OnDestroy()
    {
        if (entranceTrigger == null) return;
        entranceTrigger.OnIntroClosed     -= HandleIntroClosed;
        entranceTrigger.OnEntranceTracked -= HandleEntranceTracked;
        entranceTrigger.OnEntranceLost    -= HandleEntranceLost;
    }

    private void HandleIntroClosed()      { introClosed = true;    UpdateVisibility(); }
    private void HandleEntranceTracked()  { entranceVisible = true; UpdateVisibility(); }
    private void HandleEntranceLost()     { entranceVisible = false; UpdateVisibility(); }

    /// <summary>
    /// Show companion only when the intro is closed AND the entrance poster
    /// is not currently visible. Otherwise the big Yucca on the poster is
    /// the one on screen — no duplicates.
    /// </summary>
    private void UpdateVisibility()
    {
        bool shouldShow = introClosed && !entranceVisible;
        if (companionRoot.activeSelf != shouldShow)
            companionRoot.SetActive(shouldShow);
    }
}