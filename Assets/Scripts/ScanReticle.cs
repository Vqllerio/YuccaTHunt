using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ScanReticle : MonoBehaviour
{
    public Color idleColor = new Color(1f, 0.65f, 0.19f, 1f);
    public Color flashColor = Color.white;
    public float pulseSpeed = 2.2f;

    Image img;
    RectTransform rt;
    QuestManager qm;
    float flash;

    void Start()
    {
        img = GetComponent<Image>();
        rt = (RectTransform)transform;

        qm = QuestManager.Instance;
        if (qm != null)
        {
            qm.OnItemScanned += OnScan;
            qm.OnQuestCompleted += OnDone;
        }
    }

    void OnDestroy()
    {
        if (qm == null) return;
        qm.OnItemScanned -= OnScan;
        qm.OnQuestCompleted -= OnDone;
    }

    void OnScan(QuestItem item) { flash = 1f; }

    void OnDone() { gameObject.SetActive(false); }

    void Update()
    {
        if (img == null) return;

        flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 2.5f);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);

        Color c = Color.Lerp(idleColor, flashColor, flash);
        c.a = Mathf.Clamp01(Mathf.Lerp(0.35f, 0.9f, pulse) + flash * 0.2f);
        img.color = c;

        rt.localScale = Vector3.one * (1f + 0.03f * pulse + 0.15f * flash);
    }
}
