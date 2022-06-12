using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Darcy Matheson 2022

// Manages the main cameras "fly upwards" transition into gameplay
public class MenuCamera : MonoBehaviour
{
    [Header("Parameters")]
    public float flyTime;
    public float flyHeight;

    [Header("Components")]
    public MenuManager menuManager;

    // PRIVATE
    private Transform camTransform;
    private float flyTimer;
    private bool isFlying;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    void Start()
    {
        camTransform = transform;
        startPosition = camTransform.position;
        isFlying = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isFlying && flyTimer < flyTime)
        {
            // Animate flying upwards
            flyTimer = Mathf.Clamp(flyTimer + Time.deltaTime, 0f, flyTime);
            float progress = Mathf.Clamp01(flyTimer / flyTime);
            float smoothProgress = progress * progress;

            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, smoothProgress);
            camTransform.position = newPosition;

            if (progress >= 1f)
            {
                // The animation has finished
                menuManager.LoadGame();
            }
        }
    }

    public void FlyUpwards()
    {
        targetPosition = startPosition + (Vector3.up * flyHeight);
        isFlying = true;
    }
}
