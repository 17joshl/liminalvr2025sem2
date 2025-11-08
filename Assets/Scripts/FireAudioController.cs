using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct WindVolumeProfile
{
    [Range(0f, 1f)] public float phase1;
    [Range(0f, 1f)] public float phase2;
    [Range(0f, 1f)] public float phase3;
}

public class FireAudioController : MonoBehaviour
{
    [Header("Auto-discover root (optional)")]
    public Transform soundRoot;

    [Header("Always On")]
    public AudioSource wind;
    public AudioSource crickets;

    [Header("Phase 1")]
    public AudioSource lowLevelCrackles;
    public AudioSource lowLevelNearEndSynth;

    [Header("Phase 2")]
    public AudioSource midLevelCrackle;
    public AudioSource midLevelWhoosh;
    public AudioSource midLevelSynth;
    public AudioSource midLevelPulseSynth;

    [Header("Phase 3")]
    public AudioSource largeFlameCrackle;
    public AudioSource largeLevelFlameWhoosh;
    public AudioSource largeFlameBuild;
    public AudioSource largeFlameSynth;

    [Header("Timings / Levels")]
    [SerializeField] float xfade = 1.25f;
    [SerializeField] float quickFade = 0.35f;
    [SerializeField] float nearEndDelay = 15f;
    [Range(0f, 1f)] public float master = 1f;

    [Header("Wind Volume Profile")]
    public WindVolumeProfile windProfile = new WindVolumeProfile { phase1 = 1f, phase2 = 0.66f, phase3 = 0.50f };

    [Header("Startup")]
    [SerializeField, Range(0, 3)] int initialPhase = 1;

    readonly List<Coroutine> running = new List<Coroutine>();
    Coroutine p1NearEndRoutine;
    Coroutine transitionRoutine;
    int currentPhase = 0;
    bool gazeOn = true;
    bool isTransitioning = false;

    public void SetWindProfile(float p1, float p2, float p3, bool applyNow = false)
    {
        windProfile.phase1 = Mathf.Clamp01(p1);
        windProfile.phase2 = Mathf.Clamp01(p2);
        windProfile.phase3 = Mathf.Clamp01(p3);
        if (applyNow) ApplyPhaseImmediate(currentPhase == 0 ? 0 : currentPhase);
    }

    void Awake()
    {
        TryResolveSoundRoot();
        AutoDiscoverIfMissing();
        PrepareSources();
        HardSilenceAllNonAmbience();
        ApplyPhaseImmediate(0);
    }

    void Start()
    {
        if (initialPhase >= 1 && initialPhase <= 3)
            SetPhase(initialPhase);
    }

    void OnEnable()
    {
        StartCoroutine(DelayedRebind());
    }

    IEnumerator DelayedRebind()
    {
        yield return null;
        TryResolveSoundRoot();
        AutoDiscoverIfMissing();
        PrepareSources();
        HardSilenceAllNonAmbience();
        ApplyPhaseImmediate(currentPhase == 0 ? 0 : currentPhase);
        ApplyGazeGate(true);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        running.Clear();
        p1NearEndRoutine = null;
        transitionRoutine = null;
        isTransitioning = false;
    }

    public void SetPhase(int phase)
    {
        phase = Mathf.Clamp(phase, 1, 3);
        if (phase == currentPhase) return;

        StopRunning();

        float windTarget = phase == 1 ? windProfile.phase1 : (phase == 2 ? windProfile.phase2 : windProfile.phase3);
        float cricketsTarget = phase == 1 ? 1f : (phase == 2 ? 0.50f : 0.00f);

        FadeTo(wind, windTarget * master, xfade, loop: true);
        FadeTo(crickets, cricketsTarget * master, xfade, loop: true);

        currentPhase = phase;

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionToPhase(phase));

        Debug.Log("[Audio] SetPhase(" + phase + ")");
    }

    IEnumerator TransitionToPhase(int phase)
    {
        isTransitioning = true;

        if (phase == 1)
        {
            FadeTo(lowLevelCrackles, 1f * master, xfade, loop: true);
            FadeTo(midLevelCrackle, 0f, xfade);
            FadeTo(midLevelSynth, 0f, xfade);
            FadeTo(midLevelPulseSynth, 0f, xfade);
            FadeTo(largeFlameCrackle, 0f, xfade);
            FadeTo(largeFlameBuild, 0f, xfade);
            FadeTo(largeFlameSynth, 0f, xfade);
            if (p1NearEndRoutine != null) StopCoroutine(p1NearEndRoutine);
            p1NearEndRoutine = StartCoroutine(Phase1NearEndSynthRoutine());
        }
        else if (phase == 2)
        {
            if (p1NearEndRoutine != null) { StopCoroutine(p1NearEndRoutine); p1NearEndRoutine = null; }
            FadeTo(lowLevelNearEndSynth, 0f, xfade);
            FadeTo(lowLevelCrackles, 0f, xfade);
            FadeTo(midLevelCrackle, 1f * master, xfade, loop: true);
            FadeTo(midLevelSynth, 1f * master, xfade, loop: true);
            FadeTo(midLevelPulseSynth, 1f * master, xfade, loop: true);
            FadeTo(largeFlameCrackle, 0f, xfade);
            FadeTo(largeFlameBuild, 0f, xfade);
            FadeTo(largeFlameSynth, 0f, xfade);
            OneShot(midLevelWhoosh);
        }
        else
        {
            FadeTo(lowLevelCrackles, 0f, xfade);
            FadeTo(midLevelCrackle, 0f, xfade);
            FadeTo(midLevelSynth, 0f, xfade);
            FadeTo(midLevelPulseSynth, 0f, xfade);
            FadeTo(lowLevelNearEndSynth, 0f, xfade);

            FadeTo(largeFlameCrackle, 1f * master, xfade, loop: true);
            FadeTo(largeFlameBuild, 1f * master, xfade, loop: true);
            FadeTo(largeFlameSynth, 1f * master, xfade, loop: true);

            OneShot(largeLevelFlameWhoosh);
        }

        float t = 0f;
        while (t < xfade)
        {
            t += Time.deltaTime;
            yield return null;
        }

        isTransitioning = false;
        ApplyGazeGate(false);
        transitionRoutine = null;
    }

    public void SetGaze(bool isOn)
    {
        gazeOn = isOn;
        ApplyGazeGate();
    }

    void ApplyPhaseImmediate(int phase)
    {
        currentPhase = phase;

        EnsureLoop(wind, true); EnsureLoop(crickets, true);
        EnsureLoop(lowLevelCrackles, true);
        EnsureLoop(lowLevelNearEndSynth, true);
        EnsureLoop(midLevelCrackle, true);
        EnsureLoop(midLevelSynth, true);
        EnsureLoop(midLevelPulseSynth, true);
        EnsureLoop(largeFlameCrackle, true);
        EnsureLoop(largeFlameBuild, true);
        EnsureLoop(largeFlameSynth, true);
        EnsureLoop(midLevelWhoosh, false);
        EnsureLoop(largeLevelFlameWhoosh, false);

        if (phase == 0)
        {
            if (wind) { wind.volume = 1f * master; SafePlay(wind); }
            if (crickets) { crickets.volume = 1f * master; SafePlay(crickets); }

            MuteStop(lowLevelCrackles);
            MuteStop(lowLevelNearEndSynth);
            MuteStop(midLevelCrackle);
            MuteStop(midLevelSynth);
            MuteStop(midLevelPulseSynth);
            MuteStop(largeFlameCrackle);
            MuteStop(largeFlameBuild);
            MuteStop(largeFlameSynth);
            return;
        }

        if (wind)
        {
            float w = (phase == 1 ? windProfile.phase1 : phase == 2 ? windProfile.phase2 : windProfile.phase3);
            wind.volume = w * master; SafePlay(wind);
        }
        if (crickets)
        {
            float c = (phase == 1 ? 1f : phase == 2 ? 0.50f : 0.00f);
            crickets.volume = c * master; SafePlay(crickets);
        }

        if (phase == 1)
        {
            if (lowLevelCrackles) { lowLevelCrackles.volume = 1f * master; SafePlay(lowLevelCrackles); }
            if (lowLevelNearEndSynth) lowLevelNearEndSynth.volume = 0f;
            MuteStop(midLevelCrackle);
            MuteStop(midLevelSynth);
            MuteStop(midLevelPulseSynth);
            MuteStop(largeFlameCrackle);
            MuteStop(largeFlameBuild);
            MuteStop(largeFlameSynth);
            if (p1NearEndRoutine != null) StopCoroutine(p1NearEndRoutine);
            p1NearEndRoutine = StartCoroutine(Phase1NearEndSynthRoutine());
        }
        else if (phase == 2)
        {
            if (midLevelCrackle) { midLevelCrackle.volume = 1f * master; SafePlay(midLevelCrackle); }
            if (midLevelSynth) { midLevelSynth.volume = 1f * master; SafePlay(midLevelSynth); }
            if (midLevelPulseSynth) { midLevelPulseSynth.volume = 1f * master; SafePlay(midLevelPulseSynth); }
            MuteStop(lowLevelCrackles);
            MuteStop(lowLevelNearEndSynth);
            MuteStop(largeFlameCrackle);
            MuteStop(largeFlameBuild);
            MuteStop(largeFlameSynth);
        }
        else
        {
            if (largeFlameCrackle) { largeFlameCrackle.volume = 1f * master; SafePlay(largeFlameCrackle); }
            if (largeFlameBuild) { largeFlameBuild.volume = 1f * master; SafePlay(largeFlameBuild); }
            if (largeFlameSynth) { largeFlameSynth.volume = 1f * master; SafePlay(largeFlameSynth); }
            MuteStop(lowLevelCrackles);
            MuteStop(lowLevelNearEndSynth);
            MuteStop(midLevelCrackle);
            MuteStop(midLevelSynth);
            MuteStop(midLevelPulseSynth);
        }

        ApplyGazeGate(true);
    }

    void ApplyGazeGate(bool immediate = false)
    {
        if (isTransitioning) return;

        float t = immediate ? 0f : quickFade;
        bool p2Active = currentPhase == 2;
        bool p3Active = currentPhase == 3;

        if (p2Active)
        {
            FadeTo(midLevelSynth, gazeOn ? 1f * master : 0f, t, loop: true);
            FadeTo(midLevelPulseSynth, gazeOn ? 1f * master : 0f, t, loop: true);
        }

        if (p3Active)
        {
            FadeTo(largeFlameBuild, gazeOn ? 1f * master : 0f, t, loop: true);
            FadeTo(largeFlameSynth, gazeOn ? 1f * master : 0f, t, loop: true);
        }
    }

    void StopRunning()
    {
        for (int i = 0; i < running.Count; i++)
        {
            if (running[i] != null) StopCoroutine(running[i]);
        }
        running.Clear();
    }

    void FadeTo(AudioSource s, float vol, float dur, bool loop = false)
    {
        if (!s) return;
        EnsureLoop(s, loop);
        if (!s.isPlaying && vol > 0f) s.Play();
        running.Add(StartCoroutine(FadeCR(s, vol, dur)));
    }

    IEnumerator FadeCR(AudioSource s, float vol, float dur)
    {
        float t = 0f, v0 = s ? s.volume : 0f;
        dur = Mathf.Max(0f, dur);
        while (t < dur && s)
        {
            t += Time.deltaTime;
            float k = dur <= 0f ? 1f : Mathf.Clamp01(t / dur);
            s.volume = Mathf.Lerp(v0, vol, k);
            yield return null;
        }
        if (s)
        {
            s.volume = vol;
            if (Mathf.Approximately(vol, 0f)) s.Stop();
        }
    }

    void OneShot(AudioSource s)
    {
        if (!s) return;
        s.loop = false;
        if (s.clip) s.PlayOneShot(s.clip, master);
        else { s.volume = master; s.Play(); }
    }

    void EnsureLoop(AudioSource s, bool loop)
    {
        if (!s) return;
        s.loop = loop;
        s.playOnAwake = false;
    }

    void SafePlay(AudioSource s)
    {
        if (!s) return;
        if (!s.isPlaying) s.Play();
    }

    void MuteStop(AudioSource s)
    {
        if (!s) return;
        s.volume = 0f;
        if (s.isPlaying) s.Stop();
    }

    IEnumerator Phase1NearEndSynthRoutine()
    {
        if (!lowLevelNearEndSynth) yield break;
        lowLevelNearEndSynth.volume = 0f;
        float t = 0f;
        while (t < nearEndDelay && currentPhase == 1)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (currentPhase == 1)
            FadeTo(lowLevelNearEndSynth, 1f * master, xfade, loop: true);
    }

    [ContextMenu("Rebind Audio Now")]
    public void RebindAudioNow()
    {
        TryResolveSoundRoot();
        AutoDiscoverIfMissing();
        PrepareSources();
        HardSilenceAllNonAmbience();
        ApplyPhaseImmediate(currentPhase);
        ApplyGazeGate(true);
        Debug.Log("[Audio] Rebind complete.");
    }

    void TryResolveSoundRoot()
    {
        if (soundRoot && soundRoot.gameObject.scene.IsValid()) return;
        foreach (var t in GetAllSceneTransforms(true))
        {
            if (t.name == "NewSoundScape") { soundRoot = t; return; }
        }
        soundRoot = null;
    }

    void AutoDiscoverIfMissing()
    {
        if (soundRoot != null)
        {
            TryBindFromPath(soundRoot, "OnAtAllTimes/Wind", ref wind);
            TryBindFromPath(soundRoot, "OnAtAllTimes/Crickets", ref crickets);
            TryBindFromPath(soundRoot, "Phase1Fire/LowLevelCrackles", ref lowLevelCrackles);
            TryBindFromPath(soundRoot, "Phase1Fire/LowLevelNearEndSynth", ref lowLevelNearEndSynth);
            TryBindFromPath(soundRoot, "Phase2Fire/MidLevelCrackle", ref midLevelCrackle);
            TryBindFromPath(soundRoot, "Phase2Fire/MidLevelWhoosh", ref midLevelWhoosh);
            TryBindFromPath(soundRoot, "Phase2Fire/MidLevelSynth", ref midLevelSynth);
            TryBindFromPath(soundRoot, "Phase2Fire/MidLevelPulseSynth", ref midLevelPulseSynth);
            TryBindFromPath(soundRoot, "Phase3Fire/LargeFlameCrackle", ref largeFlameCrackle);
            TryBindFromPath(soundRoot, "Phase3Fire/LargeLevelFlameWhoosh", ref largeLevelFlameWhoosh);
            TryBindFromPath(soundRoot, "Phase3Fire/LargeFlameBuild", ref largeFlameBuild);
            TryBindFromPath(soundRoot, "Phase3Fire/LargeFlameSynth", ref largeFlameSynth);
        }

        var all = GetAllSceneAudio(true);
        wind = wind ? wind : FindByAny(all, "wind");
        crickets = crickets ? crickets : FindByAny(all, "cricket");
        lowLevelCrackles = lowLevelCrackles ? lowLevelCrackles : FindByAny(all, "lowlevelcrackles", "low level crackle", "p1");
        lowLevelNearEndSynth = lowLevelNearEndSynth ? lowLevelNearEndSynth : FindByAny(all, "lowlevelnearendsynth", "nearend", "near end", "p1 synth");
        midLevelCrackle = midLevelCrackle ? midLevelCrackle : FindByAny(all, "midlevelcrackle", "mid crackle", "p2");
        midLevelWhoosh = midLevelWhoosh ? midLevelWhoosh : FindByAny(all, "p2 whoosh", "whoosh");
        midLevelSynth = midLevelSynth ? midLevelSynth : FindByAny(all, "midlevelsynth", "mid synth", "p2 synth");
        midLevelPulseSynth = midLevelPulseSynth ? midLevelPulseSynth : FindByAny(all, "midlevelpulsesynth", "pulse", "mid pulse");
        largeFlameCrackle = largeFlameCrackle ? largeFlameCrackle : FindByAny(all, "largeflamecrackle", "large crackle", "p3");
        largeLevelFlameWhoosh = largeLevelFlameWhoosh ? largeLevelFlameWhoosh : FindByAny(all, "large flame whoosh", "p3 whoosh", "flame whoosh", "sfx");
        largeFlameBuild = largeFlameBuild ? largeFlameBuild : FindByAny(all, "largeflamebuild", "large build", "build");
        largeFlameSynth = largeFlameSynth ? largeFlameSynth : FindByAny(all, "largeflamesynth", "large synth", "p3 synth");

        Debug.Log("[Audio AutoFind] wind=" + NameOrDash(wind) + " crickets=" + NameOrDash(crickets) + " | " +
                  "P1 crackle=" + NameOrDash(lowLevelCrackles) + " nearEnd=" + NameOrDash(lowLevelNearEndSynth) + " | " +
                  "P2 crackle=" + NameOrDash(midLevelCrackle) + " whoosh=" + NameOrDash(midLevelWhoosh) + " synth=" + NameOrDash(midLevelSynth) + " pulse=" + NameOrDash(midLevelPulseSynth) + " | " +
                  "P3 crackle=" + NameOrDash(largeFlameCrackle) + " whoosh=" + NameOrDash(largeLevelFlameWhoosh) + " build=" + NameOrDash(largeFlameBuild) + " synth=" + NameOrDash(largeFlameSynth));
    }

    static string NameOrDash(Object o) => o ? o.name : "--";

    void TryBindFromPath(Transform root, string path, ref AudioSource slot)
    {
        if (slot || root == null) return;
        var t = root.Find(path);
        if (!t)
        {
            var segs = path.Split('/');
            Transform cur = root;
            for (int i = 0; i < segs.Length && cur != null; i++)
            {
                string seg = segs[i];
                Transform found = null;
                for (int c = 0; c < cur.childCount; c++)
                {
                    var ch = cur.GetChild(c);
                    if (string.Equals(ch.name, seg, System.StringComparison.OrdinalIgnoreCase)) { found = ch; break; }
                }
                cur = found;
            }
            t = cur;
        }
        if (t)
        {
            var a = t.GetComponent<AudioSource>() ?? t.GetComponentInChildren<AudioSource>(true);
            if (a && a.gameObject.scene.IsValid()) slot = a;
        }
    }

    AudioSource FindByAny(List<AudioSource> all, params string[] aliases)
    {
        if (all == null || all.Count == 0) return null;

        for (int i = 0; i < all.Count; i++)
        {
            var a = all[i]; if (!a) continue;
            string n = a.name.ToLowerInvariant();
            for (int j = 0; j < aliases.Length; j++)
            {
                var al = aliases[j]; if (string.IsNullOrEmpty(al)) continue;
                if (n.Contains(al.ToLowerInvariant())) return a;
            }
        }
        for (int i = 0; i < all.Count; i++)
        {
            var a = all[i]; if (!a || !a.clip) continue;
            string cn = a.clip.name.ToLowerInvariant();
            for (int j = 0; j < aliases.Length; j++)
            {
                var al = aliases[j]; if (string.IsNullOrEmpty(al)) continue;
                if (cn.Contains(al.ToLowerInvariant())) return a;
            }
        }
        return null;
    }

    List<AudioSource> GetAllSceneAudio(bool includeInactive)
    {
#if UNITY_2020_1_OR_NEWER
        return Object.FindObjectsOfType<AudioSource>(includeInactive)
                     .Where(a => a && a.gameObject.scene.IsValid())
                     .ToList();
#else
        var list = new List<AudioSource>();
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in roots)
        {
            if (!go) continue;
            list.AddRange(go.GetComponentsInChildren<AudioSource>(true));
        }
        return list;
#endif
    }

    IEnumerable<Transform> GetAllSceneTransforms(bool includeInactive)
    {
#if UNITY_2020_1_OR_NEWER
        return Object.FindObjectsOfType<Transform>(includeInactive)
                     .Where(t => t && t.gameObject.scene.IsValid());
#else
        var list = new List<Transform>();
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in roots)
        {
            if (!go) continue;
            list.AddRange(go.GetComponentsInChildren<Transform>(true));
        }
        return list;
#endif
    }

    void PrepareSources()
    {
        EnsureLoop(wind, true);
        EnsureLoop(crickets, true);
        EnsureLoop(lowLevelCrackles, true);
        EnsureLoop(lowLevelNearEndSynth, true);
        EnsureLoop(midLevelCrackle, true);
        EnsureLoop(midLevelSynth, true);
        EnsureLoop(midLevelPulseSynth, true);
        EnsureLoop(largeFlameCrackle, true);
        EnsureLoop(largeFlameBuild, true);
        EnsureLoop(largeFlameSynth, true);
        EnsureLoop(midLevelWhoosh, false);
        EnsureLoop(largeLevelFlameWhoosh, false);
    }

    void HardSilenceAllNonAmbience()
    {
        var all = AllPhaseSources();
        for (int i = 0; i < all.Count; i++)
        {
            var s = all[i];
            if (!s) continue;
            s.playOnAwake = false;
            s.volume = 0f;
            if (s.isPlaying) s.Stop();
        }
        if (wind) wind.playOnAwake = false;
        if (crickets) crickets.playOnAwake = false;
    }

    List<AudioSource> AllPhaseSources()
    {
        var list = new List<AudioSource>(12);
        if (lowLevelCrackles) list.Add(lowLevelCrackles);
        if (lowLevelNearEndSynth) list.Add(lowLevelNearEndSynth);
        if (midLevelCrackle) list.Add(midLevelCrackle);
        if (midLevelWhoosh) list.Add(midLevelWhoosh);
        if (midLevelSynth) list.Add(midLevelSynth);
        if (midLevelPulseSynth) list.Add(midLevelPulseSynth);
        if (largeFlameCrackle) list.Add(largeFlameCrackle);
        if (largeLevelFlameWhoosh) list.Add(largeLevelFlameWhoosh);
        if (largeFlameBuild) list.Add(largeFlameBuild);
        if (largeFlameSynth) list.Add(largeFlameSynth);
        return list;
    }
}
