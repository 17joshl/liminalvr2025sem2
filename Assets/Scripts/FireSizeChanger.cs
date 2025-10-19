using System.Linq;
using UnityEngine;

public class FireSizeChanger : MonoBehaviour
{
    
    public Transform mainFireCore;             // Phase 1
    public Transform stylizedBackingFlames;    // Phase 2
    public Transform redFlames;                // Phase 3

    // Glow particle 
    public ParticleSystem glowPS;

    // Glow settings per phase
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
    [Range(0f, 3f)] public float glowAlphaP3 = 1.4f;

    
    ParticleSystem.MinMaxGradient _originalGlowStartColor;
    bool _haveOriginalColor;

    void Awake()
    {
        AutoBind();
        CacheGlowColor();
        SetStageByNumber(1);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            AutoBind();
            CacheGlowColor();
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

        if (!glowPS && mainFireCore)
        {
            
            glowPS = mainFireCore.GetComponentsInChildren<ParticleSystem>(true)
                                 .FirstOrDefault(p => p.gameObject.name == "Particle System")
                     ?? mainFireCore.GetComponentInChildren<ParticleSystem>(true);
        }
    }

    void CacheGlowColor()
    {
        _haveOriginalColor = false;
        if (!glowPS) return;
        _originalGlowStartColor = glowPS.main.startColor;
        _haveOriginalColor = true;
    }

    
    public void SetStageByNumber(int phase)
    {
        phase = Mathf.Clamp(phase, 1, 3);

        SetActive(mainFireCore, false);
        SetActive(stylizedBackingFlames, false);
        SetActive(redFlames, false);

        if (phase >= 1) SetActive(mainFireCore, true);
        if (phase >= 2) SetActive(stylizedBackingFlames, true);
        if (phase >= 3) SetActive(redFlames, true);

        ApplyGlow(phase);
    }

    

    void SetActive(Transform t, bool on)
    {
        if (!t) return;
        t.gameObject.SetActive(on);

        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            r.enabled = on;

        foreach (var ps in t.GetComponentsInChildren<ParticleSystem>(true))
        {
            var e = ps.emission; e.enabled = on;
            if (on && !ps.isPlaying) ps.Play(true);
            else if (!on && ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void ApplyGlow(int phase)
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
    }

   
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
                var two = new ParticleSystem.MinMaxGradient(cMin, cMax) { mode = ParticleSystemGradientMode.TwoColors };
                return two;

            case ParticleSystemGradientMode.Gradient:
                var g = Clone(src.gradient); ScaleAlpha(ref g, mul);
                return new ParticleSystem.MinMaxGradient(g);

            case ParticleSystemGradientMode.TwoGradients:
                var gMin = Clone(src.gradientMin); ScaleAlpha(ref gMin, mul);
                var gMax = Clone(src.gradientMax); ScaleAlpha(ref gMax, mul);
                var tg = new ParticleSystem.MinMaxGradient(gMin, gMax) { mode = ParticleSystemGradientMode.TwoGradients };
                return tg;

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
