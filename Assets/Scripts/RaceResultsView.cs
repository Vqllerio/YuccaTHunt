using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the "your time" card (with name entry and Save) and the Top 10 card
/// inside the end-of-quest scroll list.
/// </summary>
public class RaceResultsView
{
    readonly TMP_FontAsset font;
    readonly Sprite cardSprite;
    readonly Color cardColor, accent, onAccent, textColor;
    readonly List<CanvasGroup> reveal;

    TMP_InputField input;
    Button saveButton;
    TextMeshProUGUI saveLabel, bestLine, statusLine;
    Transform listRoot;
    Transform raceRoot;
    TextMeshProUGUI raceSummary;
    int durationMs;

    public RaceResultsView(TMP_FontAsset font, Sprite cardSprite, Color cardColor, Color accent,
                           Color onAccent, Color textColor, List<CanvasGroup> reveal)
    {
        this.font = font;
        this.cardSprite = cardSprite;
        this.cardColor = cardColor;
        this.accent = accent;
        this.onAccent = onAccent;
        this.textColor = textColor;
        this.reveal = reveal;
    }

    public void Build(Transform parent, int durationMs)
    {
        this.durationMs = durationMs;
        bool online = MultiplayerGate.Online && SessionController.Instance != null;
        BuildTimeCard(parent);
        if (online) BuildRaceCard(parent);
        BuildTopCard(parent);
        RefreshBest();
        RefreshTop();
        if (online)
        {
            SessionController.Instance.OnChanged += RefreshRace;
            RefreshRace();
        }
    }

    // ---------- cards ----------

    void BuildTimeCard(Transform parent)
    {
        var card = NewCard("TimeCard", parent, cardColor);
        var v = card.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(40, 40, 34, 34);
        v.spacing = 14f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        reveal.Add(card.AddComponent<CanvasGroup>());

        MakeText("Label", card.transform, MultiplayerGate.Online ? "YOUR RACE TIME" : "YOUR TIME  -  SOLO RUN", 30f, FontStyles.Bold, accent,
             TextAlignmentOptions.Center);
        MakeText("Time", card.transform, RaceTimer.FormatPrecise(durationMs), 104f, FontStyles.Bold,
             textColor, TextAlignmentOptions.Center);
        bestLine = MakeText("Best", card.transform, "", 30f, FontStyles.Normal,
                        new Color(textColor.r, textColor.g, textColor.b, 0.7f), TextAlignmentOptions.Center);

        bool auto = Leaderboard.Backend.SubmitsAutomatically;
        if (!auto)
        {
            var row = New("NameRow", card.transform);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 110f;
            rowLe.minHeight = 110f;

            BuildInput(row.transform);
            BuildSaveButton(row.transform);

        }

        statusLine = MakeText("Status", card.transform, "", 32f, FontStyles.Bold, accent, TextAlignmentOptions.Center);
        statusLine.gameObject.SetActive(false);
        if (auto) ShowServerStatus();
    }

    void ShowServerStatus()
    {
        var sc = SessionController.Instance;
        var r = sc != null ? sc.LastResult : null;
        if (r == null || !r.ok) return;
        statusLine.text = (r.new_best ? "New personal best! " : "") + "You are #" + r.rank + " all-time.";
        statusLine.gameObject.SetActive(true);
    }

    void BuildInput(Transform parent)
    {
        var go = New("NameField", parent);
        var bg = go.AddComponent<Image>();
        ApplySprite(bg, new Color(1f, 1f, 1f, 0.12f));
        bg.raycastTarget = true;
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.preferredHeight = 110f;

        var area = New("TextArea", go.transform);
        var art = (RectTransform)area.transform;
        art.anchorMin = Vector2.zero;
        art.anchorMax = Vector2.one;
        art.offsetMin = new Vector2(28f, 8f);
        art.offsetMax = new Vector2(-28f, -8f);
        area.AddComponent<RectMask2D>();

        var placeholder = MakeText("Placeholder", area.transform, "Your name", 42f, FontStyles.Italic,
                               new Color(textColor.r, textColor.g, textColor.b, 0.4f), TextAlignmentOptions.MidlineLeft);
        Stretch(placeholder.rectTransform);
        var text = MakeText("Text", area.transform, "", 42f, FontStyles.Normal, textColor, TextAlignmentOptions.MidlineLeft);
        Stretch(text.rectTransform);

        input = go.AddComponent<TMP_InputField>();
        input.textViewport = art;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.targetGraphic = bg;
        input.characterLimit = 14;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.text = PlayerProfile.DisplayName;
    }

    void BuildSaveButton(Transform parent)
    {
        var go = New("SaveButton", parent);
        var img = go.AddComponent<Image>();
        ApplySprite(img, accent);
        img.raycastTarget = true;
        saveButton = go.AddComponent<Button>();
        saveButton.targetGraphic = img;
        saveButton.onClick.AddListener(OnSave);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 280f;
        le.preferredHeight = 110f;
        go.AddComponent<ButtonPressFx>();

        saveLabel = MakeText("Label", go.transform, "Save", 44f, FontStyles.Bold, onAccent, TextAlignmentOptions.Center);
        Stretch(saveLabel.rectTransform);
    }

    void BuildRaceCard(Transform parent)
    {
        var card = NewCard("RaceCard", parent, cardColor);
        var v = card.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(34, 34, 30, 30);
        v.spacing = 8f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        reveal.Add(card.AddComponent<CanvasGroup>());

        MakeText("Title", card.transform, "THIS RACE", 30f, FontStyles.Bold, accent, TextAlignmentOptions.Center);
        raceSummary = MakeText("Summary", card.transform, "", 34f, FontStyles.Bold, textColor,
                               TextAlignmentOptions.Center);

        var list = New("List", card.transform);
        var lv = list.AddComponent<VerticalLayoutGroup>();
        lv.spacing = 6f;
        lv.childControlWidth = true;
        lv.childControlHeight = true;
        lv.childForceExpandWidth = true;
        lv.childForceExpandHeight = false;
        raceRoot = list.transform;
    }

    void RefreshRace()
    {
        var sc = SessionController.Instance;
        if (raceRoot == null)
        {
            if (sc != null) sc.OnChanged -= RefreshRace;
            return;
        }
        if (sc == null || sc.Info == null || sc.Info.players == null) return;

        var players = new List<PlayerInfo>(sc.Info.players);
        players.Sort((a, b) =>
        {
            if (a.finished != b.finished) return a.finished ? -1 : 1;
            return a.duration_ms.CompareTo(b.duration_ms);
        });

        for (int i = raceRoot.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(raceRoot.GetChild(i).gameObject);

        string me = PlayerProfile.Id;
        int finishedCount = 0;
        int myPlace = 0;
        foreach (var p in players)
        {
            bool mine = p.player_id == me;
            if (p.finished)
            {
                finishedCount++;
                if (mine) myPlace = finishedCount;
                Row(raceRoot, finishedCount.ToString(), p.display_name,
                    RaceTimer.FormatPrecise(p.duration_ms), mine);
            }
            else
            {
                Row(raceRoot, "-", p.display_name, "racing...", mine);
            }
        }

        if (myPlace > 0)
            raceSummary.text = "You placed " + Ordinal(myPlace) + " of " + players.Count +
                               (finishedCount < players.Count ? " so far" : "");
        else
            raceSummary.text = "";
    }

    static string Ordinal(int n)
    {
        int mod100 = n % 100;
        if (mod100 >= 11 && mod100 <= 13) return n + "th";
        switch (n % 10)
        {
            case 1: return n + "st";
            case 2: return n + "nd";
            case 3: return n + "rd";
            default: return n + "th";
        }
    }

    void BuildTopCard(Transform parent)
    {
        var card = NewCard("TopCard", parent, cardColor);
        var v = card.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(34, 34, 30, 30);
        v.spacing = 8f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        reveal.Add(card.AddComponent<CanvasGroup>());

        MakeText("Title", card.transform, Leaderboard.Backend.BoardTitle, 30f, FontStyles.Bold, accent,
             TextAlignmentOptions.Center);

        var list = New("List", card.transform);
        var lv = list.AddComponent<VerticalLayoutGroup>();
        lv.spacing = 6f;
        lv.childControlWidth = true;
        lv.childControlHeight = true;
        lv.childForceExpandWidth = true;
        lv.childForceExpandHeight = false;
        listRoot = list.transform;
    }

    // ---------- data ----------

    void RefreshBest()
    {
        Leaderboard.Backend.GetBest(PlayerProfile.Id, e =>
        {
            bestLine.text = e != null
                ? "Personal best: " + RaceTimer.FormatPrecise(e.durationMs)
                : "Your first run";
        });
    }

    void RefreshTop()
    {
        for (int i = listRoot.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(listRoot.GetChild(i).gameObject);

        Leaderboard.Backend.GetTop(10, entries =>
        {
            if (entries == null || entries.Count == 0)
            {
                Row(listRoot, "", "No runs saved yet", "", false);
                return;
            }
            string me = PlayerProfile.Id;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                Row(listRoot, (i + 1).ToString(), e.displayName, RaceTimer.FormatPrecise(e.durationMs),
                    e.playerId == me);
            }
        });
    }

    void Row(Transform parent, string rank, string name, string time, bool mine)
    {
        var row = New("Row", parent);
        var img = row.AddComponent<Image>();
        img.color = mine ? new Color(accent.r, accent.g, accent.b, 0.22f) : new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;

        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(16, 16, 10, 10);
        h.spacing = 12f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        var c = mine ? accent : textColor;
        var a = MakeText("Rank", row.transform, rank, 36f, FontStyles.Bold, c, TextAlignmentOptions.MidlineLeft);
        a.gameObject.AddComponent<LayoutElement>().preferredWidth = 70f;
        var b = MakeText("Name", row.transform, name, 36f, mine ? FontStyles.Bold : FontStyles.Normal, c,
                     TextAlignmentOptions.MidlineLeft);
        b.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        b.overflowMode = TextOverflowModes.Ellipsis;
        var t = MakeText("Time", row.transform, time, 36f, FontStyles.Bold, c, TextAlignmentOptions.MidlineRight);
        t.gameObject.AddComponent<LayoutElement>().preferredWidth = 230f;
    }

    void OnSave()
    {
        string name = (input.text ?? "").Trim();
        if (name.Length == 0) name = "Player";
        if (name.Length > 14) name = name.Substring(0, 14);
        PlayerProfile.DisplayName = name;

        var entry = new LeaderboardEntry
        {
            playerId = PlayerProfile.Id,
            displayName = name,
            durationMs = durationMs,
            finishedAtTicks = DateTime.UtcNow.Ticks
        };

        saveButton.interactable = false;
        Leaderboard.Backend.Submit(entry, (rank, newBest) =>
        {
            input.interactable = false;
            saveLabel.text = "Saved";
            statusLine.text = newBest
                ? "New personal best! You are #" + rank + "."
                : "You are #" + rank + ". Your best is still faster.";
            statusLine.gameObject.SetActive(true);
            RefreshBest();
            RefreshTop();
        });
    }

    // ---------- helpers ----------

    GameObject NewCard(string name, Transform parent, Color color)
    {
        var card = New(name, parent);
        var img = card.AddComponent<Image>();
        ApplySprite(img, color);
        return card;
    }

    void ApplySprite(Image img, Color color)
    {
        img.sprite = cardSprite;
        img.type = cardSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        img.raycastTarget = false;
    }

    TextMeshProUGUI MakeText(string name, Transform parent, string content, float size, FontStyles style,
                         Color color, TextAlignmentOptions align)
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
