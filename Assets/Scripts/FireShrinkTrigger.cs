using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireShrinkTrigger : MonoBehaviour

{
    public FireSizeChanger fire;
    [Tooltip("Which stage to shrink to (1=Fireball, 2=Small, 3=Big).")]
    public int targetStage = 1;

    public void TriggerShrink()
    {
        if (fire)
        {
            fire.SetStageByNumber(targetStage);
            Debug.Log($"Shrink trigger: fire set to Phase {targetStage}");
        }
    }
}
