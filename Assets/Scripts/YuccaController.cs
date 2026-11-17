using UnityEngine;

public class YuccaController : MonoBehaviour
{
    [Header("References")]
    public Renderer yuccaRenderer;
    public ParticleSystem glitchParticles;

    [Header("Progression")]
    [Range(0f, 1f)] public float minAlpha = 0.25f;
    [Range(0f, 1f)] public float maxAlpha = 1.0f;
    public float minGlitchRate = 30f;
    public float maxGlitchRate = 0f;
    public float transitionSpeed = 2f;

    [Header("Reaction")]
    public float scanBounceScale = 1.15f;
    public float scanBounceDuration = 0.3f;

    private QuestManager qm;
    private Material mat;
    private float targetAlpha;
    private float currentAlpha;
    private Vector3 baseScale;
    private float bounceTimer = -1f;

    void Start()
    {
        qm = QuestManager.Instance;
        if (yuccaRenderer != null) mat = yuccaRenderer.material;
        baseScale = transform.localScale;
        currentAlpha = minAlpha;
        if (mat != null) SetAlpha(currentAlpha);
        if (glitchParticles != null) SetParticleRate(minGlitchRate);
        if (qm != null) qm.OnProgressChanged += OnProgress;
    }

    void OnDestroy()
    {
        if (qm != null) qm.OnProgressChanged -= OnProgress;
    }

    void OnProgress(int scanned, int total)
    {
        float t = total > 0 ? (float)scanned / total : 0f;
        targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        if (glitchParticles != null)
            SetParticleRate(Mathf.Lerp(minGlitchRate, maxGlitchRate, t));

        // Bounce reaction
        bounceTimer = 0f;
    }

    void Update()
    {
        if (mat != null)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, transitionSpeed * Time.deltaTime);
            SetAlpha(currentAlpha);
        }

        // Bounce
        if (bounceTimer >= 0f)
        {
            bounceTimer += Time.deltaTime;
            float p = Mathf.Clamp01(bounceTimer / scanBounceDuration);
            float s = 1f + (scanBounceScale - 1f) * Mathf.Sin(p * Mathf.PI);
            transform.localScale = baseScale * s;
            if (p >= 1f) { bounceTimer = -1f; transform.localScale = baseScale; }
        }
    }

    void SetAlpha(float a)
    {
        var c = mat.color;
        c.a = a;
        mat.color = c;
    }

    void SetParticleRate(float rate)
    {
        var em = glitchParticles.emission;
        em.rateOverTime = rate;
    }
}