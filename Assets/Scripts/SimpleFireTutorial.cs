using UnityEngine;
using UnityEngine.UI;

public class SimpleFireTutorial : MonoBehaviour
{
    [Header("Tutorial UI")]
    public Text tutorialText;
    public float displayTime = 15f;
    public bool showOnStart = true;
    
    [Header("VR Instructions")]
    public bool showVRInstructions = true;
    
    void Start()
    {
        if (showOnStart && tutorialText != null)
        {
            ShowTutorial();
        }
    }
    
    public void ShowTutorial()
    {
        string tutorialMessage = BuildTutorialMessage();
        
        if (tutorialText != null)
        {
            tutorialText.text = tutorialMessage;
            tutorialText.gameObject.SetActive(true);
            
            // Hide after specified time
            if (displayTime > 0)
            {
                Invoke(nameof(HideTutorial), displayTime);
            }
        }
        
        // Also log to console for debugging
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
    
    // Call this method to show tutorial again
    public void RestartTutorial()
    {
        ShowTutorial();
    }
    
    void Update()
    {
        // VR controllers or hand tracking could trigger these
        // Remove keyboard controls for pure VR experience
    }
}