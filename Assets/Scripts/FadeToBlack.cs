using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeToBlack : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 2f;

    void Awake() //ensures image starts at 0 opacity, so everything's visible
    {
        if (fadeImage)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    public void StartFade()
    {
        if (fadeImage)
        {
            StartCoroutine(FadeRoutine());
        }
    }

    IEnumerator FadeRoutine() //fades the image out over an adjustable time (right now, 2 seconds)
    {
        float timer = 0f;
        Color c = fadeImage.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;

        Application.Quit();
    }
}
