using UnityEngine;
using UnityEngine.UI;

public class VRFireTutorial : MonoBehaviour
{
    [Header("Tutorial UI")]
    public Text tutorialText;
    public Canvas tutorialCanvas;
    public float displayTime = 15f;
    public bool showOnStart = true;
    
    [Header("VR Setup")]
    public Camera vrCamera;
    public bool autoSetupCanvas = true;
    
    [Header("Canvas Positioning")]
    [Tooltip("Choose how the tutorial should appear")]
    public CanvasDisplayMode displayMode = CanvasDisplayMode.FollowCamera;
    
    [Header("Follow Camera Settings (Billboard Style)")]
    [Tooltip("Distance in front of camera")]
    public float distanceFromCamera = 2f;
    [Tooltip("Height offset from camera center")]
    public float heightOffset = 0f;
    
    [Header("World Space Settings (Fixed Position)")]
    [Tooltip("World position for fixed canvas")]
    public Vector3 worldPosition = new Vector3(0f, 1.5f, 2f);
    [Tooltip("World rotation for fixed canvas")]
    public Vector3 worldRotation = new Vector3(0f, 0f, 0f);
    
    [Header("Canvas Scale")]
    public float canvasScale = 0.003f;
    
    [Header("VR Instructions")]
    public bool showVRInstructions = true;
    
    public enum CanvasDisplayMode
    {
        FollowCamera,  // Follows player's head (billboard style)
        WorldSpace     // Fixed position in world
    }
    
    void Start()
    {
        // Find VR camera if not assigned
        if (vrCamera == null)
        {
            vrCamera = Camera.main;
            if (vrCamera == null)
            {
                Debug.LogError("VRFireTutorial: No VR camera found! Please assign it in the inspector.");
                return;
            }
        }
        
        // Auto-setup canvas for VR if enabled
        if (autoSetupCanvas && tutorialCanvas != null)
        {
            SetupCanvasForVR();
        }
        
        if (showOnStart && tutorialText != null)
        {
            ShowTutorial();
        }
    }
    
    void Update()
    {
        // If following camera, update position and rotation each frame
        if (displayMode == CanvasDisplayMode.FollowCamera && tutorialCanvas != null && vrCamera != null)
        {
            UpdateCanvasPosition();
        }
    }
    
    void SetupCanvasForVR()
    {
        // Set render mode to World Space
        tutorialCanvas.renderMode = RenderMode.WorldSpace;
        
        // Set initial scale
        tutorialCanvas.transform.localScale = Vector3.one * canvasScale;
        
        if (displayMode == CanvasDisplayMode.FollowCamera)
        {
            // Parent to camera for follow mode
            tutorialCanvas.transform.SetParent(vrCamera.transform, false);
            tutorialCanvas.transform.localPosition = new Vector3(0f, heightOffset, distanceFromCamera);
            tutorialCanvas.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Set world position for fixed mode
            tutorialCanvas.transform.SetParent(null);
            tutorialCanvas.transform.position = worldPosition;
            tutorialCanvas.transform.eulerAngles = worldRotation;
        }
        
        Debug.Log($"VRFireTutorial: Canvas setup complete in {displayMode} mode");
    }
    
    void UpdateCanvasPosition()
    {
        // Keep canvas in front of camera at specified distance
        Vector3 targetPosition = vrCamera.transform.position + 
                                vrCamera.transform.forward * distanceFromCamera +
                                vrCamera.transform.up * heightOffset;
        
        tutorialCanvas.transform.position = targetPosition;
        
        // Make canvas face the camera
        tutorialCanvas.transform.LookAt(vrCamera.transform);
        tutorialCanvas.transform.Rotate(0f, 180f, 0f); // Flip to face camera
    }
    
    public void ShowTutorial()
    {
        string tutorialMessage = BuildTutorialMessage();
        
        if (tutorialText != null)
        {
            tutorialText.text = tutorialMessage;
            
            // Make sure canvas is active
            if (tutorialCanvas != null)
            {
                tutorialCanvas.gameObject.SetActive(true);
            }
            
            tutorialText.gameObject.SetActive(true);
            
            // Hide after specified time
            if (displayTime > 0)
            {
                CancelInvoke(nameof(HideTutorial)); // Cancel any existing invoke
                Invoke(nameof(HideTutorial), displayTime);
            }
        }
        
        Debug.Log("VRFireTutorial: " + tutorialMessage);
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
        
        if (tutorialCanvas != null && displayMode == CanvasDisplayMode.FollowCamera)
        {
            // For follow camera mode, keep canvas active but hide text
            // For world space, you might want to hide the whole canvas
            // tutorialCanvas.gameObject.SetActive(false);
        }
    }
    
    public void RestartTutorial()
    {
        ShowTutorial();
    }
    
    // Public method to change display mode at runtime
    public void SetDisplayMode(CanvasDisplayMode mode)
    {
        displayMode = mode;
        if (tutorialCanvas != null)
        {
            SetupCanvasForVR();
        }
    }
    
    // Helper method to adjust canvas distance
    public void SetCanvasDistance(float distance)
    {
        distanceFromCamera = distance;
    }
}