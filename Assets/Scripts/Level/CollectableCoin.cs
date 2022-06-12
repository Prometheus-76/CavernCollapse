using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Darcy Matheson

// Controls an individual gem, and it's collection and magnetism towards the player
public class CollectableCoin : MonoBehaviour
{
    public float xScale;
    public float floatRate;
    public float floatAmount;

    public float magnetismRange;
    public float collectionRange;
    public LayerMask playerLayer;
    public LayerMask solidLayer;
    public float magnetismSpeed;

    private float offset;
    private Transform coinTransform;
    private Vector3 startPosition;
    private bool isAttracted;
    private bool isCollected;

    private Transform playerTransform;

    // Start is called before the first frame update
    void Start()
    {
        coinTransform = GetComponent<Transform>();

        startPosition = coinTransform.position;
        isAttracted = false;
        isCollected = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isCollected == false)
        {
            if (isAttracted == false)
            {
                // When the coin hasn't been collected yet, make it bob up and down
                offset = transform.position.x * xScale;
                coinTransform.position = startPosition + (Mathf.Sin((Time.time * floatRate) + offset) * floatAmount * Vector3.up);

                // The coin is yet to be collected, so check if the player is within magnetism range of it
                if (Physics2D.OverlapCircle(coinTransform.position, magnetismRange, playerLayer))
                {
                    // Get reference to the player transform, we can't do this in Start() because the coins are generated before the player
                    if (playerTransform == null) playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

                    // Check there is no wall separating the coin from the player
                    if (Physics2D.Linecast(coinTransform.position, playerTransform.position + (Vector3.up * 0.2f), solidLayer) == false)
                    {
                        isAttracted = true;
                    }
                }
            }
            else
            {
                // The coin has now been attracted to the player, lerp to their position
                Vector3 from = coinTransform.position;
                Vector3 to = playerTransform.position;
                Vector3 lerped = Vector3.Lerp(from, to, magnetismSpeed * Time.deltaTime);

                coinTransform.position = lerped;

                // The coin is within pickup range of the player, so delete it and record the collection
                if (Vector3.Distance(lerped, to) <= collectionRange)
                {
                    // Obviously this is a less than ideal solution but it's only happening once every few frames when coins are collected, so it shouldn't be noticable
                    LevelManager levelManager = GameObject.FindGameObjectWithTag("Level").GetComponent<LevelManager>();
                    levelManager.CollectCoin();

                    Destroy(gameObject);
                }
            }
        }
    }
}
