using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimpleFireTutorial : MonoBehaviour
{
    [Header("Tutorial UI")]
    [SerializeField] private Text tutorialText;

    [Header("Timing")]
    [SerializeField] private float initialDelay = 3f; //delay before first fade-in
    [SerializeField] private float fadeDuration = 2f; //fade in/out duration
    [SerializeField] private float textDuration = 5f; //time text is fully visible
    [SerializeField] private float delayBetweenLines = 0.5f;

    [Header("Text Lines")]
    [TextArea(2, 4)]
    public string[] lines = new string[]
    {
        "The cool desert night brushes against your back...",
        "In front of you, a small kindling. Stare directly at the warmth to infuse it with the power to grow.",
        "Don't look away for an instant, or the night will steal your flame's energy away from you."
    };

    void Awake()
    {
        if (tutorialText == null)
        {
            Debug.LogError("SimpleFireTutorial: No Text component assigned in Inspector.");
            enabled = false;
            return;
        }

        Color c = tutorialText.color;
        c.a = 0f;
        tutorialText.color = c;
        tutorialText.text = "";
        //starts text empty/clear
    }

    void Start()
    {
        StartCoroutine(FadeSequence());
    }

    IEnumerator FadeSequence()
    {
        yield return new WaitForSeconds(initialDelay);

        foreach (string line in lines)
        {
            tutorialText.text = line;
            yield return StartCoroutine(FadeText(0f, 1f, fadeDuration)); //fade in
            yield return new WaitForSeconds(textDuration); //text stays until fade out begins
            yield return StartCoroutine(FadeText(1f, 0f, fadeDuration)); //fade out
            yield return new WaitForSeconds(delayBetweenLines); //waits a bit before the next line fades in
        }
    }

    IEnumerator FadeText(float start, float end, float duration)
    {
        float t = 0f;
        Color c = tutorialText.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(start, end, t / duration);
            tutorialText.color = c;
            yield return null;
        }

        c.a = end;
        tutorialText.color = c;
    }
}