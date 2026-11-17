using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rt;
    Rect lastSafeArea;
    Vector2Int lastScreen;

    void OnEnable()
    {
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea ||
            Screen.width != lastScreen.x ||
            Screen.height != lastScreen.y)
        {
            Apply();
        }
    }

    void Apply()
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect sa = Screen.safeArea;
        lastSafeArea = sa;
        lastScreen = new Vector2Int(Screen.width, Screen.height);

        Vector2 min = sa.position;
        Vector2 max = sa.position + sa.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
