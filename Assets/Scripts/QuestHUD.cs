using System.Collections;
using System.Text;
using UnityEngine;
using TMPro;

public class QuestHUD : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI progressText;
    public GameObject memoryPanel;
    public TextMeshProUGUI memoryText;
    public TextMeshProUGUI checklistText;

    [Header("Timing")]
    public float memoryDisplaySeconds = 5f;

    private Coroutine hideRoutine;
    private QuestManager qm;

    void Start()
    {
        if (memoryPanel != null) memoryPanel.SetActive(false);

        qm = QuestManager.Instance;
        if (qm == null) { Debug.LogError("[QuestHUD] No QuestManager in scene."); return; }

        qm.OnProgressChanged += UpdateProgress;
        qm.OnItemScanned += ShowMemory;
        qm.OnQuestCompleted += HideMemory;

        UpdateProgress(qm.ScannedCount, qm.TotalItems);
    }

    void OnDestroy()
    {
        if (qm == null) return;
        qm.OnProgressChanged -= UpdateProgress;
        qm.OnItemScanned -= ShowMemory;
        qm.OnQuestCompleted -= HideMemory;
    }

    void UpdateProgress(int scanned, int total)
    {
        if (progressText != null)
            progressText.text = $"Memories restored: {scanned}/{total}";
        BuildChecklist();
    }

    void BuildChecklist()
    {
        if (checklistText == null || qm == null || qm.Items == null) return;
        var sb = new StringBuilder();
        foreach (var item in qm.Items)
        {
            if (item == null) continue;
            sb.AppendLine($"{(qm.IsScanned(item.itemId) ? "✓" : "○")} {item.displayName}");
        }
        checklistText.text = sb.ToString();
    }

    void ShowMemory(QuestItem item)
    {
        if (memoryPanel == null || memoryText == null) return;
        memoryText.text = $"\"{item.memoryLine}\"";

        var fader = memoryPanel.GetComponent<CanvasGroupFader>();
        if (fader != null)
        {
            if (hideRoutine != null) StopCoroutine(hideRoutine);
            fader.Show(memoryDisplaySeconds);
        }
        else
        {
            memoryPanel.SetActive(true);
            if (hideRoutine != null) StopCoroutine(hideRoutine);
            hideRoutine = StartCoroutine(HideAfterDelay());
        }
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(memoryDisplaySeconds);
        if (memoryPanel != null) memoryPanel.SetActive(false);
    }

    void HideMemory()
    {
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        var fader = memoryPanel != null ? memoryPanel.GetComponent<CanvasGroupFader>() : null;
        if (fader != null) fader.Hide();
        else if (memoryPanel != null) memoryPanel.SetActive(false);
    }
}