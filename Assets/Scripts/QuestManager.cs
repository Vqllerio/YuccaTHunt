using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class QuestItem
{
    public string itemId;
    public string displayName;
    [TextArea(2, 4)] public string memoryLine;
    [Tooltip("Optional clue shown by the Hint button (level 2). Point at the location, not the content.")]
    [TextArea(1, 2)] public string hintLine;
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Items — edit memory lines here")]
    public QuestItem[] questItems;

    [Header("Options")]
    public bool allowMemoryReplay = true;
    public bool logScans = true;

    private readonly HashSet<string> scannedIds = new HashSet<string>();

    public int TotalItems => questItems != null ? questItems.Length : 0;
    public int ScannedCount => scannedIds.Count;
    public QuestItem[] Items => questItems;

    public event System.Action<QuestItem> OnItemScanned;
    public event System.Action<int, int> OnProgressChanged;
    public event System.Action OnQuestCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        OnProgressChanged?.Invoke(ScannedCount, TotalItems);
    }

    public void RegisterScan(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        QuestItem item = FindItem(itemId);
        if (item == null)
        {
            Debug.LogWarning($"[QuestManager] Unknown itemId '{itemId}'. Check the TargetEventHandler on that target.");
            return;
        }

        if (scannedIds.Contains(itemId))
        {
            if (allowMemoryReplay) OnItemScanned?.Invoke(item);
            return;
        }

        scannedIds.Add(itemId);
        if (logScans) Debug.Log($"[QuestManager] {item.displayName} — {ScannedCount}/{TotalItems}");

        OnItemScanned?.Invoke(item);
        OnProgressChanged?.Invoke(ScannedCount, TotalItems);

        if (TotalItems > 0 && ScannedCount >= TotalItems)
        {
            if (logScans) Debug.Log("[QuestManager] Quest complete.");
            OnQuestCompleted?.Invoke();
        }
    }

    public QuestItem FindItem(string itemId)
    {
        if (questItems == null) return null;
        foreach (var i in questItems) if (i.itemId == itemId) return i;
        return null;
    }

    public bool IsScanned(string itemId) => scannedIds.Contains(itemId);
}
