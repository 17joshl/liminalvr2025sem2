using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeColorChange : MonoBehaviour
{
 public Color baseColor = Color.white;           // Default cube color
    public Color glowColor = Color.yellow;          // Glow color when looked at
    public float glowIntensity = 3f;                // Brightness multiplier for glow

    private Material cubeMaterial;
    private Color targetColor;
    private Color currentColor;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Important: use instance material so we don’t change the shared material
            cubeMaterial = renderer.material;
            currentColor = cubeMaterial.color;
            targetColor = baseColor;
        }
    }

    void Update()
    {
        // Smoothly lerp color each frame
        if (cubeMaterial != null)
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * 5f);
            cubeMaterial.color = currentColor;
        }
    }

    public void OnLookAt()
    {
        targetColor = glowColor * glowIntensity; // HDR color (brightens with bloom)
    }

    public void OnLookAway()
    {
        targetColor = baseColor;
    }
}