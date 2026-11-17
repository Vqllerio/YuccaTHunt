using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Living Yucca": a speech bubble that reacts to scans, progress, completion and idling,
/// plus a Hint button that escalates (vague -> clue -> name) with a cooldown.
/// Builds its own UI and wires itself to QuestManager / EntranceTrigger / RaceTimer,
/// so it only needs to exist on the Canvas (QuestCompleteScreen adds it automatically).
/// Hint wording per item comes from QuestItem.hintLine (optional).
/// </summary>
public class YuccaVoice : MonoBehaviour
{
    public TMP_FontAsset font;
    public Sprite cardSprite;

    [Header("Timing")]
    public float idleNudgeSeconds = 35f;
    public float hintCooldownSeconds = 25f;
    public float charsPerSecond = 45f;
    public float holdSeconds = 3.2f;

    [Header("Layout")]
    public Vector2 bubblePosition = new Vector2(0f, 330f);
    public Vector2 bubbleSize = new Vector2(700f, 150f);
    public Vector2 hintButtonPosition = new Vector2(-70f, 330f);

    [Header("Colours")]
    public Color bubbleColor = new Color32(26, 29, 62, 240);
    public Color textColor = new Color32(240, 240, 248, 255);
    public Color accent = new Color32(255, 166, 48, 255);

    public static int HintsUsed { get; private set; }

    static readonly string[] ScanLines =
    {
        "Ooh, {0}! That one felt warm.",
        "{0}... I remember that!",
        "Yes! {0} is back with me.",
        "Another piece. Thank you.",
    };

    QuestManager qm;
    EntranceTrigger entrance;
    RaceTimer timer;

    GameObject root, bubble, hintButton;
    TextMeshProUGUI bubbleText, hintLabel, hintSub;
    Image hintFill;
    Coroutine speakRoutine;

    bool introClosed, completed;
    float lastScanTime, hintReadyAt, lastIdleNudge;
    int hintLevel;               // 0..2 for the current target item
    string hintTargetId;
    int lastLineIndex = -1;

    void Start()
    {
        Build();
        qm = QuestManager.Instance;
        entrance = FindAnyObjectByType<EntranceTrigger>();
        if (entrance != null) entrance.OnIntroClosed += OnIntroClosed; else introClosed = true;
        if (qm != null)
        {
            qm.OnItemScanned += OnScanned;
            qm.OnProgressChanged += OnProgress;
            qm.OnQuestCompleted += OnCompleted;
        }
        HintsUsed = 0;
        SetVisible(false);
    }

    void OnDestroy()
    {
        if (entrance != null) entrance.OnIntroClosed -= OnIntroClosed;
        if (qm != null)
        {
            qm.OnItemScanned -= OnScanned;
            qm.OnProgressChanged -= OnProgress;
            qm.OnQuestCompleted -= OnCompleted;
        }
    }

    // ---------- events ----------

    void OnIntroClosed()
    {
        introClosed = true;
        lastScanTime = lastIdleNudge = Time.unscaledTime;
        hintReadyAt = Time.unscaledTime + 10f;
        Say("I'm Yucca. Find the memories hidden around the lounge. Tap Hint if you get stuck.", 4.5f);
    }

    void OnScanned(QuestItem item)
    {
        lastScanTime = lastIdleNudge = Time.unscaledTime;
        if (item != null && item.itemId == hintTargetId) { hintTargetId = null; hintLevel = 0; }
        if (qm != null && qm.ScannedCount >= qm.TotalItems) return; // completion line handles it

        int i;
        do { i = Random.Range(0, ScanLines.Length); } while (i == lastLineIndex && ScanLines.Length > 1);
        lastLineIndex = i;
        string name = item != null && !string.IsNullOrEmpty(item.displayName) ? item.displayName : "that";
        Say(string.Format(ScanLines[i], name));
    }

    void OnProgress(int scanned, int total)
    {
        if (total <= 0 || scanned >= total || scanned == 0) return;
        if (total - scanned == 1) StartCoroutine(SayLater("One memory left. I can feel it close.", 2.6f));
        else if (scanned * 2 == total || (total % 2 == 1 && scanned == total / 2 + 1))
            StartCoroutine(SayLater("Halfway there. I feel more like myself.", 2.6f));
    }

    void OnCompleted()
    {
        completed = true;
        Say("That's all of them. You found every memory. Thank you.", 5f);
        SetButtonVisible(false);
    }

    IEnumerator SayLater(string line, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (!completed) Say(line);
    }

    // ---------- update ----------

    void Update()
    {
        if (root == null) return;
        if (timer == null) timer = RaceTimer.Instance;

        bool active = introClosed && !completed && (timer == null || timer.IsRunning);
        if (hintButton.activeSelf != active) SetButtonVisible(active);
        if (!active) return;

        // idle nudge
        if (Time.unscaledTime - Mathf.Max(lastScanTime, lastIdleNudge) > idleNudgeSeconds)
        {
            lastIdleNudge = Time.unscaledTime;
            Say("Stuck? Tap Hint and I'll help.", 3.5f);
        }

        // cooldown visual
        float left = hintReadyAt - Time.unscaledTime;
        bool ready = left <= 0f;
        hintFill.fillAmount = ready ? 1f : 1f - Mathf.Clamp01(left / hintCooldownSeconds);
        hintSub.text = ready ? "tap for a clue" : Mathf.CeilToInt(left) + "s";
    }

    // ---------- hint ----------

    void OnHintPressed()
    {
        if (completed || Time.unscaledTime < hintReadyAt) return;
        SfxPlayer.PlayClick();

        QuestItem target = NextUnscanned();
        if (target == null) return;
        if (target.itemId != hintTargetId) { hintTargetId = target.itemId; hintLevel = 0; }

        string line;
        switch (hintLevel)
        {
            case 0:
                line = "Look around slowly. Something here holds a memory.";
                break;
            case 1:
                line = !string.IsNullOrEmpty(target.hintLine) ? target.hintLine
                     : "Try somewhere you haven't pointed your camera yet.";
                break;
            default:
                line = "I think I felt the " + target.displayName + ".";
                break;
        }
        hintLevel = Mathf.Min(hintLevel + 1, 2);
        HintsUsed++;
        hintReadyAt = Time.unscaledTime + hintCooldownSeconds;
        lastIdleNudge = Time.unscaledTime;
        Say(line, 5f);
    }

    QuestItem NextUnscanned()
    {
        if (qm == null || qm.questItems == null) return null;
        foreach (var it in qm.questItems)
            if (it != null && !qm.IsScanned(it.itemId)) return it;
        return null;
    }

    // ---------- speech ----------

    public void Say(string line, float extraHold = 0f)
    {
        if (bubble == null || completed && extraHold == 0f) return;
        if (speakRoutine != null) StopCoroutine(speakRoutine);
        speakRoutine = StartCoroutine(SpeakRoutine(line, Mathf.Max(holdSeconds, extraHold)));
    }

    IEnumerator SpeakRoutine(string line, float hold)
    {
        bubble.SetActive(true);
        bubbleText.text = line;
        bubbleText.maxVisibleCharacters = 0;
        var rt = (RectTransform)bubble.transform;

        for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.18f;
            rt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        rt.localScale = Vector3.one;

        float shown = 0f;
        while (shown < line.Length)
        {
            shown += charsPerSecond * Time.unscaledDeltaTime;
            bubbleText.maxVisibleCharacters = Mathf.Min(line.Length, Mathf.FloorToInt(shown));
            yield return null;
        }
        bubbleText.maxVisibleCharacters = line.Length;

        yield return new WaitForSecondsRealtime(hold);
        bubble.SetActive(false);
        speakRoutine = null;
    }

    // ---------- UI ----------

    void SetVisible(bool v) { bubble.SetActive(v); SetButtonVisible(v); }
    void SetButtonVisible(bool v) { if (hintButton != null) hintButton.SetActive(v); }

    void Build()
    {
        Transform parent = transform.Find("SafeAreaRoot");
        if (parent == null) parent = transform;

        root = new GameObject("YuccaVoiceRoot", typeof(RectTransform));
        root.layer = parent.gameObject.layer;
        var rrt = (RectTransform)root.transform;
        rrt.SetParent(parent, false);
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
        rrt.offsetMin = rrt.offsetMax = Vector2.zero;
        rrt.SetAsFirstSibling();   // draws beneath panels (settings, results)

        // bubble
        bubble = NewImage("YuccaBubble", root.transform, bubbleColor);
        var brt = (RectTransform)bubble.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.anchoredPosition = bubblePosition;
        brt.sizeDelta = bubbleSize;
        bubble.GetComponent<Image>().raycastTarget = false;

        var nameTag = NewText("Name", bubble.transform, "YUCCA", 26f, FontStyles.Bold, accent, TextAlignmentOptions.TopLeft);
        var nrt = (RectTransform)nameTag.transform;
        nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one;
        nrt.offsetMin = new Vector2(32f, 0f); nrt.offsetMax = new Vector2(-32f, -14f);

        bubbleText = NewText("Line", bubble.transform, "", 34f, FontStyles.Normal, textColor, TextAlignmentOptions.Left);
        var trt = (RectTransform)bubbleText.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(32f, 14f); trt.offsetMax = new Vector2(-32f, -48f);
        bubbleText.textWrappingMode = TextWrappingModes.Normal;

        // hint button
        hintButton = NewImage("HintButton", root.transform, bubbleColor);
        var hrt = (RectTransform)hintButton.transform;
        hrt.anchorMin = hrt.anchorMax = new Vector2(1f, 0f);
        hrt.pivot = new Vector2(1f, 0f);
        hrt.anchoredPosition = hintButtonPosition;
        hrt.sizeDelta = new Vector2(190f, 130f);

        var fillGo = NewImage("Cooldown", hintButton.transform, new Color(accent.r, accent.g, accent.b, 0.35f));
        var frt = (RectTransform)fillGo.transform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        hintFill = fillGo.GetComponent<Image>();
        hintFill.sprite = null; hintFill.type = Image.Type.Filled;
        hintFill.fillMethod = Image.FillMethod.Horizontal;
        hintFill.raycastTarget = false;

        hintLabel = NewText("Label", hintButton.transform, "HINT", 40f, FontStyles.Bold, accent, TextAlignmentOptions.Center);
        var lrt = (RectTransform)hintLabel.transform;
        lrt.anchorMin = new Vector2(0f, 0.4f); lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        hintSub = NewText("Sub", hintButton.transform, "", 24f, FontStyles.Normal, textColor * 0.85f, TextAlignmentOptions.Center);
        var srt = (RectTransform)hintSub.transform;
        srt.anchorMin = Vector2.zero; srt.anchorMax = new Vector2(1f, 0.45f);
        srt.offsetMin = srt.offsetMax = Vector2.zero;

        var btn = hintButton.AddComponent<Button>();
        btn.targetGraphic = hintButton.GetComponent<Image>();
        btn.onClick.AddListener(OnHintPressed);
    }

    GameObject NewImage(string name, Transform parent, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = cardSprite;
        img.type = cardSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = c;
        return go;
    }

    TextMeshProUGUI NewText(string name, Transform parent, string s, float size, FontStyles style, Color c, TextAlignmentOptions a)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size; t.fontStyle = style; t.color = c; t.alignment = a;
        t.enableAutoSizing = false; t.raycastTarget = false; t.text = s;
        return t;
    }
}
