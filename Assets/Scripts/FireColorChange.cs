using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireColorChange : MonoBehaviour
{
    public Color lookAtColor = Color.red;
    private Color originalColor;
    private ParticleSystem.MainModule mainModule;

    void Start()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            mainModule = ps.main;
            originalColor = mainModule.startColor.color;
        }
    }

    public void OnLookAt()
    {
        mainModule.startColor = lookAtColor;
    }

    public void OnLookAway()
    {
        mainModule.startColor = originalColor;
    }
}