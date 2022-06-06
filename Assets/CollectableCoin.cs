using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableCoin : MonoBehaviour
{
    public float xScale;
    public float floatRate;
    public float floatAmount;

    private float offset;
    private Transform coinTransform;
    private Vector3 startPosition;
    private bool isCollected;

    // Start is called before the first frame update
    void Start()
    {
        coinTransform = GetComponent<Transform>();
        startPosition = coinTransform.position;
        isCollected = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isCollected == false)
        {
            // When the coin hasn't been collected yet, make it bob up and down
            offset = transform.position.x * xScale;
            coinTransform.position = startPosition + (Mathf.Sin((Time.time * floatRate) + offset) * floatAmount * Vector3.up);
        }
    }

    public void CollectCoin()
    {
        isCollected = true;
    }
}
