using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tumbleweed : MonoBehaviour
{
    public float rollForce = 5f;
    public float torqueForce = 10f;

    void OnEnable()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.AddForce(transform.forward * rollForce, ForceMode.VelocityChange);
        rb.AddTorque(Vector3.up * torqueForce, ForceMode.VelocityChange);
    }
}
