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
    public Transform soundRoot;

    public AudioSource wind;
    public AudioSource crickets;

    public AudioSource lowLevelCrackles;
    public AudioSource lowLevelNearEndSynth;

    public AudioSource midLevelCrackle;
    public AudioSource midLevelWhoosh;
    public AudioSource midLevelSynth;
    public AudioSource midLevelPulseSynth;

    public AudioSource largeFlameCrackle;
    public AudioSource largeLevelFlameWhoosh;
    public AudioSource largeFlameBuild;
    public AudioSource largeFlameSynth;

    public AudioSource midLevelStrum;
    public AudioSource largeLevelStrum;

    [Header("Strum Timing (seconds)")]
    public float midStrumIntervalSeconds = 10f;
    public float largeStrumIntervalSeconds = 10f;

    [Header("Strum Levels")]
    [Range(0f, 1f)] public float midStrumVolume = 1f;
    [Range(0f, 1f)] public float largeStrumVolume = 1f;

    [Header("Fades")]
    [SerializeField] float xfade = 1.25f;
    [SerializeField] float quickFade = 0.35f;
    [SerializeField] float gazeFade = 0.65f;
    [SerializeField] float strumFadeOut = 0.25f;

    [SerializeField] float nearEndDelay = 15f;
    [Range(0f, 1f)] public float master = 1f;

    public WindVolumeProfile windProfile = new WindVolumeProfile { phase1 = 1f, phase2 = 0.66f, phase3 = 0.50f };

    [SerializeField, Range(0, 3)] int initialPhase = 1;

    readonly List<Coroutine> running = new List<Coroutine>();
    readonly Dictionary<AudioSource, Coroutine> fadeBySource = new Dictionary<AudioSource, Coroutine>();

    Coroutine p1NearEndRoutine;
    Coroutine transitionRoutine;
    Coroutine p2StrumRoutine;
    Coroutine p3StrumRoutine;

    Coroutine midStrumFadeOutCo;
    Coroutine largeStrumFadeOutCo;

    int currentPhase = 0;
    bool gazeOn = true;
    bool isTransitioning = false;

    Dictionary<AudioSource, float> baseVol = new Dictionary<AudioSource, float>(16);

    const float oneShotFloor = 0.15f;

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
        CacheInitialVolumes();
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
        CacheInitialVolumes();
        HardSilenceAllNonAmbience();
        ApplyPhaseImmediate(currentPhase == 0 ? 0 : currentPhase);
        ApplyGazeGate(true);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        running.Clear();
        fadeBySource.Clear();
        p1NearEndRoutine = null;
        transitionRoutine = null;
        p2StrumRoutine = null;
        p3StrumRoutine = null;
        midStrumFadeOutCo = null;
        largeStrumFadeOutCo = null;
        isTransitioning = false;
    }

    public void SetPhase(int phase)
    {
        phase = Mathf.Clamp(phase, 1, 3);
        if (phase == currentPhase) return;

        StopRunning();

        float windTarget = phase == 1 ? windProfile.phase1 : (phase == 2 ? windProfile.phase2 : windProfile.phase3);
        float cricketsTarget = phase == 1 ? 1f : (phase == 2 ? 0.50f : 0.00f);

        FadeTo(wind, windTarget * GetBase(wind) * master, xfade, true);
        FadeTo(crickets, cricketsTarget * GetBase(crickets) * master, xfade, true);

        currentPhase = phase;

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionToPhase(phase));
    }

    IEnumerator TransitionToPhase(int phase)
    {
        isTransitioning = true;

        if (phase == 1)
        {
            FadeTo(lowLevelCrackles, 1f * GetBase(lowLevelCrackles) * master, xfade, true);

            FadeTo(midLevelCrackle, 0f, xfade);
            FadeTo(midLevelSynth, 0f, xfade);
            FadeTo(midLevelPulseSynth, 0f, xfade);
            FadeTo(largeFlameCrackle, 0f, xfade);
            FadeTo(largeFlameBuild, 0f, xfade);
            FadeTo(largeFlameSynth, 0f, xfade);

            if (p1NearEndRoutine != null) StopCoroutine(p1NearEndRoutine);
            p1NearEndRoutine = StartCoroutine(Phase1NearEndSynthRoutine());
            StopStrumLoops();
        }
        else if (phase == 2)
        {
            if (p1NearEndRoutine != null) { StopCoroutine(p1NearEndRoutine); p1NearEndRoutine = null; }
            FadeTo(lowLevelNearEndSynth, 0f, xfade);
            FadeTo(lowLevelCrackles, 0f, xfade);

            FadeTo(midLevelCrackle, 1f * GetBase(midLevelCrackle) * master, xfade, true);
            FadeTo(midLevelSynth, (gazeOn ? 1f : 0f) * GetBase(midLevelSynth) * master, xfade, true);
            FadeTo(midLevelPulseSynth, (gazeOn ? 1f : 0f) * GetBase(midLevelPulseSynth) * master, xfade, true);

            FadeTo(largeFlameCrackle, 0f, xfade);
            FadeTo(largeFlameBuild, 0f, xfade);
            FadeTo(largeFlameSynth, 0f, xfade);

            OneShot(midLevelWhoosh);
            StartP2StrumLoopIfNeeded(true);
        }
        else
        {
            FadeTo(lowLevelCrackles, 0f, xfade);
            FadeTo(midLevelCrackle, 0f, xfade);
            FadeTo(midLevelSynth, 0f, xfade);
            FadeTo(midLevelPulseSynth, 0f, xfade);
            FadeTo(lowLevelNearEndSynth, 0f, xfade);

            FadeTo(largeFlameCrackle, 1f * GetBase(largeFlameCrackle) * master, xfade, true);
            FadeTo(largeFlameBuild, (gazeOn ? 1f : 0f) * GetBase(largeFlameBuild) * master, xfade, true);
            FadeTo(largeFlameSynth, (gazeOn ? 1f : 0f) * GetBase(largeFlameSynth) * master, xfade, true);

            OneShot(largeLevelFlameWhoosh);
            StartP3StrumLoopIfNeeded(true);
        }

        float t = 0f;
        while (t < xfade) { t += Time.deltaTime; yield return null; }

        isTransitioning = false;
        ApplyGazeGate(false);
        transitionRoutine = null;
    }

    public void SetGaze(bool isOn)
    {
        gazeOn = isOn;
        if (!gazeOn)
        {
            StopStrumLoops();
            FadeOutStopStrum(midLevelStrum, ref midStrumFadeOutCo, strumFadeOut);
            FadeOutStopStrum(largeLevelStrum, ref largeStrumFadeOutCo, strumFadeOut);
        }
        else
        {
            if (currentPhase == 2) StartP2StrumLoopIfNeeded(true);
            else if (currentPhase == 3) StartP3StrumLoopIfNeeded(true);
        }
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
        EnsureLoop(midLevelStrum, false);
        EnsureLoop(largeLevelStrum, false);

        if (phase == 0)
        {
            if (wind) { wind.volume = 1f * GetBase(wind) * master; SafePlay(wind); }
            if (crickets) { crickets.volume = 1f * GetBase(crickets) * master; SafePlay(crickets); }

            MutePause(lowLevelCrackles);
            MutePause(lowLevelNearEndSynth);
            MutePause(midLevelCrackle);
            MutePause(midLevelSynth);
            MutePause(midLevelPulseSynth);
            MutePause(largeFlameCrackle);
            MutePause(largeFlameBuild);
            MutePause(largeFlameSynth);
            StopStrumLoops();
            return;
        }

        if (wind)
        {
            float w = (phase == 1 ? windProfile.phase1 : phase == 2 ? windProfile.phase2 : phase == 3 ? windProfile.phase3 : 1f);
            wind.volume = w * GetBase(wind) * master; SafePlay(wind);
        }
        if (crickets)
        {
            float c = (phase == 1 ? 1f : phase == 2 ? 0.50f : 0.00f);
            crickets.volume = c * GetBase(crickets) * master; SafePlay(crickets);
        }

        if (phase == 1)
        {
            if (lowLevelCrackles) { lowLevelCrackles.volume = 1f * GetBase(lowLevelCrackles) * master; SafePlay(lowLevelCrackles); }
            if (lowLevelNearEndSynth) lowLevelNearEndSynth.volume = 0f;
            MutePause(midLevelCrackle);
            MutePause(midLevelSynth);
            MutePause(midLevelPulseSynth);
            MutePause(largeFlameCrackle);
            MutePause(largeFlameBuild);
            MutePause(largeFlameSynth);
            if (p1NearEndRoutine != null) StopCoroutine(p1NearEndRoutine);
            p1NearEndRoutine = StartCoroutine(Phase1NearEndSynthRoutine());
            StopStrumLoops();
        }
        else if (phase == 2)
        {
            if (midLevelCrackle) { midLevelCrackle.volume = 1f * GetBase(midLevelCrackle) * master; SafePlay(midLevelCrackle); }
            if (midLevelSynth) { midLevelSynth.volume = (gazeOn ? 1f : 0f) * GetBase(midLevelSynth) * master; SafePlay(midLevelSynth); }
            if (midLevelPulseSynth) { midLevelPulseSynth.volume = (gazeOn ? 1f : 0f) * GetBase(midLevelPulseSynth) * master; SafePlay(midLevelPulseSynth); }
            MutePause(lowLevelCrackles);
            MutePause(lowLevelNearEndSynth);
            MutePause(largeFlameCrackle);
            MutePause(largeFlameBuild);
            MutePause(largeFlameSynth);
            StartP2StrumLoopIfNeeded(true);
        }
        else
        {
            if (largeFlameCrackle) { largeFlameCrackle.volume = 1f * GetBase(largeFlameCrackle) * master; SafePlay(largeFlameCrackle); }
            if (largeFlameBuild) { largeFlameBuild.volume = (gazeOn ? 1f : 0f) * GetBase(largeFlameBuild) * master; SafePlay(largeFlameBuild); }
            if (largeFlameSynth) { largeFlameSynth.volume = (gazeOn ? 1f : 0f) * GetBase(largeFlameSynth) * master; SafePlay(largeFlameSynth); }
            MutePause(lowLevelCrackles);
            MutePause(lowLevelNearEndSynth);
            MutePause(midLevelCrackle);
            MutePause(midLevelSynth);
            MutePause(midLevelPulseSynth);
            StartP3StrumLoopIfNeeded(true);
        }

        ApplyGazeGate(true);
    }

    void ApplyGazeGate(bool immediate = false)
    {
        if (isTransitioning) return;

        float t = immediate ? 0f : gazeFade;
        bool p1Active = currentPhase == 1;
        bool p2Active = currentPhase == 2;
        bool p3Active = currentPhase == 3;

        if (p1Active && lowLevelNearEndSynth)
            FadeTo(lowLevelNearEndSynth, (gazeOn ? 1f : 0f) * GetBase(lowLevelNearEndSynth) * master, t, true);

        if (p2Active)
        {
            FadeTo(midLevelSynth, (gazeOn ? 1f : 0f) * GetBase(midLevelSynth) * master, t, true);
            FadeTo(midLevelPulseSynth, (gazeOn ? 1f : 0f) * GetBase(midLevelPulseSynth) * master, t, true);
            if (gazeOn) StartP2StrumLoopIfNeeded(false); else StopP2StrumLoop();
        }

        if (p3Active)
        {
            FadeTo(largeFlameBuild, (gazeOn ? 1f : 0f) * GetBase(largeFlameBuild) * master, t, true);
            FadeTo(largeFlameSynth, (gazeOn ? 1f : 0f) * GetBase(largeFlameSynth) * master, t, true);
            if (gazeOn) StartP3StrumLoopIfNeeded(false); else StopP3StrumLoop();
        }
    }

    void StopRunning()
    {
        for (int i = 0; i < running.Count; i++)
        {
            if (running[i] != null) StopCoroutine(running[i]);
        }
        running.Clear();
        foreach (var kv in fadeBySource) if (kv.Value != null) StopCoroutine(kv.Value);
        fadeBySource.Clear();
    }

    void FadeTo(AudioSource s, float vol, float dur, bool loop = false)
    {
        if (!s) return;
        EnsureLoop(s, loop);

        if (fadeBySource.TryGetValue(s, out var existing) && existing != null)
            StopCoroutine(existing);

        if (vol > 0f)
        {
            if (!s.isPlaying)
            {
                if (s.time > 0f) s.UnPause(); else s.Play();
            }
        }

        var co = StartCoroutine(FadeCR(s, vol, dur));
        fadeBySource[s] = co;
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
            if (Mathf.Approximately(vol, 0f))
            {
                if (s.isPlaying) s.Pause();
            }
        }
    }

    void OneShot(AudioSource s)
    {
        if (!s) return;
        s.loop = false;
        float baseV = Mathf.Max(oneShotFloor, GetBase(s) * master);
        s.volume = baseV;
        if (s.clip) s.PlayOneShot(s.clip, 1f);
        else { s.Play(); }
    }

    void EnsureLoop(AudioSource s, bool loop)
    {
        if (!s) return;
        s.loop = loop;
        s.playOnAwake = false;
        s.mute = false;
    }

    void SafePlay(AudioSource s)
    {
        if (!s) return;
        if (s.time > 0f && !s.isPlaying) s.UnPause();
        else if (!s.isPlaying) s.Play();
    }

    void MutePause(AudioSource s)
    {
        if (!s) return;
        s.volume = 0f;
        if (s.isPlaying) s.Pause();
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
        if (currentPhase == 1 && gazeOn)
            FadeTo(lowLevelNearEndSynth, 1f * GetBase(lowLevelNearEndSynth) * master, xfade, true);
    }

    [ContextMenu("Rebind Audio Now")]
    public void RebindAudioNow()
    {
        TryResolveSoundRoot();
        AutoDiscoverIfMissing();
        PrepareSources();
        CacheInitialVolumes();
        HardSilenceAllNonAmbience();
        ApplyPhaseImmediate(currentPhase);
        ApplyGazeGate(true);
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
            TryBindFromPath(soundRoot, "Strums/MidLevelStrum", ref midLevelStrum);
            TryBindFromPath(soundRoot, "Strums/LargeFlameStrum", ref largeLevelStrum);
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
        if (!midLevelStrum) midLevelStrum = FindByAny(all, "midlevelstrum", "mid level strum", "midstrum", "p2 strum");
        if (!largeLevelStrum) largeLevelStrum = FindByAny(all, "largeflamestrum", "large flame strum", "p3 strum", "largelevelstrum");
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
        EnsureLoop(midLevelStrum, false);
        EnsureLoop(largeLevelStrum, false);
    }

    void CacheInitialVolumes()
    {
        var all = AllPhaseSources();
        if (wind && !baseVol.ContainsKey(wind)) baseVol[wind] = Mathf.Clamp01(wind.volume <= 0f ? 1f : wind.volume);
        if (crickets && !baseVol.ContainsKey(crickets)) baseVol[crickets] = Mathf.Clamp01(crickets.volume <= 0f ? 1f : crickets.volume);
        for (int i = 0; i < all.Count; i++)
        {
            var s = all[i]; if (!s) continue;
            if (!baseVol.ContainsKey(s))
            {
                float v = s.volume;
                baseVol[s] = Mathf.Clamp01(v <= 0f ? 1f : v);
            }
        }
        if (midLevelStrum && !baseVol.ContainsKey(midLevelStrum)) baseVol[midLevelStrum] = Mathf.Clamp01(midLevelStrum.volume <= 0f ? 1f : midLevelStrum.volume);
        if (largeLevelStrum && !baseVol.ContainsKey(largeLevelStrum)) baseVol[largeLevelStrum] = Mathf.Clamp01(largeLevelStrum.volume <= 0f ? 1f : largeLevelStrum.volume);
    }

    float GetBase(AudioSource s)
    {
        if (!s) return 1f;
        float v;
        if (!baseVol.TryGetValue(s, out v))
        {
            v = Mathf.Clamp01(s.volume <= 0f ? 1f : s.volume);
            baseVol[s] = v;
        }
        return v;
    }

    void HardSilenceAllNonAmbience()
    {
        var all = AllPhaseSources();
        for (int i = 0; i < all.Count; i++)
        {
            var s = all[i];
            if (!s) continue;
            s.playOnAwake = false;

            if (s == midLevelStrum || s == largeLevelStrum)
            {
                if (s.isPlaying) s.Pause();
                continue;
            }

            s.volume = 0f;
            if (s.isPlaying) s.Pause();
        }
        if (wind) wind.playOnAwake = false;
        if (crickets) crickets.playOnAwake = false;
    }

    List<AudioSource> AllPhaseSources()
    {
        var list = new List<AudioSource>(16);
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
        if (midLevelStrum) list.Add(midLevelStrum);
        if (largeLevelStrum) list.Add(largeLevelStrum);
        return list;
    }

    void StartP2StrumLoopIfNeeded(bool restartTimer)
    {
        if (currentPhase != 2 || !gazeOn || midLevelStrum == null) { StopP2StrumLoop(); return; }
        if (p2StrumRoutine != null) return;
        float initialDelay = Mathf.Max(0.1f, midStrumIntervalSeconds);
        p2StrumRoutine = StartCoroutine(StrumLoop(
            midLevelStrum,
            Mathf.Max(0.1f, midStrumIntervalSeconds),
            () => currentPhase == 2 && gazeOn,
            initialDelay,
            () => Mathf.Clamp01(midStrumVolume)));
    }

    void StartP3StrumLoopIfNeeded(bool restartTimer)
    {
        if (currentPhase != 3 || !gazeOn || largeLevelStrum == null) { StopP3StrumLoop(); return; }
        if (p3StrumRoutine != null) return;
        float initialDelay = Mathf.Max(0.1f, largeStrumIntervalSeconds);
        p3StrumRoutine = StartCoroutine(StrumLoop(
            largeLevelStrum,
            Mathf.Max(0.1f, largeStrumIntervalSeconds),
            () => currentPhase == 3 && gazeOn,
            initialDelay,
            () => Mathf.Clamp01(largeStrumVolume)));
    }

    void StopP2StrumLoop()
    {
        if (p2StrumRoutine != null) StopCoroutine(p2StrumRoutine);
        p2StrumRoutine = null;
    }

    void StopP3StrumLoop()
    {
        if (p3StrumRoutine != null) StopCoroutine(p3StrumRoutine);
        p3StrumRoutine = null;
    }

    void StopStrumLoops()
    {
        StopP2StrumLoop();
        StopP3StrumLoop();
    }

    IEnumerator StrumLoop(AudioSource src, float intervalSeconds, System.Func<bool> condition, float initialDelaySeconds, System.Func<float> getVolumeMul)
    {
        float wait = Mathf.Max(0.1f, intervalSeconds);
        float initialWait = Mathf.Max(0.1f, initialDelaySeconds);

        float t0 = 0f;
        while (t0 < initialWait && condition())
        {
            t0 += Time.deltaTime;
            yield return null;
        }
        if (!condition()) yield break;

        while (condition())
        {
            float volMul = Mathf.Clamp01(getVolumeMul != null ? getVolumeMul() : 1f);
            float baseV = Mathf.Max(oneShotFloor, GetBase(src) * master * volMul);

            if (src)
            {
                src.volume = baseV;
                if (!src.clip)
                {
                    src.Play();
                }
                else
                {
                    if (src.isPlaying) src.Stop();
                    src.time = 0f;
                    src.Play();
                }
            }

            float t = 0f;
            while (t < wait && condition())
            {
                t += Time.deltaTime;
                yield return null;
            }
            if (!condition()) break;
        }
    }

    void FadeOutStopStrum(AudioSource s, ref Coroutine holder, float dur)
    {
        if (!s || !s.isPlaying) return;
        if (holder != null) StopCoroutine(holder);
        holder = StartCoroutine(FadeOutStopCR(s, dur));
    }

    IEnumerator FadeOutStopCR(AudioSource s, float dur)
    {
        float v0 = s.volume;
        float t = 0f;
        dur = Mathf.Max(0f, dur);
        while (t < dur && s && s.isPlaying)
        {
            t += Time.deltaTime;
            float k = dur <= 0f ? 1f : Mathf.Clamp01(t / dur);
            s.volume = Mathf.Lerp(v0, 0f, k);
            yield return null;
        }
        if (s)
        {
            s.volume = 0f;
            s.Stop();
        }
    }
}
