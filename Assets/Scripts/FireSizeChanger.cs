using System.Linq;
using UnityEngine;

public class FireSizeChanger : MonoBehaviour
{
    public Transform[] phase1Objects; // StartingFire
    public Transform[] phase2Objects; // StartingFire + Main/MainFire
    public Transform[] phase3Objects; // Main/MainFire + SupportingFlames + RedFlames + StylizedFire1
    public bool debugBinding = false;

    void Awake()
    {
        AutoBind();
        SetStageByNumber(1);
    }

    void AutoBind()
    {
        var all = GetComponentsInChildren<Transform>(true)
                  .Where(t => t && t != transform).ToArray();

        // helpers
        System.Func<Transform, bool> isStarting =
            t => t.name == "StartingFire" || t.name.ToLower().Replace(" ", "") == "startingfire";

        System.Func<Transform, bool> isMain =
            t =>
            {
                var n = t.name.ToLower();
                var ns = n.Replace(" ", "");
                return t.name == "MainFire" || t.name == "Main" ||
                       ns.Contains("mainfire") || n.Contains("main fire") || n == "main";
            };

        System.Func<Transform, bool> isPhase3Extra =
            t =>
            {
                var n = t.name.ToLower();
                return n == "supportingflames" || n == "redflames" || n == "stylizedfire1" ||
                       n.Contains("supportingflames") || n.Contains("redflames") || n.Contains("stylizedfire");
            };

        // PHASE 1: StartingFire
        if (phase1Objects == null || phase1Objects.Length == 0)
            phase1Objects = all.Where(isStarting).ToArray();

        // PHASE 2: StartingFire + Main/MainFire
        if (phase2Objects == null || phase2Objects.Length == 0)
        {
            var p2 = all.Where(isStarting).Concat(all.Where(isMain)).Distinct().ToArray();
            phase2Objects = p2;
        }

        // PHASE 3: Main/MainFire + extras
        if (phase3Objects == null || phase3Objects.Length == 0)
        {
            var p3 = all.Where(isMain).Concat(all.Where(isPhase3Extra)).Distinct().ToArray();
            phase3Objects = p3;
        }

        // Exclude CampFire everywhere (base only)
        phase1Objects = phase1Objects.Where(t => t && t.name != "CampFire").ToArray();
        phase2Objects = phase2Objects.Where(t => t && t.name != "CampFire").ToArray();
        phase3Objects = phase3Objects.Where(t => t && t.name != "CampFire").ToArray();

        if (debugBinding)
        {
            Debug.Log("[FireSizeChanger] Phase1: " + string.Join(", ", phase1Objects.Select(t => t.name)));
            Debug.Log("[FireSizeChanger] Phase2: " + string.Join(", ", phase2Objects.Select(t => t.name)));
            Debug.Log("[FireSizeChanger] Phase3: " + string.Join(", ", phase3Objects.Select(t => t.name)));
        }
    }

    public void SetStageByNumber(int phase)
    {
        phase = Mathf.Clamp(phase, 1, 3);

        // turn EVERYTHING off first so phases don’t overlap unexpectedly
        SetGroupActive(phase1Objects, false);
        SetGroupActive(phase2Objects, false);
        SetGroupActive(phase3Objects, false);

        if (phase == 1) SetGroupActive(phase1Objects, true);
        else if (phase == 2) SetGroupActive(phase2Objects, true);
        else SetGroupActive(phase3Objects, true);
    }

    void SetGroupActive(Transform[] group, bool on)
    {
        if (group == null) return;

        for (int i = 0; i < group.Length; i++)
        {
            var t = group[i];
            if (!t) continue;

            t.gameObject.SetActive(on);

            var rens = t.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < rens.Length; r++) rens[r].enabled = on;

            var ps = t.GetComponentsInChildren<ParticleSystem>(true);
            for (int p = 0; p < ps.Length; p++)
            {
                var e = ps[p].emission; e.enabled = on;
                if (on && !ps[p].isPlaying) ps[p].Play(true);
                else if (!on && ps[p].isPlaying) ps[p].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
