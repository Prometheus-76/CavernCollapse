using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Following")]
    public Vector3 offset;
    public float followStrength;
    public float horizontalRestZone;
    private Transform targetTransform;

    [Header("Screenshake")]
    public float screenshakeMagnitude;
    public float screenshakeFrequency;
    public float traumaDrainTime;

    private Transform camTransform;
    private Transform holderTransform;
    private float screenshakeTrauma;
    private Vector2 shakeDirection;

    // Start is called before the first frame update
    void Start()
    {
        camTransform = Camera.main.transform;
        holderTransform = transform;
    }

    public void SetStartPosition(Vector3 startPos)
    {
        holderTransform.position = startPos + offset;
    }

    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }

    // Update is called once per frame
    void Update()
    {
        if (targetTransform != null)
        {
            // Follow the player smoothly, and allow some movement on the x without immediately following
            Vector3 fromPosition = holderTransform.position;
            Vector3 toPosition = targetTransform.position + offset;
            toPosition.x = Mathf.Clamp(fromPosition.x, toPosition.x - horizontalRestZone, toPosition.x + horizontalRestZone);
            Vector3 newPosition = Vector3.Lerp(fromPosition, toPosition, followStrength * Time.deltaTime);

            // Manage trauma level
            screenshakeTrauma -= (Time.deltaTime / traumaDrainTime);
            screenshakeTrauma = Mathf.Max(screenshakeTrauma, 0f);

            // Calculate offset strength due to trauma
            Vector3 screenshakeOffset = Vector3.zero;
            float scaledTime = Time.time * screenshakeFrequency;
            if (shakeDirection != Vector2.zero)
            {
                // Directional shake
                screenshakeOffset = Mathf.PerlinNoise(scaledTime + 1f, scaledTime + 1f) * Mathf.Clamp01(screenshakeTrauma * screenshakeTrauma) * shakeDirection;
            }
            else
            {
                // Random noise shake
                screenshakeOffset.x = Mathf.PerlinNoise(scaledTime + 1f, scaledTime + 1f) * Mathf.Clamp01(screenshakeTrauma * screenshakeTrauma);
                screenshakeOffset.y = Mathf.PerlinNoise(scaledTime + 2f, scaledTime + 2f) * Mathf.Clamp01(screenshakeTrauma * screenshakeTrauma);
            }

            // Apply new positions
            holderTransform.position = newPosition;
            camTransform.localPosition = screenshakeOffset;
        }
    }

    public void AddTrauma(float amount)
    {
        screenshakeTrauma += amount;
        screenshakeTrauma = Mathf.Clamp01(screenshakeTrauma);
    }

    public void SetShakeDirection(Vector2 direction)
    {
        shakeDirection = direction;
    }
}
