using UnityEngine;

public class FireColorChange : MonoBehaviour
{
    [Header("Color Settings")]
    public Color lookAtColor = Color.red;
    private Color originalColor;

    public Transform fireVisual;

    [Header("Scale Settings")]
    public Vector3 startingScale = new Vector3(0.1f, 0.1f, 0.1f);
    public Vector3 midScale = new Vector3(0.5f, 0.5f, 0.5f);   // Halfway
    public Vector3 finalScale = new Vector3(1f, 1f, 1f);       // Full

    [Tooltip("How fast the fire grows when looked at")]
    public float growSpeed = 2f;

    [Tooltip("How fast the fire shrinks when looked away from")]
    public float shrinkSpeed = 2f;

    [Header("Particles")]
    public ParticleSystem smallFire;  
    public ParticleSystem largeFire;  

    private bool isLookedAt = false;
    private bool phase2Active = false;
    private bool phase3Active = false;   
    private ParticleSystem mainParticles;

    void Start()
    {
        fireVisual.localScale = startingScale;

        mainParticles = GetComponent<ParticleSystem>();
        if (mainParticles != null)
        {
            var main = mainParticles.main;
            originalColor = main.startColor.color;
        }

        if (smallFire != null)
        {
            smallFire.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            smallFire.gameObject.SetActive(false);
        }

        if (largeFire != null)
        {
            largeFire.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            largeFire.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        Vector3 target = isLookedAt ? finalScale : startingScale;
        float speed = isLookedAt ? growSpeed : shrinkSpeed;


        fireVisual.localScale = Vector3.MoveTowards(
            fireVisual.localScale,
            target,
            speed * Time.deltaTime
        );

 
        float currentSize = fireVisual.localScale.x; 

        // ---- Small fire (midpoint) ----
        if (!phase2Active && currentSize >= midScale.x)
        {
            ActivateSmallFire();
            phase2Active = true;
        }
        else if (phase2Active && currentSize < midScale.x)
        {
            DeactivateSmallFire();
            phase2Active = false;
        }

        // ---- Large fire (near full scale) ----
        if (!phase3Active && currentSize >= finalScale.x * 0.80f) 
        {
            ActivateLargeFire();
            phase3Active = true;
        }
        else if (phase3Active && currentSize < finalScale.x * 0.80f)
        {
            DeactivateLargeFire();
            phase3Active = false;
        }
    }

    public void OnLookAt()
    {
        isLookedAt = true;
        if (mainParticles != null)
        {
            var main = mainParticles.main; 
            main.startColor = lookAtColor;
        }
    }

    public void OnLookAway()
    {
        isLookedAt = false;
        if (mainParticles != null)
        {
            var main = mainParticles.main; 
            main.startColor = originalColor;
        }
    }

    private void ActivateSmallFire()
    {
        if (smallFire != null)
        {
            smallFire.gameObject.SetActive(true);
            var emission = smallFire.emission;
            emission.enabled = true;
            smallFire.Play(true);
        }
    }

    private void DeactivateSmallFire()
    {
        if (smallFire != null)
        {
            smallFire.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            smallFire.gameObject.SetActive(false);
        }
    }

    private void ActivateLargeFire()
    {
        if (largeFire != null)
        {
            largeFire.gameObject.SetActive(true);
            var emission = largeFire.emission;
            emission.enabled = true;
            largeFire.Play(true);
        }
    }

    private void DeactivateLargeFire()
    {
        if (largeFire != null)
        {
            largeFire.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            largeFire.gameObject.SetActive(false);
        }
    }
}
