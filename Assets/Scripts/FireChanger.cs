using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FireChanger : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem fireParticleSystem;
    public ParticleSystem glowParticleSystem;
    public Light fireLight; 

    [Header("Fire Stages")]
    public FireStage smallFire;   // Phase 1 start
    public FireStage mediumFire;  // Phase 2 start
    public FireStage largeFire;   // Phase 3 start


    [Header("Glow Feedback Settings")]
    [Tooltip("How strong the glow becomes when looked at (multiplier for emission).")]
    public float glowIntensityMultiplier = 3f;
    [Tooltip("How quickly the glow fades in/out.")]
    public float glowSmoothSpeed = 3f;


    [Header("Progress Split")]
    [Tooltip("Overall progress split between Small->Medium and Medium->Large. " +
             "Example: phase1to2Time / (phase1to2Time + phase2to3Time).")]
    [Range(0.05f, 0.95f)] public float split = 0.5f;

    [Header("Smoothing / Feel")]
    [Tooltip("How fast we blend toward the target each frame.")]
    public float smoothSpeed = 5f;

    [Tooltip("Optional curve to remap overall progress (0..1). Leave null for linear.")]
    public AnimationCurve growthCurve;

    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;

    private ParticleSystem.EmissionModule glowEmission;
    private ParticleSystem.MainModule glowMain;

    private float targetGlowRate;
    private float currentGlowRate;

    private float _currentSize;
    private float _currentLifetime;
    private float _currentEmission;
    private float _currentScale;
    private float _currentLight;

    [System.Serializable]
    public struct FireStage
    {
        [Header("Particle Main")]
        public float startSize;
        public float lifetime;

        [Header("Emission")]
        public float emissionRate;

        [Header("Transform / Light")]
        public float scale;
        public float lightIntensity;
    }

    private void Awake()
    {
        if (!fireParticleSystem) fireParticleSystem = GetComponent<ParticleSystem>();
        mainModule = fireParticleSystem.main;
        emissionModule = fireParticleSystem.emission;

        ApplyInstant(smallFire);

        if (glowParticleSystem)
        {
            glowEmission = glowParticleSystem.emission;
            glowMain = glowParticleSystem.main;
        }
    }


    public void SetGrowthProgress(float overallT)
    {
        overallT = Mathf.Clamp01(overallT);

        if (growthCurve != null && growthCurve.keys != null && growthCurve.length > 0)
            overallT = Mathf.Clamp01(growthCurve.Evaluate(overallT));


        float segT, size, lifetime, emission, scale, light;

        if (overallT <= split)
        {
            float localT = Mathf.InverseLerp(0f, split, overallT); 
            size     = Mathf.Lerp(smallFire.startSize,   mediumFire.startSize,   localT);
            lifetime = Mathf.Lerp(smallFire.lifetime,    mediumFire.lifetime,    localT);
            emission = Mathf.Lerp(smallFire.emissionRate,mediumFire.emissionRate,localT);
            scale    = Mathf.Lerp(smallFire.scale,       mediumFire.scale,       localT);
            light    = Mathf.Lerp(smallFire.lightIntensity, mediumFire.lightIntensity, localT);
        }
        else
        {
            float localT = Mathf.InverseLerp(split, 1f, overallT); 
            size     = Mathf.Lerp(mediumFire.startSize,   largeFire.startSize,   localT);
            lifetime = Mathf.Lerp(mediumFire.lifetime,    largeFire.lifetime,    localT);
            emission = Mathf.Lerp(mediumFire.emissionRate,largeFire.emissionRate,localT);
            scale    = Mathf.Lerp(mediumFire.scale,       largeFire.scale,       localT);
            light    = Mathf.Lerp(mediumFire.lightIntensity, largeFire.lightIntensity, localT);
        }

        float s = Time.deltaTime * Mathf.Max(0.01f, smoothSpeed);

        _currentSize     = Mathf.Lerp(mainModule.startSize.constant,        size,     s);
        _currentLifetime = Mathf.Lerp(mainModule.startLifetime.constant,    lifetime, s);
        _currentEmission = Mathf.Lerp(emissionModule.rateOverTime.constant, emission, s);
        _currentScale    = Mathf.Lerp(transform.localScale.x,               scale,    s); 
        if (fireLight)
            _currentLight = Mathf.Lerp(fireLight.intensity,                 light,    s);

        mainModule.startSize     = _currentSize;
        mainModule.startLifetime = _currentLifetime;
        emissionModule.rateOverTime = _currentEmission;
        transform.localScale = Vector3.one * _currentScale;
        if (fireLight) fireLight.intensity = _currentLight;
    }


    public void SetGlowActive(bool isLooking)
    {
        if (!glowParticleSystem) return;

        targetGlowRate = isLooking ? glowMain.startSize.constant * glowIntensityMultiplier : 0f;
    }


    public void SnapToStage(int phase) // 1=Small, 2=Medium, 3=Large
    {
        switch (Mathf.Clamp(phase, 1, 3))
        {
            case 1: ApplyInstant(smallFire); break;
            case 2: ApplyInstant(mediumFire); break;
            case 3: ApplyInstant(largeFire); break;
        }
    }


    public void SetSplit(float split01) => split = Mathf.Clamp(split01, 0.05f, 0.95f);

    // --- internals ---
    private void ApplyInstant(FireStage s)
    {
        _currentSize     = s.startSize;
        _currentLifetime = s.lifetime;
        _currentEmission = s.emissionRate;
        _currentScale    = s.scale;
        _currentLight    = s.lightIntensity;

        mainModule.startSize     = _currentSize;
        mainModule.startLifetime = _currentLifetime;
        emissionModule.rateOverTime = _currentEmission;
        transform.localScale = Vector3.one * _currentScale;

        if (fireLight) fireLight.intensity = _currentLight;
    }



        private void Update()
    {
        if (glowParticleSystem)
        {
            currentGlowRate = Mathf.Lerp(currentGlowRate, targetGlowRate, Time.deltaTime * glowSmoothSpeed);
            var emission = glowParticleSystem.emission;
            emission.rateOverTime = currentGlowRate;

            var main = glowParticleSystem.main;
            Color c = main.startColor.color;
            c.a = Mathf.Lerp(c.a, isGlowActive() ? 1f : 0f, Time.deltaTime * glowSmoothSpeed);
            main.startColor = c;
        }
    }

    private bool isGlowActive()
    {
        return targetGlowRate > 0.01f;
    }


}
