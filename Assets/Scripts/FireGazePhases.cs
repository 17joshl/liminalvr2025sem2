using UnityEngine;
using UnityEngine.UI;

public class FireGazePhases : MonoBehaviour
{
    public enum DeadzoneMode { ViewportRadius, PixelRadius, AngularDegrees }

    [Header("Look Target & Camera")]
    public Transform fireRoot;
    public Camera playerCamera;
    public LayerMask hitMask = ~0;
    public float maxRayDistance = 50f;

    [Header("Dead Zone")]
    public DeadzoneMode deadzoneMode = DeadzoneMode.ViewportRadius;
    [Range(0.01f, 0.6f)] public float deadZoneRadius = 0.2f;   
    public float deadZonePixels = 180f;                        
    public float deadZoneDegrees = 14f;                        

    [Header("Gaze Volume")]
    public Collider gazeCollider;          
    public bool useSphereCast = false;
    public float sphereCastRadius = 0.1f;

    [Header("Gaze Filtering")]
    public bool requirePhysicsHit = true;  

    [Header("Anchor Fallback Tuning")]
    [Range(0f, 1f)] public float anchorBiasFromBottom = 0.25f;

    [Header("Phase Timings (Seconds)")]
    public float phase1to2Time = 30f;
    public float phase2to3Time = 30f;
    public float phase3to2Time = 30f;
    public float phase2to1Time = 30f;

    float totalLookTime = 0f;

    
    [Header("Fade Effect")]
    public FadeToBlack fadeController;
    public float timeUntilFade = 30f;

    [Header("Refs")]
    public FireChanger fireController;
    public FireSizeChanger fireObjectController;

    public GameObject supportingBonfire;
    public GameObject supportingGlow;




    [Header("UI")]
    public Text messageText;
    public Text timerText;
    public Text phaseText;
    public bool showTimer = true;
    public bool showPhase = true;

    [Header("SFX")]
    [SerializeField] private AudioClip[] fireClips;

    [Header("Testing / Debug")]
    public bool alwaysLooking = false;
    public bool enableKeyboardShortcuts = true;
    public bool drawDebugRay = true;
    public bool debugLog = false;

    int currentPhase = 1;
    float lookTimer = 0f;
    float awayTimer = 0f;

    public bool gazeActive;
    public Vector3 lastAnchorWorld;
    public Vector2 lastAnchorViewport;
    public float lastAnchorAngleDeg;

    void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;
        if (!fireController) fireController = FindObjectOfType<FireChanger>();
        if (!fireRoot && fireController) fireRoot = fireController.transform;

        if (!gazeCollider)
        {
            gazeCollider = GetComponent<Collider>();
            if (!gazeCollider) gazeCollider = GetComponentInChildren<Collider>();
        }
    }

    void Start()
    {
        if (!playerCamera || !fireController) { enabled = false; return; }
        SetPhase(1, "Start: Phase 1");
        UpdateInfoUI();
    }

    void Update()
    {
        if (!playerCamera || !fireController) return;

        if (enableKeyboardShortcuts)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ForcePhase(1, "Manual: Phase 1");
            if (Input.GetKeyDown(KeyCode.Alpha2)) ForcePhase(2, "Manual: Phase 2");
            if (Input.GetKeyDown(KeyCode.Alpha3)) ForcePhase(3, "Manual: Phase 3");
        }

        bool looking = alwaysLooking || IsLookingAtFire();
        gazeActive = looking;


        var visual = supportingBonfire.GetComponent<SupportingFireController>();
        var glowVisual = supportingGlow.GetComponent<SupportingGlowController>();


        if (visual)
        {
            if (looking) visual.FadeIn();
            else visual.FadeOut();
        }

        if (glowVisual)
        {
            if (looking) glowVisual.FadeIn();
            else glowVisual.FadeOut();
        }


        var audioCtrl = fireObjectController ? fireObjectController.audioCtrl : null;
        if (audioCtrl) audioCtrl.SetGaze(looking);

        fireController.SetGlowActive(looking);

        if (looking)
        {
            awayTimer = 0f;
            lookTimer += Time.deltaTime;
            totalLookTime += Time.deltaTime;


            if (currentPhase == 1 && totalLookTime >= phase1to2Time) {
                SetPhase(2, "Phase 2");
            }
            else if (currentPhase == 2 && totalLookTime >= phase1to2Time + phase2to3Time) {
                SetPhase(3, "Phase 3");
            }

            //if (currentPhase == 1 && lookTimer >= phase1to2Time) { SetPhase(2, "Phase 2"); lookTimer = 0f; }
            //else if (currentPhase == 2 && lookTimer >= phase2to3Time) { SetPhase(3, "Phase 3"); lookTimer = 0f; }

        }
        else
        {
            lookTimer = 0f;
            awayTimer += Time.deltaTime;

            totalLookTime = Mathf.Max(0f, totalLookTime - Time.deltaTime * 0.5f); // shrink slowly

            if (currentPhase == 3 && awayTimer >= phase3to2Time) { SetPhase(2, "Shrink → Phase 2"); awayTimer = 0f; }
            else if (currentPhase == 2 && awayTimer >= phase2to1Time) { SetPhase(1, "Shrink → Phase 1"); awayTimer = 0f; }
        }

        if (currentPhase == 3)
        {
            timeUntilFade -= Time.deltaTime;
            if (timeUntilFade <= 0 && fadeController != null)
                fadeController.StartFade();
        }


        float totalPhaseTime = phase1to2Time + phase2to3Time;
        float growthProgress = Mathf.Clamp01(totalLookTime / totalPhaseTime);
        fireController.SetGrowthProgress(growthProgress);



        if (drawDebugRay) DrawDebugRay(looking);
        UpdateInfoUI();

    }


    bool IsLookingAtFire()
    {
        if (!playerCamera) return false;

        Ray centerRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!gazeCollider)
        {
            if (!fireRoot) return false;
            Vector3 vp = playerCamera.WorldToViewportPoint(fireRoot.position);
            if (vp.z < 0f) return false;
            Vector2 delta = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            lastAnchorWorld = fireRoot.position;
            lastAnchorViewport = new Vector2(vp.x, vp.y);
            lastAnchorAngleDeg = Vector3.Angle(playerCamera.transform.forward, (fireRoot.position - playerCamera.transform.position).normalized);
            return DeadzonePass(delta, fireRoot.position);
        }

        RaycastHit hit;
        bool hitOk = useSphereCast
            ? Physics.SphereCast(centerRay, Mathf.Max(0.0001f, sphereCastRadius), out hit, maxRayDistance, hitMask, QueryTriggerInteraction.Collide)
            : Physics.Raycast(centerRay, out hit, maxRayDistance, hitMask, QueryTriggerInteraction.Collide);

        bool hitFire = hitOk && hit.collider && IsSameOrChild(hit.collider, gazeCollider);

        if (!hitFire && requirePhysicsHit)
        {
            if (debugLog) Debug.Log("[Gaze] No physics hit on fire → gaze OFF");
            return false;
        }

        Vector3 anchorWorld = hitFire
            ? hit.point
            : ComputeBottomBiasedAnchor(gazeCollider, anchorBiasFromBottom);

        lastAnchorWorld = anchorWorld;

        Vector3 anchorVp3 = playerCamera.WorldToViewportPoint(anchorWorld);
        lastAnchorViewport = new Vector2(anchorVp3.x, anchorVp3.y);
        Vector2 deltaVp = lastAnchorViewport - new Vector2(0.5f, 0.5f);

        Vector3 anchorSp3 = playerCamera.WorldToScreenPoint(anchorWorld);
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 deltaPx = new Vector2(anchorSp3.x, anchorSp3.y) - screenCenter;

        lastAnchorAngleDeg = Vector3.Angle(playerCamera.transform.forward, (anchorWorld - playerCamera.transform.position).normalized);

        switch (deadzoneMode)
        {
            case DeadzoneMode.ViewportRadius: return deltaVp.magnitude <= deadZoneRadius;
            case DeadzoneMode.PixelRadius: return deltaPx.magnitude <= deadZonePixels;
            case DeadzoneMode.AngularDegrees: return lastAnchorAngleDeg <= deadZoneDegrees;
        }
        return false;
    }

    bool DeadzonePass(Vector2 deltaViewport, Vector3 anchorWorld)
    {
        Vector3 toAnchor = (anchorWorld - playerCamera.transform.position).normalized;
        lastAnchorAngleDeg = Vector3.Angle(playerCamera.transform.forward, toAnchor);
        switch (deadzoneMode)
        {
            case DeadzoneMode.ViewportRadius: return deltaViewport.magnitude <= deadZoneRadius;
            case DeadzoneMode.PixelRadius:
                Vector3 sp = playerCamera.WorldToScreenPoint(anchorWorld);
                Vector2 sc = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                return (new Vector2(sp.x, sp.y) - sc).magnitude <= deadZonePixels;
            case DeadzoneMode.AngularDegrees: return lastAnchorAngleDeg <= deadZoneDegrees;
        }
        return false;
    }


    void DrawDebugRay(bool hit)
    {
        Color c = hit ? Color.green : Color.red;
        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * maxRayDistance, c);
    }


    void SetPhase(int phase, string msg)
    {
        int p = Mathf.Clamp(phase, 1, 3);
        if (p == currentPhase) return;
        currentPhase = p;
        if (fireObjectController) fireObjectController.SetStageByNumber(currentPhase);
        if (messageText)
        {
            messageText.text = msg;
            CancelInvoke(nameof(ClearMessage));
            Invoke(nameof(ClearMessage), 2f);
        }
        if (phaseText && showPhase) phaseText.text = $"Phase {currentPhase}";
    }

    void ForcePhase(int phase, string msg)
    {
        currentPhase = Mathf.Clamp(phase, 1, 3);
        if (fireObjectController) fireObjectController.SetStageByNumber(currentPhase);
        lookTimer = 0f;
        awayTimer = 0f;
        if (messageText)
        {
            messageText.text = msg;
            CancelInvoke(nameof(ClearMessage));
            Invoke(nameof(ClearMessage), 2f);
        }
        if (phaseText && showPhase) phaseText.text = $"Phase {currentPhase}";
    }

    void ClearMessage() { if (messageText) messageText.text = ""; }

    void UpdateInfoUI()
    {
        if (timerText && showTimer)
        {
            if (gazeActive) timerText.text = $"Looking: {lookTimer:0.0}s";
            else timerText.text = $"Away: {awayTimer:0.0}s";
        }
        if (phaseText && showPhase) phaseText.text = $"Phase {currentPhase}";
    }

    private void PlayRandomFireSound()
    {
        if (fireClips == null || fireClips.Length == 0) return;
        AudioClip randomClip = fireClips[Random.Range(0, fireClips.Length)];
        SoundFXManager.instance.PlaySound(randomClip, gameObject.transform, 0.7f);
    }


    static bool IsSameOrChild(Collider hit, Collider targetRoot)
    {
        if (hit == targetRoot) return true;
        if (!hit || !targetRoot) return false;
        Transform t = hit.transform, root = targetRoot.transform;
        while (t != null) { if (t == root) return true; t = t.parent; }
        return false;
    }

    static Vector3 ComputeBottomBiasedAnchor(Collider col, float biasFromBottom)
    {
        if (!col) return Vector3.zero;
        Bounds b = col.bounds;
        float y = Mathf.Lerp(b.min.y, b.max.y, Mathf.Clamp01(biasFromBottom));
        Vector3 bottomCenter = new Vector3(b.center.x, y, b.center.z);

        Vector3 probe = bottomCenter + Vector3.up * 0.01f;
        Vector3 closest = col.ClosestPoint(probe);
        if ((closest - probe).sqrMagnitude < 1e-6f) return bottomCenter;
        return closest;
    }
}
