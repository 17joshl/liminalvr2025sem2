using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireGrowTrigger : MonoBehaviour

{
    public FireSizeChanger fire;
    [Tooltip("Which stage to grow to (1=Fireball, 2=Small, 3=Big).")]
    public int targetStage = 2;

    public void TriggerGrow()
    {
        if (fire)
        {
            fire.SetStageByNumber(targetStage);
            Debug.Log($"Grow trigger: fire set to Phase {targetStage}");
        }
    }
}

