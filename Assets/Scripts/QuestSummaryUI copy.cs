using System.Text;
using UnityEngine;
using TMPro;

public class QuestSummaryUI : MonoBehaviour
{
    [Header("References")]
    public GameObject summaryPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;

    [Header("Audio")]
    public AudioSource closingAudio;

    private QuestManager qm;

    void Start()
    {
        if (summaryPanel != null) summaryPanel.SetActive(false);

        qm = QuestManager.Instance;
        if (qm == null) { Debug.LogError("[QuestSummaryUI] No QuestManager in scene."); return; }

        qm.OnQuestCompleted += ShowSummary;
    }

    void OnDestroy()
    {
        if (qm != null) qm.OnQuestCompleted -= ShowSummary;
    }

    void ShowSummary()
    {
        if (titleText != null) titleText.text = "Memories Restored";

        var sb = new StringBuilder();
        sb.AppendLine("What Yucca remembers about the Informatics major and the fifth floor:");
        sb.AppendLine();

        int i = 1;
        foreach (var item in qm.Items)
        {
            if (item == null) continue;
            sb.AppendLine($"{i}. {item.displayName} — {item.memoryLine}");
            i++;
        }

        sb.AppendLine();
        sb.AppendLine("\"That's what I remember. Now you remember it too. Welcome to the fifth floor.\"");

        if (bodyText != null) bodyText.text = sb.ToString();
        if (summaryPanel != null) summaryPanel.SetActive(true);
        if (closingAudio != null) closingAudio.Play();
    }
}