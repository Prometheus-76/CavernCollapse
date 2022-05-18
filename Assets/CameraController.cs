using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Vector3 offset;
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
        
    }
}
