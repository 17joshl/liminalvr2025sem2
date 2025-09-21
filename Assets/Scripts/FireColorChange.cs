using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireColorChange : MonoBehaviour
{
    public Color lookAtColor = Color.red;
    private Color originalColor;
    private ParticleSystem.MainModule mainModule;


    public float growRate = 0.1f;
    public float scaleCount = 0.1f;

    public Vector3 startingScale = new Vector3(0.1f, 0.1f, 0.1f);
    public Vector3 targetScale = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 finalScale = new Vector3(1f, 1f, 1f);


    public float duration = 2f; 
    public float lossDuration = 30f;


    public float noiseTarget = 2f; 


    public ParticleSystem smallFire;  // Phase 2



    void Start()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();

        if (ps != null)
        {
            mainModule = ps.main;
            originalColor = mainModule.startColor.color;
        }

    }

    void Update() {
        if(transform.localScale == targetScale) {

            smallFire.gameObject.SetActive(true);

            var emission = smallFire.emission;
            emission.enabled = true;

            smallFire.Play(true);
        } else {
            smallFire.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

    }

    public void OnLookAt()
    {
        //mainModule.startColor = lookAtColor;

                StartCoroutine(growOverTime(targetScale, duration));

            
    }


    IEnumerator growOverTime(Vector3 endScale, float time)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0;

            while (elapsed < time)
            {
                transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / time);
                //scaleCount += growRate;

                Debug.Log(scaleCount);


                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = endScale; 
        }




    public void OnLookAway()
    {
        mainModule.startColor = originalColor;


            StartCoroutine(growOverTime(startingScale, lossDuration));

    }



}