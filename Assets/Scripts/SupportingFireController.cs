using UnityEngine;

public class SupportingFireController : MonoBehaviour
{
    public ParticleSystem particles;
    public float fadeSpeed = 3f;
    public float maxEmission = 40f;   

    private float targetEmission = 0f;
    private float currentEmission = 0f;

    private bool disableAfterFade = false;

    void Awake()
    {
        if (!particles) particles = GetComponent<ParticleSystem>();
        currentEmission = 0f;
    }

    void Update()
    {
        currentEmission = Mathf.Lerp(currentEmission, targetEmission, Time.deltaTime * fadeSpeed);

        var emission = particles.emission;
        emission.rateOverTime = currentEmission;

        if (disableAfterFade && currentEmission <= 0.05f && particles.particleCount == 0)
        {
            gameObject.SetActive(false);
            disableAfterFade = false;
        }
    }

    public void FadeIn()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        disableAfterFade = false;
        targetEmission = maxEmission;
    }

    public void FadeOut()
    {
        targetEmission = 0f;
        disableAfterFade = true;
    }
}
