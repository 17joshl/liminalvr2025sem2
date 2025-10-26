using UnityEngine;
using UnityEngine.UI;

public class FireGazePhases : MonoBehaviour
{
    [Header("Look Target & Camera")]
    public Transform fireRoot;
    public Camera playerCamera;
    public LayerMask hitMask = ~0;
    public float maxRayDistance = 50f;

    [Header("Screen-Space Dead Zone")]
    [Range(0.01f, 0.35f)] public float deadZoneRadius = 0.2f;

    [Header("Gaze Volume")]
    public Collider gazeCollider;
    public float sphereCastRadius = 0.5f;

    [Header("Phase Timings (Seconds)")]
    public float phase1to2Time = 30f;
    public float phase2to3Time = 30f;
    public float phase3to2Time = 30f;
    public float phase2to1Time = 30f;

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
    public bool drawDebugRay = true;

    int currentPhase = 1;
    float lookTimer = 0f;
    float awayTimer = 0f;

    void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;
        if (!fireController) fireController = FindObjectOfType<FireSizeChanger>();
        if (!fireRoot && fireController) fireRoot = fireController.transform;
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

        if (looking)
        {
            awayTimer = 0f;
            lookTimer += Time.deltaTime;

            if (currentPhase == 1 && lookTimer >= phase1to2Time) { SetPhase(2, "Phase 2"); lookTimer = 0f; }
            else if (currentPhase == 2 && lookTimer >= phase2to3Time) { SetPhase(3, "Phase 3"); lookTimer = 0f; }
        }
        else
        {
            lookTimer = 0f;
            awayTimer += Time.deltaTime;

            if (currentPhase == 3 && awayTimer >= phase3to2Time) { SetPhase(2, "Shrink → Phase 2"); awayTimer = 0f; }
            else if (currentPhase == 2 && awayTimer >= phase2to1Time) { SetPhase(1, "Shrink → Phase 1"); awayTimer = 0f; }
        }

        if (drawDebugRay) DrawDebugRay(looking);
        UpdateInfoUI();
    }

    bool IsLookingAtFire()
    {
        if (!playerCamera || !fireRoot) return false;

        Vector3 vp = playerCamera.WorldToViewportPoint(fireRoot.position);
        if (vp.z < 0f) return false;
        float dx = vp.x - 0.5f;
        float dy = vp.y - 0.5f;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        if (dist > deadZoneRadius) return false;

        if (!gazeCollider) return true;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, maxRayDistance, hitMask, QueryTriggerInteraction.Collide))
        {
            if (hit.collider == gazeCollider || hit.collider.transform.IsChildOf(gazeCollider.transform)) return true;
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
        fireController.SetStageByNumber(currentPhase);
        if (messageText)
        {
            messageText.text = msg;
            CancelInvoke(nameof(ClearMessage));
            Invoke(nameof(ClearMessage), 2f);
        }
    }

    void ForcePhase(int phase, string msg)
    {
        currentPhase = Mathf.Clamp(phase, 1, 3);
        fireController.SetStageByNumber(currentPhase);
        lookTimer = 0f;
        awayTimer = 0f;
        if (messageText)
        {
            messageText.text = msg;
            CancelInvoke(nameof(ClearMessage));
            Invoke(nameof(ClearMessage), 2f);
        }
    }

    void ClearMessage() { if (messageText) messageText.text = ""; }

    void UpdateInfoUI()
    {
        if (timerText)
        {
            timerText.gameObject.SetActive(showTimer);
            if (showTimer)
            {
                float t = Mathf.Max(lookTimer, awayTimer);
                timerText.alignment = TextAnchor.UpperRight;
                timerText.text = $"Timer: {t:0.0}s";
            }
        }

        if (phaseText)
        {
            phaseText.gameObject.SetActive(showPhase);
            if (showPhase)
            {
                phaseText.alignment = TextAnchor.UpperRight;
                phaseText.text = $"Phase: {currentPhase}";
            }
        }
    }
}
