using System.Linq;
using UnityEngine;
using System.Collections;

public class FireSizeChanger : MonoBehaviour
{
    public Transform mainFireCore;
    public Transform stylizedBackingFlames;
    public Transform redFlames;

    /*public ParticleSystem glowPS;

    [Header("Glow – Emission (rateOverTime)")]
    public float glowRateP1 = 8f;
    public float glowRateP2 = 18f;
    public float glowRateP3 = 32f;

    [Header("Glow – Start Size")]
    public float glowSizeP1 = 0.6f;
    public float glowSizeP2 = 0.9f;
    public float glowSizeP3 = 1.2f;

    [Header("Glow – Alpha Multiplier (keeps original color)")]
    [Range(0f, 3f)] public float glowAlphaP1 = 0.6f;
    [Range(0f, 3f)] public float glowAlphaP2 = 1.0f;
    [Range(0f, 3f)] public float glowAlphaP3 = 1.4f; */

    [Header("Transition Durations (Seconds)")]
    public float transitionDurationP2 = 5.0f;  
    public float transitionDurationP3 = 5.0f;  

    [Header("Anchor Options")]
    public bool p2UseWorldUp = false;
    public bool p3UseWorldUp = false;

   // ParticleSystem.MinMaxGradient _originalGlowStartColor;
    bool _haveOriginalColor;

    Vector3 p2BaseScale = Vector3.one;
    Vector3 p3BaseScale = Vector3.one;

    Vector3 p2BaseCenterWorld;
    float p2WorldHeight;

    Vector3 p3BaseCenterWorld;
    float p3WorldHeight;

    Coroutine p2Routine;
    Coroutine p3Routine;

    void Awake()
    {
        AutoBind();
       // CacheGlowColor();

        if (stylizedBackingFlames)
        {
            p2BaseScale = stylizedBackingFlames.localScale;
            var b2 = ComputeWorldBounds(stylizedBackingFlames);
            p2BaseCenterWorld = b2.center;
            p2WorldHeight = Mathf.Max(0.001f, b2.size.y);
            stylizedBackingFlames.localScale = new Vector3(p2BaseScale.x, 0f, p2BaseScale.z);
            stylizedBackingFlames.gameObject.SetActive(true);
            EnsureEnabled(stylizedBackingFlames, false);
        }

        if (redFlames)
        {
            p3BaseScale = redFlames.localScale;
            var b3 = ComputeWorldBounds(redFlames);
            p3BaseCenterWorld = b3.center;
            p3WorldHeight = Mathf.Max(0.001f, b3.size.y);
            redFlames.localScale = new Vector3(p3BaseScale.x, 0f, p3BaseScale.z);
            redFlames.gameObject.SetActive(true);
            EnsureEnabled(redFlames, false);
        }

        SetStageByNumber(1);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            AutoBind();
           // CacheGlowColor();
        }
    }
#endif

    void AutoBind()
    {
        var all = GetComponentsInChildren<Transform>(true);

        if (!mainFireCore)
            mainFireCore = all.FirstOrDefault(t => t.name == "MainFireCore");
        if (!stylizedBackingFlames)
            stylizedBackingFlames = all.FirstOrDefault(t => t.name == "StylizedBackingFlames");
        if (!redFlames)
            redFlames = all.FirstOrDefault(t => t.name == "RedFlames");

       /* if (!glowPS && mainFireCore)
        {
            glowPS = mainFireCore.GetComponentsInChildren<ParticleSystem>(true)
                                 .FirstOrDefault(p => p.gameObject.name == "Particle System")
                  ?? mainFireCore.GetComponentInChildren<ParticleSystem>(true);
        } */
    }

    /*void CacheGlowColor()
    {
        _haveOriginalColor = false;
        if (!glowPS) return;
        _originalGlowStartColor = glowPS.main.startColor;
        _haveOriginalColor = true;
    } */

    public void SetStageByNumber(int phase)
    {
        phase = Mathf.Clamp(phase, 1, 3);

        if (mainFireCore) SetActive(mainFireCore, true);

        bool wantP2 = phase >= 2;
        bool wantP3 = phase >= 3;

        if (stylizedBackingFlames)
        {
            if (p2Routine != null) StopCoroutine(p2Routine);
            p2Routine = StartCoroutine(SmoothScaleYAnchored(
                stylizedBackingFlames,
                p2BaseScale,
                p2BaseCenterWorld,
                p2WorldHeight,
                p2UseWorldUp,
                wantP2 ? 1f : 0f,
                transitionDurationP2,
                wantP2
            ));
        }

        if (redFlames)
        {
            if (p3Routine != null) StopCoroutine(p3Routine);
            p3Routine = StartCoroutine(SmoothScaleYAnchored(
                redFlames,
                p3BaseScale,
                p3BaseCenterWorld,
                p3WorldHeight,
                p3UseWorldUp,
                wantP3 ? 1f : 0f,
                transitionDurationP3,
                wantP3
            ));
        }

        //ApplyGlow(phase);
    }

    IEnumerator SmoothScaleYAnchored(
        Transform t,
        Vector3 baseScale,
        Vector3 baseCenterWorld,
        float worldHeight,
        bool useWorldUp,
        float targetFactorY,
        float dur,
        bool enableAtEnd)
    {
        if (!t) yield break;

        t.gameObject.SetActive(true);

        float startY = Mathf.Approximately(baseScale.y, 0f) ? 0f : t.localScale.y / Mathf.Max(0.0001f, baseScale.y);
        float endY = Mathf.Clamp01(targetFactorY);

        float d = Mathf.Max(0.01f, dur);
        float acc = 0f;

        if (endY > startY + 0.0001f)
            EnsureEnabled(t, true);

        Vector3 up = useWorldUp ? Vector3.up : t.up;
        float H0 = Mathf.Max(0.001f, worldHeight);

        while (acc < d)
        {
            acc += Time.deltaTime;
            float f = Mathf.SmoothStep(0f, 1f, acc / d);
            float yFactor = Mathf.Lerp(startY, endY, f);

            t.localScale = new Vector3(baseScale.x, baseScale.y * yFactor, baseScale.z);

            Vector3 newCenter = baseCenterWorld + up * (H0 * (yFactor - 1f) * 0.5f);
            t.position = newCenter;

            yield return null;
        }

        float finalY = endY;
        t.localScale = new Vector3(baseScale.x, baseScale.y * finalY, baseScale.z);

        Vector3 finalCenter = baseCenterWorld + up * (H0 * (finalY - 1f) * 0.5f);
        t.position = finalCenter;

        bool on = finalY > 0.0001f && enableAtEnd;
        EnsureEnabled(t, on);
    }

    Bounds ComputeWorldBounds(Transform root)
    {
        var rens = root.GetComponentsInChildren<Renderer>(true);
        if (rens.Length == 0) return new Bounds(root.position, Vector3.one * 0.1f);
        Bounds b = rens[0].bounds;
        for (int i = 1; i < rens.Length; i++) b.Encapsulate(rens[i].bounds);
        return b;
    }

    void EnsureEnabled(Transform t, bool on)
    {
        if (!t) return;
        foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = on;
        foreach (var ps in t.GetComponentsInChildren<ParticleSystem>(true))
        {
            var e = ps.emission; e.enabled = on;
            if (on && !ps.isPlaying) ps.Play(true);
            else if (!on && ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void SetActive(Transform t, bool on)
    {
        if (!t) return;
        t.gameObject.SetActive(true);
        EnsureEnabled(t, on);
    }

   /* void ApplyGlow(int phase)
    {
        if (!glowPS) return;

        var main = glowPS.main;
        var emission = glowPS.emission;

        float rate, size, alphaMul;
        if (phase == 1) { rate = glowRateP1; size = glowSizeP1; alphaMul = glowAlphaP1; }
        else if (phase == 2) { rate = glowRateP2; size = glowSizeP2; alphaMul = glowAlphaP2; }
        else { rate = glowRateP3; size = glowSizeP3; alphaMul = glowAlphaP3; }

        emission.enabled = true;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
        main.startSize = new ParticleSystem.MinMaxCurve(size);

        if (_haveOriginalColor)
            main.startColor = MultiplyAlpha(_originalGlowStartColor, alphaMul);

        if (!glowPS.gameObject.activeSelf) glowPS.gameObject.SetActive(true);
        if (!glowPS.isPlaying) glowPS.Play(true);
    } */

    static ParticleSystem.MinMaxGradient MultiplyAlpha(ParticleSystem.MinMaxGradient src, float mul)
    {
        switch (src.mode)
        {
            case ParticleSystemGradientMode.Color:
                var c = src.color; c.a = Mathf.Clamp01(c.a * mul);
                return new ParticleSystem.MinMaxGradient(c);
            case ParticleSystemGradientMode.TwoColors:
                var cMin = src.colorMin; cMin.a = Mathf.Clamp01(cMin.a * mul);
                var cMax = src.colorMax; cMax.a = Mathf.Clamp01(cMax.a * mul);
                return new ParticleSystem.MinMaxGradient(cMin, cMax);
            case ParticleSystemGradientMode.Gradient:
                var g = Clone(src.gradient); ScaleAlpha(ref g, mul);
                return new ParticleSystem.MinMaxGradient(g);
            case ParticleSystemGradientMode.TwoGradients:
                var gMin = Clone(src.gradientMin); ScaleAlpha(ref gMin, mul);
                var gMax = Clone(src.gradientMax); ScaleAlpha(ref gMax, mul);
                return new ParticleSystem.MinMaxGradient(gMin, gMax);
            default:
                return src;
        }
    }

    static Gradient Clone(Gradient src)
    {
        if (src == null) return new Gradient();
        return new Gradient { colorKeys = src.colorKeys, alphaKeys = src.alphaKeys, mode = src.mode };
    }

    static void ScaleAlpha(ref Gradient g, float mul)
    {
        if (g == null) return;
        var a = g.alphaKeys;
        for (int i = 0; i < a.Length; i++) a[i].alpha = Mathf.Clamp01(a[i].alpha * mul);
        g.SetKeys(g.colorKeys, a);
    }
}
