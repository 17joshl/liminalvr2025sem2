using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSize : MonoBehaviour
{
    private ParticleSystem.MainModule mainModule;

    public float growRate = 0.1f;

    public Vector3 targetScale = new Vector3(0.5f, 0.5f, 0.5f);
    public float duration = 2f; 



    void Start() {
        ParticleSystem ps = GetComponent<ParticleSystem>();

    }

    public void OnLookAt() {

            StartCoroutine(growOverTime(targetScale, duration));
    }


    IEnumerator growOverTime(Vector3 endScale, float time) {
            Vector3 startScale = transform.localScale;
            float elapsed = 0;

            while (elapsed < time)
            {
                transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / time);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = endScale; // Ensure the final scale is precisely set
        }




    public void OnLookAway() {
    }
}