using UnityEngine;
using UnityEngine.UI;

public class ProgressPips : MonoBehaviour
{
    public Sprite pipSprite;
    public Color offColor = new Color(1f, 1f, 1f, 0.25f);
    public Color onColor = new Color(1f, 0.65f, 0.19f, 1f);
    public float pipSize = 34f;

    Image[] pips;
    int lit;
    QuestManager qm;

    void Start()
    {
        qm = QuestManager.Instance;
        if (qm == null) return;

        Build(qm.TotalItems);
        qm.OnProgressChanged += Refresh;
        Refresh(qm.ScannedCount, qm.TotalItems);
    }

    void OnDestroy()
    {
        if (qm != null) qm.OnProgressChanged -= Refresh;
    }

    void Build(int count)
    {
        pips = new Image[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Pip" + (i + 1), typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = pipSize;
            le.preferredHeight = pipSize;

            var img = go.GetComponent<Image>();
            img.sprite = pipSprite;
            img.color = offColor;
            img.raycastTarget = false;
            pips[i] = img;
        }
    }

    void Refresh(int scanned, int total)
    {
        if (pips == null) return;

        for (int i = 0; i < pips.Length; i++)
        {
            bool on = i < scanned;
            pips[i].color = on ? onColor : offColor;
            if (on && i >= lit) pips[i].rectTransform.localScale = Vector3.one * 1.7f;
        }
        lit = scanned;
    }

    void Update()
    {
        if (pips == null) return;
        float k = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
        for (int i = 0; i < pips.Length; i++)
        {
            var t = pips[i].rectTransform;
            t.localScale = Vector3.Lerp(t.localScale, Vector3.one, k);
        }
    }
}
