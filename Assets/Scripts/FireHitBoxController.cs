using UnityEngine;
using System.Linq;
using System.Reflection;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class FireHitBoxController : MonoBehaviour
{
    [Header("Auto-Discovery (no assignment needed)")]
    public Transform campfireBase;
    public Transform supportingBonfire; 
    public Transform mainFire;          
    public Transform redFlames;         

    [Header("Sizing Mode")]
    public bool useManualPerPhaseHeights = true;
    public bool smoothResize = true;
    [Range(0.01f, 0.6f)] public float smoothTime = 0.15f;

    [Header("Manual Per-Phase Heights (m)")]
    public float p1Height = 0.9f;
    public float p2Height = 1.4f;
    public float p3Height = 2.0f;

    [Header("Auto From Scale (if manual off)")]
    public float p1ScaleToHeight = 1.0f;
    public float p2ScaleToHeight = 1.5f;
    public float p3ScaleToHeight = 2.0f;

    [Header("Bounds & Offsets")]
    public float minHeight = 1.0f;
    public float maxHeight = 4.0f;
    public float extraHeightOffset = 0.0f;

    [Header("Width Lock (kept constant)")]
    public bool lockWidthFromInitial = true;
    public float boxWidthX = 1.0f;
    public float boxWidthZ = 1.0f;

    [Header("Debug")]
    public bool debugLog = false;

    BoxCollider box;
    float yVel;

    void Awake()
    {
        box = GetComponent<BoxCollider>();
        AutoGrab();
        InitWidth();
        ApplyHeight(true);
    }

    void OnValidate()
    {
        if (!box) box = GetComponent<BoxCollider>();
        if (lockWidthFromInitial) InitWidth();
        ApplyHeight(true);
    }

    void Update()
    {
        ApplyHeight(false);
    }

    void AutoGrab()
    {
        if (!campfireBase)
        {
            var baseByName = FindByNames("CampfireBase", "campfireBase", "Base", "Campfire");
            campfireBase = baseByName ? baseByName : transform.root;
        }

        var fsc = FindObjectOfType<FireSizeChanger>();
        if (fsc)
        {
            if (!supportingBonfire) supportingBonfire = fsc.supportingBonfire;
            if (!mainFire) mainFire = fsc.mainFire;
            if (!redFlames) redFlames = fsc.redFlames;
        }

        if (!supportingBonfire) supportingBonfire = FindByNames("supportingBonfire", "SupportingBonfire", "Phase1", "P1");
        if (!mainFire) mainFire = FindByNames("mainFire", "MainFire", "Phase2", "P2");
        if (!redFlames) redFlames = FindByNames("redFlames", "RedFlames", "Phase3", "P3");

        if ((!lockWidthFromInitial || ApproxZero(boxWidthX) || ApproxZero(boxWidthZ)) && campfireBase)
        {
            var rAct = campfireBase.GetComponentsInChildren<Renderer>(true).FirstOrDefault();
            if (rAct)
            {
                var sz = rAct.bounds.size;
                if (ApproxZero(boxWidthX)) boxWidthX = Mathf.Max(0.1f, sz.x);
                if (ApproxZero(boxWidthZ)) boxWidthZ = Mathf.Max(0.1f, sz.z);
            }
        }
    }

    void InitWidth()
    {
        var s = box.size;
        s.x = Mathf.Max(0.01f, boxWidthX);
        s.z = Mathf.Max(0.01f, boxWidthZ);
        box.size = s;

        var c = box.center;
        c.y = Mathf.Max(0f, box.size.y * 0.5f);
        box.center = c;
    }

    void ApplyHeight(bool immediate)
    {
        float target = Mathf.Clamp(GetTargetHeightMeters() + extraHeightOffset, minHeight, maxHeight);
        float newH = immediate || !smoothResize ? target : Mathf.SmoothDamp(box.size.y, target, ref yVel, smoothTime);

        var s = box.size;
        s.y = newH;
        if (lockWidthFromInitial)
        {
            s.x = boxWidthX;
            s.z = boxWidthZ;
        }
        box.size = s;

        var c = box.center;
        c.y = Mathf.Max(0f, newH * 0.5f);
        box.center = c;

        
        Physics.SyncTransforms();
    }

    float GetTargetHeightMeters()
    {
        int phase = DetectPhaseRobust();
        if (useManualPerPhaseHeights)
        {
            if (phase == 3) return p3Height;
            if (phase == 2) return p2Height;
            return p1Height;
        }
        else
        {
            if (phase == 3) return FromScale(redFlames, p3ScaleToHeight);
            if (phase == 2) return FromScale(mainFire, p2ScaleToHeight);
            return FromScale(supportingBonfire, p1ScaleToHeight);
        }
    }

    int DetectPhaseRobust()
    {
        var fsc = FindObjectOfType<FireSizeChanger>();
        int fromFsc = TryGetPhaseFromScript(fsc);
        if (fromFsc >= 1 && fromFsc <= 3)
        {
            if (debugLog) Debug.Log($"[FireHitBoxController] Phase from FireSizeChanger: {fromFsc}");
            return fromFsc;
        }

        float s1 = supportingBonfire ? supportingBonfire.localScale.y : 0f;
        float s2 = mainFire ? mainFire.localScale.y : 0f;
        float s3 = redFlames ? redFlames.localScale.y : 0f;

        bool p3flag = redFlames && (redFlames.gameObject.activeInHierarchy || s3 > 0.05f);
        bool p2flag = mainFire && (mainFire.gameObject.activeInHierarchy || s2 > Mathf.Max(0.05f, s1 * 0.8f));

        if (p3flag) return 3;
        if (p2flag) return 2;
        return 1;
    }

    int TryGetPhaseFromScript(object script)
    {
        if (script == null) return 0;
        string[] names = { "phase", "currentPhase", "activePhase" };

        var type = script.GetType();
        foreach (var n in names)
        {
            var f = type.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(int))
            {
                int v = (int)f.GetValue(script);
                if (v >= 1 && v <= 3) return v;
            }
            var p = type.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(int) && p.CanRead)
            {
                int v = (int)p.GetValue(script, null);
                if (v >= 1 && v <= 3) return v;
            }
        }
        return 0;
    }

    float FromScale(Transform t, float mult)
    {
        if (!t) return minHeight;
        return Mathf.Max(minHeight, t.localScale.y * mult);
    }

    static bool ApproxZero(float v) { return Mathf.Abs(v) < 1e-3f; }

    Transform FindByNames(params string[] names)
    {
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go) return go.transform;
        }

        var active = FindObjectsOfType<Transform>();
        foreach (var t in active)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (t.name.ToLower().Contains(names[i].ToLower())) return t;
            }
        }

        var all = Resources.FindObjectsOfTypeAll(typeof(Transform)) as Transform[];
        if (all != null)
        {
            foreach (var t in all)
            {
                if (!t) continue;
                if (t.hideFlags != HideFlags.None) continue;
                for (int i = 0; i < names.Length; i++)
                {
                    if (t.name.ToLower().Contains(names[i].ToLower())) return t;
                }
            }
        }
        return null;
    }
}
