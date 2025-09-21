using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GazeMechanic : MonoBehaviour
{
    public Camera vrCamera;
    private FireColorChange currentTarget;

    void Start()
    {
        if (vrCamera == null)
            vrCamera = Camera.main;
    }

    void Update()
    {
        Ray ray = new Ray(vrCamera.transform.position, vrCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            FireColorChange target = hit.collider.GetComponent<FireColorChange>();

            if (target != null)
            {
                if (currentTarget != target)
                {
                    if (currentTarget != null)
                        currentTarget.OnLookAway();

                    currentTarget = target;
                    currentTarget.OnLookAt();
                }
                return;
            }
        }

        if (currentTarget != null) {
            
            currentTarget.OnLookAway();
            currentTarget = null;
        }
    }
}
