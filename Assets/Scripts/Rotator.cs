using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    public float rotationSpeed = 50f; // Vitesse de rotation
    public float bounceMagnitude = 0.2f; // Amplitude du rebond
    public float bounceSpeed = 3f; // Vitesse du rebond

    private float startY; // Position Y initiale

    void Start()
    {
        // Enregistrer la position Y initiale
        startY = transform.position.y;
    }

    void Update()
    {
        // Rotation de l'objet
        transform.Rotate(new Vector3(0, rotationSpeed, 0) * Time.deltaTime);

        // Rebondissement
        float bounceOffset = Mathf.Sin(Time.time * bounceSpeed) * bounceMagnitude;
        Vector3 newPosition = transform.position;
        newPosition.y = startY + bounceOffset;
        transform.position = newPosition;
    }
}
