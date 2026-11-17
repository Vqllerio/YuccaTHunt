using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UiPopIn : MonoBehaviour
{
    public float duration = 0.3f;
    public float startScale = 0.92f;
    public float slideFromY = 0f;
    [Tooltip("Whoosh volume multiplier when this panel opens. 0 = silent.")]
    public float soundVolume = 1f;

    RectTransform rt;
    CanvasGroup group;
    Vector2 basePos;
    bool captured;
    bool controlsAlpha;

    void Capture()
    {
        if (captured) return;
        rt = (RectTransform)transform;
        basePos = rt.anchoredPosition;
        controlsAlpha = GetComponent<CanvasGroupFader>() == null;
        if (controlsAlpha)
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        }
        captured = true;
    }

    void OnEnable()
    {
        Capture();
        StopAllCoroutines();
        if (soundVolume > 0f && Time.timeSinceLevelLoad > 0.5f) SfxPlayer.PlayOpen(soundVolume);
        StartCoroutine(Play());
    }

    void OnDisable()
    {
        if (!captured) return;
        rt.localScale = Vector3.one;
        rt.anchoredPosition = basePos;
        if (controlsAlpha && group != null) group.alpha = 1f;
    }

    IEnumerator Play()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = EaseOutBack(k);
            rt.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1f, e);
            rt.anchoredPosition = basePos + new Vector2(0f, Mathf.LerpUnclamped(slideFromY, 0f, e));
            if (controlsAlpha) group.alpha = Mathf.Clamp01(k * 1.6f);
            yield return null;
        }
        rt.localScale = Vector3.one;
        rt.anchoredPosition = basePos;
        if (controlsAlpha) group.alpha = 1f;
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}
