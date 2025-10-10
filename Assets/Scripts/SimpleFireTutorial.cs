using UnityEngine;
using UnityEngine.UI;

public class SimpleFireTutorial : MonoBehaviour
{
    [Header("Tutorial UI")]
    public Text tutorialText;
    public float displayTime = 8f;
    public bool showOnStart = true;

    [Header("VR Instructions")]
    public bool showVRInstructions = true;

    void Awake()
    {
        EnsureCanvasAndText();
    }

    void Start()
    {
        if (showOnStart && tutorialText != null)
        {
            ShowTutorial();
        }
    }

    void EnsureCanvasAndText()
    {
        if (tutorialText && tutorialText.gameObject.activeInHierarchy)
        {
            EnableCanvasParents(tutorialText.transform);
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (!canvas)
        {
            GameObject cgo = new GameObject("RuntimeCanvas");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }
        else
        {
            if (!canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        }

        if (!tutorialText)
        {
            Text[] texts = canvas.GetComponentsInChildren<Text>(true);
            foreach (var t in texts)
            {
                string n = t.name.ToLower();
                if (n.Contains("tutorial"))
                {
                    tutorialText = t;
                    break;
                }
            }
        }

        if (!tutorialText)
        {
            GameObject tgo = new GameObject("TutorialText");
            tgo.transform.SetParent(canvas.transform, false);
            tutorialText = tgo.AddComponent<Text>();
            tutorialText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tutorialText.color = Color.white;
            tutorialText.raycastTarget = false;
            tutorialText.alignment = TextAnchor.UpperCenter;
            tutorialText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tutorialText.verticalOverflow = VerticalWrapMode.Overflow;
            tutorialText.resizeTextForBestFit = true;
            tutorialText.resizeTextMinSize = 14;
            tutorialText.resizeTextMaxSize = 36;
            RectTransform rt = (RectTransform)tgo.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -60f);
            rt.sizeDelta = new Vector2(1000f, 500f);
        }

        EnableCanvasParents(tutorialText.transform);

        if (tutorialText.GetComponentInParent<Canvas>().renderMode == RenderMode.WorldSpace)
        {
            tutorialText.GetComponentInParent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }
    }

    void EnableCanvasParents(Transform t)
    {
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    public void ShowTutorial()
    {
        string tutorialMessage = BuildTutorialMessage();
        if (tutorialText != null)
        {
            tutorialText.text = tutorialMessage;
            tutorialText.gameObject.SetActive(true);
            if (displayTime > 0) Invoke(nameof(HideTutorial), displayTime);
        }
        Debug.Log(tutorialMessage);
    }

    string BuildTutorialMessage()
    {
        string message = "FOCUS FIRE\n\n";
        message += "HOW IT WORKS:\n";
        message += "• Look directly at the fire to make it GROW\n";
        message += "• Look away from the fire to make it SHRINK\n";
        message += "• The longer you stare, the bigger it gets!\n\n";
        if (showVRInstructions)
        {
            message += "VR EXPERIENCE:\n";
            message += "• Turn your head to look around\n";
            message += "• Move closer or step back as needed\n";
            message += "• Focus your gaze on the flames\n\n";
        }
        message += "Try it now - find the fire and stare at it!";
        return message;
    }

    public void HideTutorial()
    {
        if (tutorialText != null)
        {
            tutorialText.gameObject.SetActive(false);
        }
    }

    public void RestartTutorial()
    {
        ShowTutorial();
    }
}
