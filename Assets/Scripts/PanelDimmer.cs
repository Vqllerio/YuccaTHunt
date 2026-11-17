using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on a panel (the settings menu). While the panel is active, everything
/// else on the canvas is darkened and blocked from taps. Tapping the dark area closes the panel.
/// </summary>
public class PanelDimmer : MonoBehaviour
{
    [Range(0f, 1f)] public float dimAmount = 0.78f;
    public Color dimColor = new Color(0.02f, 0.03f, 0.07f, 1f);
    public float fadeSeconds = 0.22f;
    public bool closeOnTap = true;

    [Tooltip("These stay bright above the dark layer. Left empty, a sibling named GearButton is used.")]
    public RectTransform[] keepAbove;

    GameObject dim;
    Image dimImage;
    Coroutine routine;

    void OnEnable()
    {
        EnsureDim();
        dim.SetActive(true);
        dim.transform.SetAsLastSibling();
        transform.SetAsLastSibling();
        RaiseKept();
        Animate(dimAmount);
    }

    void RaiseKept()
    {
        if (keepAbove == null || keepAbove.Length == 0)
        {
            var gear = transform.parent != null ? transform.parent.Find("GearButton") : null;
            if (gear == null) return;
            gear.SetAsLastSibling();
            return;
        }
        foreach (var rt in keepAbove)
            if (rt != null && rt.parent == transform.parent) rt.SetAsLastSibling();
    }

    void OnDisable()
    {
        if (dim == null) return;
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (dimImage != null) dimImage.color = new Color(dimColor.r, dimColor.g, dimColor.b, 0f);
        dim.SetActive(false);
    }

    void OnDestroy()
    {
        if (dim != null) Destroy(dim);
    }

    void EnsureDim()
    {
        if (dim != null) return;

        dim = new GameObject(name + "_Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)dim.transform;
        rt.SetParent(transform.parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(8000f, 8000f);

        dimImage = dim.GetComponent<Image>();
        dimImage.color = new Color(dimColor.r, dimColor.g, dimColor.b, 0f);
        dimImage.raycastTarget = true;

        if (closeOnTap)
        {
            var btn = dim.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }

    void Animate(float target)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeTo(target));
    }

    IEnumerator FadeTo(float target)
    {
        float from = dimImage.color.a;
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, target, Mathf.Clamp01(t / fadeSeconds));
            dimImage.color = new Color(dimColor.r, dimColor.g, dimColor.b, a);
            yield return null;
        }
        dimImage.color = new Color(dimColor.r, dimColor.g, dimColor.b, target);
        routine = null;
    }
}
