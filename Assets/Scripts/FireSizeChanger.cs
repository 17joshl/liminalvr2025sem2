using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireSizeChanger : MonoBehaviour
{
    public Transform supportingBonfire;
    public Transform mainFire;
    public Transform redFlames;
    public ParticleSystem glowPS;

    public FireAudioController audioCtrl; 

    private const float transitionDurationP2 = 5f;
    private const float transitionDurationP3 = 5f;

    public bool p2UseWorldUp = false;
    public bool p3UseWorldUp = false;

    public float glowRateP1 = 8f, glowRateP2 = 18f, glowRateP3 = 32f;
    public float glowSizeP1 = 0.6f, glowSizeP2 = 0.9f, glowSizeP3 = 1.2f;
    [Range(0f, 3f)] public float glowAlphaP1 = 0.6f, glowAlphaP2 = 1.0f, glowAlphaP3 = 1.4f;

    Vector3 p2BaseScale = Vector3.one;
    Vector3 p2BaseCenterWorld;
    float p2WorldHeight;

    Vector3 p3BaseScale = Vector3.one;
    Vector3 p3BaseCenterWorld;
    float p3WorldHeight;

    Vector3 mainBaseScale = Vector3.one;
    Vector3 mainBaseCenterWorld;
    float mainWorldHeight;

    Coroutine mainRoutine, p3Routine;

    ParticleSystem.MinMaxGradient glowOrig;
    bool haveGlowOrig;

    class PSOrig
    {
        public ParticleSystem ps;
        public bool hadEmission;
        public float rateMultOrig;
        public float sizeMultOrig;
    }

    List<PSOrig> mainPsList;
    int currentPhase = 1;

    void Awake()
    {
        AutoBind();

        if (supportingBonfire)
        {
            p2BaseScale = supportingBonfire.localScale;
            var b2 = WorldBounds(supportingBonfire);
            p2BaseCenterWorld = b2.center;
            p2WorldHeight = Mathf.Max(0.001f, b2.size.y);
            EnsureHierarchyActive(supportingBonfire, true);
            EnsureEnabled(supportingBonfire, true);
            SnapToFullAtBase(supportingBonfire, p2BaseScale, p2BaseCenterWorld, p2WorldHeight, p2UseWorldUp);
        }

        PrepareMainFire();
        PrepareRedFlames();

        CacheGlowColor();
        currentPhase = 1;
        ApplyGlow(currentPhase);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying) AutoBind();
    }
#endif

    void AutoBind()
    {
        var all = GetComponentsInChildren<Transform>(true);

        if (!supportingBonfire)
            supportingBonfire = all.FirstOrDefault(t => t.name == "SupportingBonfire");

        if (!mainFire)
        {
            mainFire =
                all.FirstOrDefault(t => t.name == "Main_Fire") ??
                all.FirstOrDefault(t => t.name == "main_flame") ??
                all.FirstOrDefault(t => t.name == "MainFlame") ??
                all.FirstOrDefault(t => t.name == "MainFire") ??
                all.FirstOrDefault(t =>
                    t.name.ToLowerInvariant().Contains("main") &&
                    t.name.ToLowerInvariant().Contains("fire"));
        }

        if (!redFlames)
            redFlames = all.FirstOrDefault(t => t.name.StartsWith("RedFlames"));

        if (!glowPS && mainFire)
        {
            glowPS = mainFire.GetComponentsInChildren<ParticleSystem>(true)
                             .FirstOrDefault(p => p.gameObject.name == "Particle System")
                     ?? mainFire.GetComponentInChildren<ParticleSystem>(true);
        }

        if (!audioCtrl) audioCtrl = FindObjectOfType<FireAudioController>();
    }

    void PrepareMainFire()
    {
        if (!mainFire) return;

        EnsureHierarchyActive(mainFire, true);

        mainBaseScale = mainFire.localScale;
        var bm = WorldBounds(mainFire);
        mainBaseCenterWorld = bm.center;
        mainWorldHeight = Mathf.Max(0.001f, bm.size.y);

        var pss = mainFire.GetComponentsInChildren<ParticleSystem>(true);
        mainPsList = new List<PSOrig>();
        foreach (var ps in pss)
        {
            var e = ps.emission;
            var m = ps.main;

            mainPsList.Add(new PSOrig
            {
                ps = ps,
                hadEmission = e.enabled,
                rateMultOrig = e.rateOverTimeMultiplier,
                sizeMultOrig = m.startSizeMultiplier
            });

            e.enabled = true;
            e.rateOverTimeMultiplier = 0f;
            m.startSizeMultiplier = 0f;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (mainPsList.Count == 0)
        {
            mainFire.localScale = new Vector3(mainBaseScale.x, 0f, mainBaseScale.z);
            EnsureEnabled(mainFire, false);
        }
        else
        {
            mainFire.localScale = mainBaseScale;
            EnsureEnabled(mainFire, true);
        }
    }

    void PrepareRedFlames()
    {
        if (!redFlames) return;

        EnsureHierarchyActive(redFlames, true);

        p3BaseScale = redFlames.localScale;
        var b3 = WorldBounds(redFlames);
        p3BaseCenterWorld = b3.center;
        p3WorldHeight = Mathf.Max(0.001f, b3.size.y);

        var pss = redFlames.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in pss)
        {
            if (!ps.isPlaying) ps.Play(true);
            var e = ps.emission; e.enabled = true;
        }

        redFlames.localScale = new Vector3(p3BaseScale.x, 0f, p3BaseScale.z);
        EnsureEnabled(redFlames, true);
    }

    public void SetStageByNumber(int phase)
    {
        Debug.Log($"[FireSizeChanger] {name} request phase={phase} (prev={currentPhase})");

        phase = Mathf.Clamp(phase, 1, 3);

        if (mainRoutine != null) StopCoroutine(mainRoutine);
        if (p3Routine != null) StopCoroutine(p3Routine);

        if (supportingBonfire)
        {
            EnsureHierarchyActive(supportingBonfire, true);
            EnsureEnabled(supportingBonfire, true);
            SnapToFullAtBase(supportingBonfire, p2BaseScale, p2BaseCenterWorld, p2WorldHeight, p2UseWorldUp);
        }

        int from = currentPhase;
        int to = phase;

        if (mainFire)
        {
            if (from < 2 && to >= 2)
            {
                if (mainPsList.Count > 0)
                    mainRoutine = StartCoroutine(FadeInParticleSystems(mainPsList, transitionDurationP2));
                else
                    mainRoutine = StartCoroutine(ScaleYFromBase(mainFire, mainBaseScale, mainBaseCenterWorld, mainWorldHeight, p2UseWorldUp, 1f, transitionDurationP2, true));
            }
            else if (from >= 2 && to < 2)
            {
                if (mainPsList.Count > 0)
                    mainRoutine = StartCoroutine(FadeOutParticleSystems(mainPsList, transitionDurationP2, true));
                else
                    mainRoutine = StartCoroutine(ScaleYFromBase(mainFire, mainBaseScale, mainBaseCenterWorld, mainWorldHeight, p2UseWorldUp, 0f, transitionDurationP2, false));
            }
        }

        if (redFlames)
        {
            if (from < 3 && to >= 3)
                p3Routine = StartCoroutine(ScaleYFromBaseRed(redFlames, p3BaseScale, p3BaseCenterWorld, p3WorldHeight, p3UseWorldUp, 1f, transitionDurationP3));
            else if (from >= 3 && to < 3)
                p3Routine = StartCoroutine(ScaleYFromBaseRed(redFlames, p3BaseScale, p3BaseCenterWorld, p3WorldHeight, p3UseWorldUp, 0f, transitionDurationP3));
        }

        currentPhase = to;
        ApplyGlow(currentPhase);

        if (!audioCtrl) audioCtrl = FindObjectOfType<FireAudioController>();
        if (audioCtrl) audioCtrl.SetPhase(currentPhase);

        Debug.Log($"[FireSizeChanger] {name} applied -> phase={currentPhase}, audioCtrl={(audioCtrl ? audioCtrl.name : "NULL")}");
    }

    IEnumerator FadeInParticleSystems(List<PSOrig> list, float dur)
    {
        float d = Mathf.Max(0.01f, dur), acc = 0f;

        foreach (var o in list)
        {
            EnsureHierarchyActive(o.ps.transform, true);
            EnsureEnabled(o.ps.transform, true);
            var e = o.ps.emission; e.enabled = true; e.rateOverTimeMultiplier = 0f;
            var m = o.ps.main; m.startSizeMultiplier = 0f;
            if (!o.ps.isPlaying) o.ps.Play(true);
        }

        while (acc < d)
        {
            acc += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, acc / d);

            foreach (var o in list)
            {
                var e = o.ps.emission; e.rateOverTimeMultiplier = Mathf.Lerp(0f, o.rateMultOrig == 0f ? 1f : o.rateMultOrig, t);
                var m = o.ps.main; m.startSizeMultiplier = Mathf.Lerp(0f, o.sizeMultOrig == 0f ? 1f : o.sizeMultOrig, t);
            }

            yield return null;
        }

        foreach (var o in list)
        {
            var e = o.ps.emission; e.enabled = o.hadEmission; e.rateOverTimeMultiplier = o.rateMultOrig == 0f ? 1f : o.rateMultOrig;
            var m = o.ps.main; m.startSizeMultiplier = o.sizeMultOrig == 0f ? 1f : o.sizeMultOrig;
            if (!o.ps.isPlaying) o.ps.Play(true);
        }
    }

    IEnumerator FadeOutParticleSystems(List<PSOrig> list, float dur, bool disableAtEnd)
    {
        float d = Mathf.Max(0.01f, dur), acc = 0f;

        foreach (var o in list)
        {
            EnsureHierarchyActive(o.ps.transform, true);
            EnsureEnabled(o.ps.transform, true);
            var e = o.ps.emission; e.enabled = true;
            if (!o.ps.isPlaying) o.ps.Play(true);
        }

        while (acc < d)
        {
            acc += Time.deltaTime;
            float t = Mathf.SmoothStep(1f, 0f, acc / d);

            foreach (var o in list)
            {
                var e = o.ps.emission; e.rateOverTimeMultiplier = Mathf.Lerp(o.rateMultOrig == 0f ? 1f : o.rateMultOrig, 0f, 1f - t);
                var m = o.ps.main; m.startSizeMultiplier = Mathf.Lerp(o.sizeMultOrig == 0f ? 1f : o.sizeMultOrig, 0f, 1f - t);
            }

            yield return null;
        }

        foreach (var o in list)
        {
            var e = o.ps.emission; e.enabled = true; e.rateOverTimeMultiplier = 0f;
            var m = o.ps.main; m.startSizeMultiplier = 0f;
            o.ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (disableAtEnd) EnsureEnabled(o.ps.transform, false);
        }
    }

    IEnumerator ScaleYFromBaseRed(Transform t, Vector3 baseScale, Vector3 baseCenterWorld, float worldHeight, bool useWorldUp, float targetFactorY, float dur)
    {
        if (!t) yield break;

        float startY = Mathf.Approximately(baseScale.y, 0f) ? 0f : t.localScale.y / Mathf.Max(0.0001f, baseScale.y);
        float endY = Mathf.Clamp01(targetFactorY);
        float d = Mathf.Max(0.01f, dur), acc = 0f;

        EnsureHierarchyActive(t, true);
        EnsureEnabled(t, true);

        Vector3 up = useWorldUp ? Vector3.up : t.up;
        float H0 = Mathf.Max(0.001f, worldHeight);

        while (acc < d)
        {
            acc += Time.deltaTime;
            float s = acc / d;
            float y = Mathf.Lerp(startY, endY, s);

            t.localScale = new Vector3(baseScale.x, baseScale.y * y, baseScale.z);
            t.position = baseCenterWorld + up * (H0 * (y - 1f) * 0.5f);

            yield return null;
        }

        float fy = endY;
        t.localScale = new Vector3(baseScale.x, baseScale.y * fy, baseScale.z);
        t.position = baseCenterWorld + (useWorldUp ? Vector3.up : t.up) * (H0 * (fy - 1f) * 0.5f);

        if (fy <= 0.0001f) EnsureEnabled(t, false);
    }

    IEnumerator ScaleYFromBase(Transform t, Vector3 baseScale, Vector3 baseCenterWorld, float worldHeight, bool useWorldUp, float targetFactorY, float dur, bool enableAtEnd)
    {
        if (!t) yield break;

        float startY = Mathf.Approximately(baseScale.y, 0f) ? 0f : t.localScale.y / Mathf.Max(0.0001f, baseScale.y);
        float endY = Mathf.Clamp01(targetFactorY);
        float d = Mathf.Max(0.01f, dur), acc = 0f;

        EnsureHierarchyActive(t, true);
        EnsureEnabled(t, true);

        Vector3 up = useWorldUp ? Vector3.up : t.up;
        float H0 = Mathf.Max(0.001f, worldHeight);

        while (acc < d)
        {
            acc += Time.deltaTime;
            float f = Mathf.SmoothStep(0f, 1f, acc / d);
            float yFactor = Mathf.Lerp(startY, endY, f);

            t.localScale = new Vector3(baseScale.x, baseScale.y * yFactor, baseScale.z);
            t.position = baseCenterWorld + up * (H0 * (yFactor - 1f) * 0.5f);

            yield return null;
        }

        float finalY = endY;
        t.localScale = new Vector3(baseScale.x, baseScale.y * finalY, baseScale.z);
        t.position = baseCenterWorld + (useWorldUp ? Vector3.up : t.up) * (H0 * (finalY - 1f) * 0.5f);

        bool on = finalY > 0.0001f && enableAtEnd;
        EnsureEnabled(t, on);
        if (!on) EnsureEnabled(t, false);
    }

    Bounds WorldBounds(Transform root)
    {
        var r = root ? root.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        if (r.Length == 0) return new Bounds(root ? root.position : Vector3.zero, Vector3.one * 0.1f);
        Bounds b = r[0].bounds;
        for (int i = 1; i < r.Length; i++) b.Encapsulate(r[i].bounds);
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

    void EnsureHierarchyActive(Transform t, bool on)
    {
        if (!t) return;

        if (on)
        {
            var p = t.parent;
            while (p)
            {
                if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
                p = p.parent;
            }
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
        }
        else
        {
            if (t.gameObject.activeSelf) t.gameObject.SetActive(false);
        }
    }

    void SnapToFullAtBase(Transform t, Vector3 baseScale, Vector3 baseCenterWorld, float worldHeight, bool useWorldUp)
    {
        if (!t) return;
        t.localScale = baseScale;
        Vector3 up = useWorldUp ? Vector3.up : t.up;
        float H0 = Mathf.Max(0.001f, worldHeight);
        t.position = baseCenterWorld + up * (H0 * (1f - 1f) * 0.5f);
    }

    void CacheGlowColor()
    {
        if (!glowPS) { haveGlowOrig = false; return; }
        glowOrig = glowPS.main.startColor;
        haveGlowOrig = true;
    }

    void ApplyGlow(int phase)
    {
        if (!glowPS) return;

        var m = glowPS.main;
        var e = glowPS.emission;

        float rate, size, aMul;
        if (phase == 1) { rate = glowRateP1; size = glowSizeP1; aMul = glowAlphaP1; }
        else if (phase == 2) { rate = glowRateP2; size = glowSizeP2; aMul = glowAlphaP2; }
        else { rate = glowRateP3; size = glowSizeP3; aMul = glowAlphaP3; }

        e.enabled = true;
        e.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
        m.startSize = new ParticleSystem.MinMaxCurve(size);
        if (haveGlowOrig) m.startColor = MultiplyAlpha(glowOrig, aMul);

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
        for (int i = 0; i < a.Length; i++)
        {
            a[i].alpha = Mathf.Clamp01(a[i].alpha * mul);
        }
        g.SetKeys(g.colorKeys, a);
    }
}
