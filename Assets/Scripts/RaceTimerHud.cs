using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the running time in a small pill at the top of the screen while a run is in progress.
/// Builds its own UI, so it only needs to sit on the Canvas.
/// </summary>
public class RaceTimerHud : MonoBehaviour
{
    public TMP_FontAsset font;
    public Sprite cardSprite;
    public Vector2 anchoredPosition = new Vector2(0f, -70f);
    public Vector2 size = new Vector2(280f, 84f);
    public Color pillColor = new Color32(26, 29, 62, 235);
    public Color textColor = new Color32(255, 166, 48, 255);

    RaceTimer timer;
    GameObject pill;
    TextMeshProUGUI label;
    int lastShownSecond = -1;

    void Start()
    {
        BuildPill();
        pill.SetActive(false);
    }

    void BuildPill()
    {
        Transform parent = transform.Find("SafeAreaRoot");
        if (parent == null) parent = transform;

        pill = new GameObject("TimerPill", typeof(RectTransform));
        pill.layer = parent.gameObject.layer;
        var rt = (RectTransform)pill.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        var img = pill.AddComponent<Image>();
        img.sprite = cardSprite;
        img.type = cardSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = pillColor;
        img.raycastTarget = false;

        var textGo = new GameObject("Time", typeof(RectTransform));
        textGo.layer = pill.layer;
        var trt = (RectTransform)textGo.transform;
        trt.SetParent(pill.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        label = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.fontSize = 46f;
        label.fontStyle = FontStyles.Bold;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = false;
        label.raycastTarget = false;
        label.text = "00:00";
    }

    void Update()
    {
        if (timer == null)
        {
            timer = RaceTimer.Instance;
            if (timer == null) return;
        }

        bool show = timer.IsRunning;
        if (pill.activeSelf != show) pill.SetActive(show);
        if (!show) return;

        int sec = Mathf.FloorToInt(timer.Elapsed);
        if (sec != lastShownSecond)
        {
            lastShownSecond = sec;
            label.text = RaceTimer.Format(timer.Elapsed);
        }
    }
}
