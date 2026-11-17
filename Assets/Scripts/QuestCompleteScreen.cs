using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds and shows the end-of-quest screen: a header, one card per memory,
// Yucca's closing line, and two buttons (Play Again / Keep Exploring).
// Put this on the Canvas. It replaces the old text-only QuestSummaryUI.
public class QuestCompleteScreen : MonoBehaviour
{
    [Header("Wiring")]
    public GameObject legacySummaryPanel;
    public AudioSource closingAudio;
    public TMP_Text hintText;
    public TMP_FontAsset font;
    public Sprite cardSprite;
    public Sprite pipSprite;
    public Sprite checkSprite;
    public Sprite portraitSprite;

    [Header("Behaviour")]
    public float delayBeforeShow = 1.8f;
    [TextArea]
    public string closingQuote = "That's what I remember. Now you remember it too. Welcome to the fifth floor.";
    public string completedHint = "All memories restored. Tap SUMMARY to read them again.";

    [Header("Colors")]
    public Color background = new Color32(10, 12, 30, 250);
    public Color cardColor = new Color32(26, 29, 62, 255);
    public Color accent = new Color32(255, 166, 48, 255);
    public Color onAccent = new Color32(20, 22, 43, 255);
    public Color textColor = new Color32(240, 240, 248, 255);

    QuestManager qm;
    GameObject screen;
    CanvasGroup screenGroup;
    GameObject reopenChip;
    Coroutine routine;
    readonly List<CanvasGroup> revealItems = new List<CanvasGroup>();

    void Start()
    {
        EnsureRaceSystem();
        if (legacySummaryPanel != null) legacySummaryPanel.SetActive(false);

        qm = QuestManager.Instance;
        if (qm == null)
        {
            Debug.LogError("[QuestCompleteScreen] No QuestManager in scene.");
            return;
        }
        qm.OnQuestCompleted += OnCompleted;
    }

    void OnDestroy()
    {
        if (qm != null) qm.OnQuestCompleted -= OnCompleted;
    }

    void OnCompleted()
    {
        if (hintText != null) hintText.text = completedHint;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(delayBeforeShow, true));
    }

    // ---------- show / hide ----------

    IEnumerator ShowRoutine(float delay, bool playAudio)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        // Online: give the server a moment to confirm the finish time before showing results.
        if (playAudio && MultiplayerGate.Online && SessionController.Instance != null)
        {
            float waited = 0f;
            while (SessionController.Instance.LastResult == null && waited < 6f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (screen == null) Build();
        if (reopenChip != null) reopenChip.SetActive(false);

        screen.transform.SetAsLastSibling();
        screen.SetActive(true);
        screenGroup.alpha = 0f;
        foreach (var g in revealItems)
        {
            g.alpha = 0f;
            g.transform.localScale = Vector3.one * 0.94f;
        }

        if (playAudio && closingAudio != null) closingAudio.Play();

        yield return Fade(screenGroup, 0f, 1f, 0.35f);

        foreach (var g in revealItems)
        {
            StartCoroutine(Pop(g));
            yield return new WaitForSecondsRealtime(0.11f);
        }
    }

    IEnumerator CloseRoutine()
    {
        yield return Fade(screenGroup, 1f, 0f, 0.25f);
        screen.SetActive(false);
        if (reopenChip == null) BuildChip();
        reopenChip.SetActive(true);
        reopenChip.transform.SetAsLastSibling();
    }

    IEnumerator Fade(CanvasGroup g, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        g.alpha = to;
    }

    IEnumerator Pop(CanvasGroup g)
    {
        float t = 0f;
        const float duration = 0.3f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = 1f - (1f - k) * (1f - k) * (1f - k);
            g.alpha = e;
            g.transform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, e);
            yield return null;
        }
        g.alpha = 1f;
        g.transform.localScale = Vector3.one;
    }

    void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void CloseScreen()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CloseRoutine());
    }

    void Reopen()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(0f, false));
    }

    // ---------- building the UI ----------

    void Build()
    {
        revealItems.Clear();

        screen = New("QuestCompleteScreen", transform);
        Stretch(screen.GetComponent<RectTransform>());
        var bg = screen.AddComponent<Image>();
        bg.color = background;
        bg.raycastTarget = true;
        screenGroup = screen.AddComponent<CanvasGroup>();

        BuildHeader(screen.transform);
        BuildScroll(screen.transform);
        BuildFooter(screen.transform);
    }

    void BuildHeader(Transform parent)
    {
        var header = New("Header", parent);
        var rt = header.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -110f);
        rt.sizeDelta = Vector2.zero;

        var v = header.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.UpperCenter;
        v.spacing = 16f;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = false;
        v.childForceExpandHeight = false;
        header.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Sprite iconSprite = portraitSprite != null ? portraitSprite : checkSprite;
        if (iconSprite != null)
        {
            var icon = New("Icon", header.transform);
            var img = icon.AddComponent<Image>();
            img.sprite = iconSprite;
            img.color = portraitSprite != null ? Color.white : accent;
            img.preserveAspect = true;
            img.raycastTarget = false;
            var le = icon.AddComponent<LayoutElement>();
            le.preferredWidth = 200f;
            le.preferredHeight = 200f;
        }

        MakeText("Title", header.transform, MultiplayerGate.Online ? "Race Complete" : "Memories Restored", 72f, FontStyles.Bold, accent,
             TextAlignmentOptions.Center);

        int total = qm != null ? qm.TotalItems : 0;
        var sub = MakeText("Subtitle", header.transform,
                       (MultiplayerGate.Online ? "Online race: " : "Solo run: ") + (total > 0 ? "all " + total + " memories recovered" : "all memories recovered"),
                       34f, FontStyles.Normal, textColor * 0.8f, TextAlignmentOptions.Center);
        sub.color = new Color(textColor.r, textColor.g, textColor.b, 0.8f);

        if (pipSprite != null && total > 0)
        {
            var row = New("Pips", header.transform);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 16f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            row.AddComponent<LayoutElement>().preferredHeight = 36f;
            for (int i = 0; i < total; i++)
            {
                var pip = New("Pip" + (i + 1), row.transform);
                var pi = pip.AddComponent<Image>();
                pi.sprite = pipSprite;
                pi.color = accent;
                pi.raycastTarget = false;
                var ple = pip.AddComponent<LayoutElement>();
                ple.preferredWidth = 32f;
                ple.preferredHeight = 32f;
            }
        }
    }

    void BuildScroll(Transform parent)
    {
        var scroll = New("Scroll", parent);
        var srt = scroll.GetComponent<RectTransform>();
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.offsetMin = new Vector2(0f, 440f);
        srt.offsetMax = new Vector2(0f, -600f);

        var viewport = New("Viewport", scroll.transform);
        var vrt = viewport.GetComponent<RectTransform>();
        Stretch(vrt);
        viewport.AddComponent<RectMask2D>();
        var catcher = viewport.AddComponent<Image>();
        catcher.color = new Color(0f, 0f, 0f, 0f);
        catcher.raycastTarget = true;

        var content = New("Content", viewport.transform);
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = Vector2.zero;

        var v = content.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(56, 56, 8, 24);
        v.spacing = 22f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scroll.AddComponent<ScrollRect>();
        sr.viewport = vrt;
        sr.content = crt;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.scrollSensitivity = 40f;

        AddRaceResults(content.transform);

        if (qm != null && qm.Items != null)
        {
            int n = 1;
            foreach (var item in qm.Items)
            {
                if (item == null) continue;
                AddMemoryCard(content.transform, n, item);
                n++;
            }
        }
        AddQuoteCard(content.transform);
    }

    void EnsureRaceSystem()
    {
        RaceTimer.EnsureExists();
        if (GetComponent<RaceTimerHud>() == null)
        {
            var hud = gameObject.AddComponent<RaceTimerHud>();
            hud.font = font;
            hud.cardSprite = cardSprite;
        }
        if (GetComponent<YuccaVoice>() == null)
        {
            var voice = gameObject.AddComponent<YuccaVoice>();
            voice.font = font;
            voice.cardSprite = cardSprite;
        }
        if (YuccaBackendConfig.IsConfigured)
        {
            SessionController.EnsureExists();
            if (GetComponent<MultiplayerUI>() == null)
            {
                var mp = gameObject.AddComponent<MultiplayerUI>();
                mp.font = font;
                mp.cardSprite = cardSprite;
            }
        }
    }

    void AddRaceResults(Transform parent)
    {
        var timer = RaceTimer.Instance;
        if (timer == null || !timer.HasResult) return;
        new RaceResultsView(font, cardSprite, cardColor, accent, onAccent, textColor, revealItems)
            .Build(parent, timer.ElapsedMs);
    }

    void AddMemoryCard(Transform parent, int number, QuestItem item)
    {
        var card = New("Card" + number, parent);
        var img = card.AddComponent<Image>();
        ApplyCardSprite(img, cardColor);

        var h = card.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(30, 32, 26, 28);
        h.spacing = 26f;
        h.childAlignment = TextAnchor.UpperLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        revealItems.Add(card.AddComponent<CanvasGroup>());

        var badge = New("Badge", card.transform);
        var bi = badge.AddComponent<Image>();
        bi.sprite = pipSprite;
        bi.color = accent;
        bi.raycastTarget = false;
        var ble = badge.AddComponent<LayoutElement>();
        ble.preferredWidth = 76f;
        ble.minWidth = 76f;
        ble.preferredHeight = 76f;
        ble.minHeight = 76f;
        var num = MakeText("Number", badge.transform, number.ToString(), 40f, FontStyles.Bold, onAccent,
                       TextAlignmentOptions.Center);
        Stretch(num.rectTransform);

        var col = New("TextColumn", card.transform);
        var cv = col.AddComponent<VerticalLayoutGroup>();
        cv.spacing = 8f;
        cv.childControlWidth = true;
        cv.childControlHeight = true;
        cv.childForceExpandWidth = true;
        cv.childForceExpandHeight = false;
        col.AddComponent<LayoutElement>().flexibleWidth = 1f;

        MakeText("Name", col.transform, item.displayName, 40f, FontStyles.Bold, textColor,
             TextAlignmentOptions.Left);
        var memory = MakeText("Memory", col.transform, "\"" + item.memoryLine + "\"", 31f, FontStyles.Italic,
                          textColor, TextAlignmentOptions.Left);
        memory.color = new Color(textColor.r, textColor.g, textColor.b, 0.85f);
    }

    void AddQuoteCard(Transform parent)
    {
        var card = New("ClosingQuote", parent);
        var img = card.AddComponent<Image>();
        ApplyCardSprite(img, new Color(accent.r, accent.g, accent.b, 0.16f));

        var v = card.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(40, 40, 34, 30);
        v.spacing = 14f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        revealItems.Add(card.AddComponent<CanvasGroup>());

        MakeText("Quote", card.transform, "\"" + closingQuote + "\"", 36f, FontStyles.Italic, textColor,
             TextAlignmentOptions.Center);
        MakeText("Signature", card.transform, "Yucca", 30f, FontStyles.Bold, accent,
             TextAlignmentOptions.Right);
    }

    void BuildFooter(Transform parent)
    {
        var footer = New("Footer", parent);
        var rt = footer.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 70f);
        rt.sizeDelta = Vector2.zero;

        var v = footer.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(70, 70, 0, 0);
        v.spacing = 26f;
        v.childAlignment = TextAnchor.LowerCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        footer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        MakeButton(footer.transform, "Play Again", true, 140f, PlayAgain);
        MakeButton(footer.transform, "Keep Exploring", false, 120f, CloseScreen);
    }

    void BuildChip()
    {
        reopenChip = New("SummaryChip", transform);
        var rt = reopenChip.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-32f, -166f);
        rt.sizeDelta = new Vector2(330f, 90f);

        var img = reopenChip.AddComponent<Image>();
        ApplyCardSprite(img, cardColor);
        var btn = reopenChip.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Reopen);
        reopenChip.AddComponent<ButtonPressFx>();

        var label = MakeText("Label", reopenChip.transform, "SUMMARY", 32f, FontStyles.Bold, accent,
                         TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
    }

    void MakeButton(Transform parent, string label, bool primary, float height, UnityEngine.Events.UnityAction onClick)
    {
        var go = New(label.Replace(" ", "") + "Button", parent);
        var img = go.AddComponent<Image>();
        ApplyCardSprite(img, primary ? accent : new Color(1f, 1f, 1f, 0.18f));
        img.raycastTarget = true;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        go.AddComponent<ButtonPressFx>();

        var text = MakeText("Label", go.transform, label, 46f, FontStyles.Bold,
                        primary ? onAccent : textColor, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
    }

    // ---------- small helpers ----------

    void ApplyCardSprite(Image img, Color color)
    {
        img.sprite = cardSprite;
        img.type = cardSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        img.raycastTarget = false;
    }

    TextMeshProUGUI MakeText(string name, Transform parent, string content, float size,
                         FontStyles style, Color color, TextAlignmentOptions align)
    {
        var go = New(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = content;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.enableAutoSizing = false;
        t.raycastTarget = false;
        return t;
    }

    static GameObject New(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
