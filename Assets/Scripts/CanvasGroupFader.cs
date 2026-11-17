using System.Collections;
using UnityEngine;

public class CanvasGroupFader : MonoBehaviour
{
    public CanvasGroup group;
    public float fadeInTime = 0.2f;
    public float fadeOutTime = 0.3f;

    void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
    }

    public void Show(float holdTime = 0f)
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group == null)
        {
            Debug.LogError($"[CanvasGroupFader] '{name}' needs a CanvasGroup.");
            return;
        }

        // A coroutine cannot start on an inactive object, so activate first.
        gameObject.SetActive(true);
        StopAllCoroutines();
        group.alpha = 0f;
        StartCoroutine(FadeRoutine(fadeInTime, 1f, holdTime, fadeOutTime));
    }

    public void Hide()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    IEnumerator FadeRoutine(float fadeIn, float peak, float hold, float fadeOut)
    {
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Clamp01(t / fadeIn) * peak;
            yield return null;
        }
        group.alpha = peak;

        if (hold > 0f) yield return new WaitForSeconds(hold);

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            group.alpha = peak * (1f - Mathf.Clamp01(t / fadeOut));
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
