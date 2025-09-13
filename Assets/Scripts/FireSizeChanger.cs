using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class FireSizeChanger : MonoBehaviour
{
    [Header("Particle Systems")]
    public ParticleSystem smallFire;
    public ParticleSystem bigFlames;
    public ParticleSystem fireball;

    [Header("Lights (optional)")]
    public Light[] lights;

    [Header("Global Intensity [0..1]")]
    [Range(0f, 1f)] public float intensity = 0.35f;

    [Header("Auto Mode")]
    public bool autoMode = true;
    [Range(0.05f, 2.0f)] public float autoSpeed = 0.35f;
    [Range(0f, 1f)] public float autoMin = 0.20f;
    [Range(0f, 1f)] public float autoMax = 0.85f;
    [Range(0.5f, 10f)] public float response = 3.0f;

    [Header("Manual Buttons Step")]
    [Range(0.01f, 0.25f)] public float manualStep = 0.05f;

    [Header("Optional UI")]
    public Toggle autoToggle;
    public Slider intensitySlider;
    public Button growButton;
    public Button shrinkButton;

   
    float smallSize = 0.6f, smallLife = 4f, smallRate = 60f;
    float bigSize = 1.5f, bigLife = 5.5f, bigRate = 2f;
    float ballSize = 1.75f, ballLife = 0.85f, ballRate = 6f;

    float _seed;

    void Awake()
    {
        _seed = Random.value * 10f;

        if (autoToggle) autoToggle.onValueChanged.AddListener(v => autoMode = v);
        if (intensitySlider) intensitySlider.onValueChanged.AddListener(v => { intensity = v; autoMode = false; });
        if (growButton) growButton.onClick.AddListener(() => AdjustIntensity(manualStep));
        if (shrinkButton) shrinkButton.onClick.AddListener(() => AdjustIntensity(-manualStep));
    }

    void Update()
    {
        
        if (autoMode)
        {
            float target = Mathf.Lerp(autoMin, autoMax, Mathf.PerlinNoise(Time.time * autoSpeed, _seed));
            intensity = Mathf.Lerp(intensity, target, 1f - Mathf.Exp(-response * Time.deltaTime));
            if (intensitySlider) intensitySlider.SetValueWithoutNotify(intensity);
        }

        intensity = Mathf.Clamp01(intensity);

        
        ApplyPS(smallFire, smallSize, smallLife, smallRate, intensity);
        ApplyPS(bigFlames, bigSize, bigLife, bigRate, Mathf.Clamp01(intensity * 1.2f));
        ApplyPS(fireball, ballSize, ballLife, ballRate, Mathf.Clamp01(intensity * 1.5f));

        
        if (lights != null)
        {
            foreach (var l in lights)
                if (l) l.intensity = Mathf.Lerp(0.5f, 2f, intensity);
        }
    }

    void ApplyPS(ParticleSystem ps, float baseSize, float baseLife, float baseRate, float factor)
    {
        if (!ps) return;

        ps.gameObject.SetActive(factor > 0.01f);

        var main = ps.main;
        main.startSize = baseSize * Mathf.Lerp(0.5f, 1.5f, factor);
        main.startLifetime = baseLife * Mathf.Lerp(0.7f, 1.3f, factor);

        var emission = ps.emission;
        emission.rateOverTime = baseRate * Mathf.Lerp(0.5f, 1.5f, factor);
    }

    void AdjustIntensity(float delta)
    {
        autoMode = false;
        if (autoToggle) autoToggle.isOn = false;
        intensity = Mathf.Clamp01(intensity + delta);
        if (intensitySlider) intensitySlider.SetValueWithoutNotify(intensity);
    }
}