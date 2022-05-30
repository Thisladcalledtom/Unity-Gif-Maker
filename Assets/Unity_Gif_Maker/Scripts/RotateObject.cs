using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour
{

    public float speed;
    float amountRotated = 0;

    void Update()
    {
        float rotationThisFrame = speed * Time.deltaTime;
        amountRotated += rotationThisFrame;
        transform.rotation = Quaternion.Euler(transform.rotation.x, -amountRotated, transform.rotation.z);

        if (amountRotated >= 360) UnityEditor.EditorApplication.isPlaying = false;
    }
}
