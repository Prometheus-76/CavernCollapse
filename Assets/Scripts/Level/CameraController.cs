using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Vector3 offset;
    public float followStrength;
    public float horizontalRestZone;
    public Transform targetTransform;

    private Transform camTransform;

    // Start is called before the first frame update
    void Start()
    {
        camTransform = transform;
    }

    public void SetStartPosition(Vector3 startPos)
    {
        camTransform.position = startPos + offset;
    }

    // Update is called once per frame
    void Update()
    {
        // Follow the player smoothly, and allow some movement on the x without immediately following
        Vector3 fromPosition = camTransform.position;
        Vector3 toPosition = targetTransform.position + offset;
        toPosition.x = Mathf.Clamp(fromPosition.x, toPosition.x - horizontalRestZone, toPosition.x + horizontalRestZone);
        Vector3 newPosition = Vector3.Lerp(fromPosition, toPosition, followStrength * Time.deltaTime);
        camTransform.position = newPosition;
    }
}
