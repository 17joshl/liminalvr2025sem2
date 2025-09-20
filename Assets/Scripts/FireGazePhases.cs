using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class FireGazePhases : MonoBehaviour

{
    [Header("Look Target & Camera")]
    public Transform fireRoot;
    public Camera playerCamera;
    public LayerMask hitMask = ~0;
    public float maxRayDistance = 50f;
    [Range(0f, 30f)] public float centerDeadZoneDegrees = 8f;

    [Header("Phase Up Times (LOOKING)")]
    public float phase1to2Time = 30f; // Phase1 to Phase2
    public float phase2to3Time = 30f; // Phase2 to Phase3

    [Header("Phase Down Times (AWAY)")]
    public float phase3to2Time = 30f; // Phase3 to Phase2
    public float phase2to1Time = 30f; // Phase2 to Phase1

    [Header("Refs")]
    public FireSizeChanger fireController;

    [Header("UI")]
    public Text messageText;   
    public Text timerText;     
    public Text phaseText;     
    public bool showTimer = true;
    public bool showPhase = true;

    [Header("Testing / Debug")]
    public bool alwaysLooking = false;
    public bool enableKeyboardShortcuts = true;

    int currentPhase = 1; // 1=Fireball, 2=Small, 3=Big
    float lookTimer = 0f;
    float awayTimer = 0f;

    void Start()
    {
        if (!playerCamera) playerCamera = Camera.main;
        SetPhase(1, "Start: Phase 1 (Fireball)");
        UpdateInfoUI();
    }

    void Update()
    {
        if (!playerCamera || !fireRoot || !fireController) return;
        
        
        if (enableKeyboardShortcuts)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ForcePhase(1, "Manual: Phase 1 (Fireball)");
            if (Input.GetKeyDown(KeyCode.Alpha2)) ForcePhase(2, "Manual: Phase 2 (Small Fire)");
            if (Input.GetKeyDown(KeyCode.Alpha3)) ForcePhase(3, "Manual: Phase 3 (Big Flames)");
        }

        bool looking = alwaysLooking || IsLookingAtFire();

        if (looking)
        {
            awayTimer = 0f;
            lookTimer += Time.deltaTime;

            if (currentPhase == 1 && lookTimer >= phase1to2Time)
            {
                SetPhase(2, "You have entered Phase 2 (Small Fire)");
                lookTimer = 0f;
            }
            else if (currentPhase == 2 && lookTimer >= phase2to3Time)
            {
                SetPhase(3, "You have entered Phase 3 (Big Flames)");
                lookTimer = 0f;
            }
        }
        else
        {
            lookTimer = 0f;
            awayTimer += Time.deltaTime;

            if (currentPhase == 3 && awayTimer >= phase3to2Time)
            {
                SetPhase(2, "Shrinking to Phase 2 (Small Fire)");
                awayTimer = 0f;
            }
            else if (currentPhase == 2 && awayTimer >= phase2to1Time)
            {
                SetPhase(1, "Shrinking to Phase 1 (Fireball)");
                awayTimer = 0f;
            }
        }

        UpdateInfoUI();
    }

    bool IsLookingAtFire()
    {
        Vector3 fwd = playerCamera.transform.forward;
        Vector3 dir = (fireRoot.position - playerCamera.transform.position).normalized;
        if (Vector3.Angle(fwd, dir) > centerDeadZoneDegrees) return false;

        Ray ray = new Ray(playerCamera.transform.position, fwd);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, hitMask, QueryTriggerInteraction.Ignore))
            return hit.transform == fireRoot || hit.transform.IsChildOf(fireRoot);

        return false;
    }

    void SetPhase(int phase, string msg)
    {
        int clamped = Mathf.Clamp(phase, 1, 3);
        if (clamped == currentPhase) { ShowMessage(msg); return; }

        currentPhase = clamped;
        fireController.SetStageByNumber(currentPhase);
        ShowMessage(msg);
    }

    void ForcePhase(int phase, string msg)
    {
        currentPhase = Mathf.Clamp(phase, 1, 3);
        fireController.SetStageByNumber(currentPhase);
        lookTimer = 0f; awayTimer = 0f;
        ShowMessage(msg);
    }

    void ShowMessage(string msg)
    {
        if (messageText)
        {
            messageText.alignment = TextAnchor.UpperLeft;
            messageText.text = msg;
            CancelInvoke(nameof(ClearMessage));
            Invoke(nameof(ClearMessage), 3f);
        }
        Debug.Log(msg);
    }

    void ClearMessage() { if (messageText) messageText.text = ""; }

    
    void UpdateInfoUI()
    {
        
        if (timerText)
        {
            if (showTimer)
            {
                float t = Mathf.Max(lookTimer, awayTimer);
                timerText.alignment = TextAnchor.UpperRight;
                timerText.text = $"Timer: {t:0.0}s";
                if (!timerText.gameObject.activeSelf) timerText.gameObject.SetActive(true);
            }
            else
            {
                timerText.text = "";
                if (timerText.gameObject.activeSelf) timerText.gameObject.SetActive(false);
            }
        }

        
        if (phaseText)
        {
            if (showPhase)
            {
                phaseText.alignment = TextAnchor.UpperRight;
                phaseText.text = $"Current Phase: {currentPhase}";
                if (!phaseText.gameObject.activeSelf) phaseText.gameObject.SetActive(true);
            }
            else
            {
                phaseText.text = "";
                if (phaseText.gameObject.activeSelf) phaseText.gameObject.SetActive(false);
            }
        }
    }
}