using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class FireSizeChanger : MonoBehaviour

{
    public ParticleSystem fireball;   // Phase 1
    public ParticleSystem smallFire;  // Phase 2
    public ParticleSystem bigFlames;  // Phase 3

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

        if (on && !ps.isPlaying) ps.Play(true);
        if (!on && ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
