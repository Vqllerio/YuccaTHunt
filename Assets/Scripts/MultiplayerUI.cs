using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The online race screens: lobby with ready-up, the shared countdown, and live standings.
/// Builds its own UI and only runs when the backend is configured. Sits on the Canvas.
/// </summary>
public class MultiplayerUI : MonoBehaviour
{
    public TMP_FontAsset font;
    public Sprite cardSprite;

    [Header("Colors")]
    public Color background = new Color32(10, 12, 30, 235);
    public Color cardColor = new Color32(26, 29, 62, 255);
    public Color accent = new Color32(255, 166, 48, 255);
    public Color onAccent = new Color32(20, 22, 43, 255);
    public Color textColor = new Color32(240, 240, 248, 255);

    SessionController sc;
    EntranceTrigger entrance;

    GameObject overlay;
    GameObject lobbyCard;
    GameObject countdownView;
    GameObject nameRow;
    GameObject joinedGroup;
    TMP_InputField nameInput;
    Button readyButton;
    TextMeshProUGUI readyLabel;
    TextMeshProUGUI statusText;
    TextMeshProUGUI errorText;
    TextMeshProUGUI countdownText;
    TextMeshProUGUI standingsText;
    Transform playerList;

    bool lobbyOpen;
    bool raceBegan;
    bool iAmReady;
    float hideOverlayAt = -1f;
    string lastListSignature = "";

    void Start()
    {
        if (!YuccaBackendConfig.IsConfigured) { enabled = false; return; }

        sc = SessionController.EnsureExists();
        sc.OnChanged += Refresh;
        sc.OnError += ShowError;

        BuildOverlay();
        BuildStandings();
        overlay.SetActive(false);

        entrance = FindAnyObjectByType<EntranceTrigger>();
        if (entrance != null)
        {
            if (entrance.HasIntroBeenClosed) OpenLobby();
            else entrance.OnIntroClosed += OpenLobby;
        }
    }

    void OnDestroy()
    {
        if (sc != null)
        {
            sc.OnChanged -= Refresh;
            sc.OnError -= ShowError;
        }
        if (entrance != null) entrance.OnIntroClosed -= OpenLobby;
    }

    // ---------- flow ----------

    void OpenLobby()
    {
        if (!MultiplayerGate.LobbyRequired || lobbyOpen) return;
        lobbyOpen = true;
        MultiplayerGate.ScanningAllowed = false;
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        lobbyCard.SetActive(true);
        countdownView.SetActive(false);
        errorText.text = "";

        string saved = PlayerProfile.DisplayName;
        nameInput.text = saved;
        nameRow.SetActive(true);
        joinedGroup.SetActive(false);

        // Returning players join straight away with their saved name.
        if (!string.IsNullOrEmpty(saved)) DoJoin();
    }

    void DoJoin()
    {
        string name = (nameInput.text ?? "").Trim();
        if (name.Length == 0) name = "Player";
        PlayerProfile.DisplayName = name;

        errorText.text = "";
        nameRow.SetActive(false);
        joinedGroup.SetActive(true);
        statusText.text = "Joining as " + name + "...";
        iAmReady = false;
        readyLabel.text = "I'm Ready";
        sc.Join(name);
    }

    void ToggleReady()
    {
        iAmReady = !iAmReady;
        readyLabel.text = iAmReady ? "Not Ready" : "I'm Ready";
        sc.SetReady(iAmReady);
    }

    void PlaySolo()
    {
        MultiplayerGate.SoloChosen = true;
        sc.Leave();
        MultiplayerGate.ScanningAllowed = true;
        overlay.SetActive(false);
        lobbyOpen = false;
        if (RaceTimer.Instance != null) RaceTimer.Instance.StartRun();
    }

    void BeginRace()
    {
        if (raceBegan || sc.Info == null) return;
        raceBegan = true;
        MultiplayerGate.ScanningAllowed = true;
        if (RaceTimer.Instance != null) RaceTimer.Instance.StartRunAtServer(sc.Info.start_at_ms);
        countdownText.text = "SCAN!";
        hideOverlayAt = Time.unscaledTime + 0.8f;
    }

    void ShowError(string message)
    {
        if (!lobbyOpen || raceBegan) return;
        errorText.text = "Can't reach the race server. Check your connection, or play solo.";
        if (!sc.Joined)
        {
            nameRow.SetActive(true);
            joinedGroup.SetActive(false);
        }
    }

    // ---------- updating the screens ----------

    void Refresh()
    {
        if (sc.Info == null) return;
        errorText.text = "";

        switch (sc.State)
        {
            case SessionState.Lobby:
                if (lobbyOpen && !raceBegan)
                {
                    overlay.SetActive(true);
                    lobbyCard.SetActive(true);
                    countdownView.SetActive(false);
                    RefreshLobby();
                }
                break;

            case SessionState.Countdown:
                if (lobbyOpen && !raceBegan)
                {
                    overlay.SetActive(true);
                    lobbyCard.SetActive(false);
                    countdownView.SetActive(true);
                }
                break;

            case SessionState.Racing:
                if (!raceBegan) BeginRace();
                break;
        }

        if (sc.State == SessionState.Racing || sc.State == SessionState.Ended)
            RefreshStandings();
    }

    void RefreshLobby()
    {
        var info = sc.Info;
        int n = info.players != null ? info.players.Length : 0;
        int need = Mathf.Max(1, info.min_players);

        var sig = new StringBuilder();
        if (info.players != null)
            foreach (var p in info.players)
                sig.Append(p.player_id).Append(p.display_name).Append(p.ready).Append('|');

        string me = PlayerProfile.Id;
        if (info.players != null)
            foreach (var p in info.players)
                if (p.player_id == me) { iAmReady = p.ready; readyLabel.text = p.ready ? "Not Ready" : "I'm Ready"; }

        statusText.text = n < need
            ? "Waiting for players (" + n + "/" + need + ")"
            : "Race starts when everyone is ready";

        if (sig.ToString() == lastListSignature) return;
        lastListSignature = sig.ToString();

        for (int i = playerList.childCount - 1; i >= 0; i--)
            Destroy(playerList.GetChild(i).gameObject);

        if (info.players == null) return;
        foreach (var p in info.players)
        {
            bool mine = p.player_id == me;
            AddPlayerRow(p.display_name + (mine ? " (you)" : ""), p.ready ? "READY" : "waiting", p.ready, mine);
        }
    }

    void RefreshStandings()
    {
        var info = sc.Info;
        if (info == null || info.players == null) return;

        var finished = new List<PlayerInfo>();
        foreach (var p in info.players) if (p.finished) finished.Add(p);
        finished.Sort((a, b) => a.duration_ms.CompareTo(b.duration_ms));

        if (finished.Count == 0)
        {
            standingsText.text = "";
            return;
        }

        var sb = new StringBuilder();
        sb.Append(finished.Count).Append('/').Append(info.players.Length).Append(" finished");
        for (int i = 0; i < finished.Count && i < 3; i++)
            sb.Append('\n').Append(i + 1).Append(". ").Append(finished[i].display_name)
              .Append("  ").Append(RaceTimer.FormatPrecise(finished[i].duration_ms));
        standingsText.text = sb.ToString();
    }

    void Update()
    {
        if (countdownView != null && countdownView.activeSelf && !raceBegan)
        {
            float left = sc.SecondsUntilStart;
            if (left > 0f)
            {
                countdownText.text = Mathf.CeilToInt(left).ToString();
            }
            else if (sc.Info != null)
            {
                BeginRace();
            }
        }

        if (hideOverlayAt > 0f && Time.unscaledTime >= hideOverlayAt)
        {
            hideOverlayAt = -1f;
            overlay.SetActive(false);
        }
    }

    // ---------- building the UI ----------

    void BuildOverlay()
    {
        overlay = New("MultiplayerOverlay", transform);
        Stretch((RectTransform)overlay.transform);
        var bg = overlay.AddComponent<Image>();
        bg.color = background;
        bg.raycastTarget = true;

        BuildLobbyCard(overlay.transform);
        BuildCountdown(overlay.transform);
    }

    void BuildLobbyCard(Transform parent)
    {
        lobbyCard = New("LobbyCard", parent);
        var rt = (RectTransform)lobbyCard.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(940f, 0f);

        var img = lobbyCard.AddComponent<Image>();
        ApplySprite(img, cardColor);

        var v = lobbyCard.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(48, 48, 48, 48);
        v.spacing = 24f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        lobbyCard.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        MakeText("Title", lobbyCard.transform, "RACE LOBBY", 56f, FontStyles.Bold, accent, TextAlignmentOptions.Center);
        MakeText("Sub", lobbyCard.transform, "Scan all the memories before anyone else.", 32f, FontStyles.Italic,
                 new Color(textColor.r, textColor.g, textColor.b, 0.75f), TextAlignmentOptions.Center);

        // name entry (shown until you join)
        nameRow = New("NameRow", lobbyCard.transform);
        var h = nameRow.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 16f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        var rowLe = nameRow.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 110f;
        rowLe.minHeight = 110f;
        BuildNameInput(nameRow.transform);
        MakeButton(nameRow.transform, "Join", true, 110f, 260f, DoJoin);

        // after joining
        joinedGroup = New("Joined", lobbyCard.transform);
        var jv = joinedGroup.AddComponent<VerticalLayoutGroup>();
        jv.spacing = 18f;
        jv.childAlignment = TextAnchor.UpperCenter;
        jv.childControlWidth = true;
        jv.childControlHeight = true;
        jv.childForceExpandWidth = true;
        jv.childForceExpandHeight = false;

        statusText = MakeText("Status", joinedGroup.transform, "", 34f, FontStyles.Bold, textColor,
                              TextAlignmentOptions.Center);

        var list = New("PlayerList", joinedGroup.transform);
        var lv = list.AddComponent<VerticalLayoutGroup>();
        lv.spacing = 8f;
        lv.childControlWidth = true;
        lv.childControlHeight = true;
        lv.childForceExpandWidth = true;
        lv.childForceExpandHeight = false;
        playerList = list.transform;

        readyButton = MakeButton(joinedGroup.transform, "I'm Ready", true, 130f, -1f, ToggleReady, out readyLabel);

        errorText = MakeText("Error", lobbyCard.transform, "", 30f, FontStyles.Normal,
                             new Color32(255, 120, 120, 255), TextAlignmentOptions.Center);

        MakeButton(lobbyCard.transform, "Play Solo Instead", false, 110f, -1f, PlaySolo);
    }

    void BuildNameInput(Transform parent)
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
                                   new Color(textColor.r, textColor.g, textColor.b, 0.4f),
                                   TextAlignmentOptions.MidlineLeft);
        Stretch(placeholder.rectTransform);
        var text = MakeText("Text", area.transform, "", 42f, FontStyles.Normal, textColor,
                            TextAlignmentOptions.MidlineLeft);
        Stretch(text.rectTransform);

        nameInput = go.AddComponent<TMP_InputField>();
        nameInput.textViewport = art;
        nameInput.textComponent = text;
        nameInput.placeholder = placeholder;
        nameInput.targetGraphic = bg;
        nameInput.characterLimit = 14;
        nameInput.lineType = TMP_InputField.LineType.SingleLine;
    }

    void BuildCountdown(Transform parent)
    {
        countdownView = New("Countdown", parent);
        Stretch((RectTransform)countdownView.transform);
        countdownText = MakeText("Number", countdownView.transform, "", 320f, FontStyles.Bold, accent,
                                 TextAlignmentOptions.Center);
        Stretch(countdownText.rectTransform);
        var label = MakeText("Label", countdownView.transform, "Get ready to scan", 44f, FontStyles.Italic,
                             textColor, TextAlignmentOptions.Center);
        var lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0f, 0.5f);
        lrt.anchorMax = new Vector2(1f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.anchoredPosition = new Vector2(0f, -260f);
        lrt.sizeDelta = new Vector2(0f, 80f);
    }

    void BuildStandings()
    {
        Transform parent = transform.Find("SafeAreaRoot");
        if (parent == null) parent = transform;

        var go = New("Standings", parent);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -166f);
        rt.sizeDelta = new Vector2(700f, 150f);

        standingsText = go.AddComponent<TextMeshProUGUI>();
        if (font != null) standingsText.font = font;
        standingsText.fontSize = 30f;
        standingsText.fontStyle = FontStyles.Bold;
        standingsText.color = accent;
        standingsText.alignment = TextAlignmentOptions.Top;
        standingsText.enableAutoSizing = false;
        standingsText.raycastTarget = false;
        standingsText.text = "";
    }

    void AddPlayerRow(string name, string status, bool ready, bool mine)
    {
        var row = New("Row", playerList);
        var img = row.AddComponent<Image>();
        img.color = mine ? new Color(accent.r, accent.g, accent.b, 0.2f) : new Color(1f, 1f, 1f, 0.06f);
        img.raycastTarget = false;

        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(24, 24, 14, 14);
        h.spacing = 12f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        var n = MakeText("Name", row.transform, name, 38f, mine ? FontStyles.Bold : FontStyles.Normal,
                         textColor, TextAlignmentOptions.MidlineLeft);
        n.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        n.overflowMode = TextOverflowModes.Ellipsis;
        var s = MakeText("State", row.transform, status, 32f, FontStyles.Bold,
                         ready ? new Color32(120, 220, 150, 255) : new Color(textColor.r, textColor.g, textColor.b, 0.5f),
                         TextAlignmentOptions.MidlineRight);
        s.gameObject.AddComponent<LayoutElement>().preferredWidth = 190f;
    }

    // ---------- helpers ----------

    Button MakeButton(Transform parent, string label, bool primary, float height, float width,
                      UnityEngine.Events.UnityAction onClick)
    {
        TextMeshProUGUI unused;
        return MakeButton(parent, label, primary, height, width, onClick, out unused);
    }

    Button MakeButton(Transform parent, string label, bool primary, float height, float width,
                      UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI labelText)
    {
        var go = New(label.Replace(" ", "").Replace("'", "") + "Button", parent);
        var img = go.AddComponent<Image>();
        ApplySprite(img, primary ? accent : new Color(1f, 1f, 1f, 0.16f));
        img.raycastTarget = true;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        if (width > 0f) le.preferredWidth = width;
        go.AddComponent<ButtonPressFx>();

        labelText = MakeText("Label", go.transform, label, 44f, FontStyles.Bold,
                             primary ? onAccent : textColor, TextAlignmentOptions.Center);
        Stretch(labelText.rectTransform);
        return btn;
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
