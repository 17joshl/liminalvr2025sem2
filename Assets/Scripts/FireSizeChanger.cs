using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSizeChanger : MonoBehaviour
{
    [Header("Manual Overrides")]
    public ParticleSystem fireball;   // Phase 1
    public ParticleSystem smallFire;  // Phase 2
    public ParticleSystem bigFlames;  // Phase 3

    void Awake()
    {
        if (!fireball || !smallFire || !bigFlames)
        {
            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem ps in systems)
            {
                string n = ps.name.ToLower();

                // --- Phase 1: Fireball ---
                if (!fireball && (
                    n.Contains("fireball (1)") ||
                    n.Contains("fireball base (1)")
                ))
                {
                    fireball = ps;
                    continue;
                }

                // --- Phase 2: Small Fire ---
                if (!smallFire && (
                    n.Contains("smallfire") ||
                    n.Contains("small fire") ||
                    n.Contains("smalldefault") 
                   
                ))
                {
                    smallFire = ps;
                    continue;
                }

                // --- Phase 3: Large Flames ---
                if (!bigFlames && (
                    n.Contains("largeflames") ||
                    n.Contains("large flames") ||
                    n.Contains("large flames (particle system)") ||
                    (n.Contains("big") && !n.Contains("smoke") && !n.Contains("sub"))
                ))
                {
                    bigFlames = ps;
                    continue;
                }
            }

            // Fallback assignment if any are still null
            if (!fireball && systems.Length > 0) fireball = systems[0];
            if (!smallFire && systems.Length > 1) smallFire = systems[1];
            if (!bigFlames && systems.Length > 2) bigFlames = systems[2];
        }
    }

    // 1 = Fireball, 2 = Small, 3 = Big
    public void SetStageByNumber(int phase)
    {
        phase = Mathf.Clamp(phase, 1, 3);
        SetActive(fireball, phase == 1);
        SetActive(smallFire, phase == 2);
        SetActive(bigFlames, phase == 3);
    }

    void SetActive(ParticleSystem ps, bool on)
    {
        if (!ps) return;

        ps.gameObject.SetActive(on);
        var emission = ps.emission;
        emission.enabled = on;

        if (on && !ps.isPlaying)
            ps.Play(true);
        else if (!on && ps.isPlaying)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
