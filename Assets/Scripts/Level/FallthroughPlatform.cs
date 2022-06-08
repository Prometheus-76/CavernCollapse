using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallthroughPlatform : MonoBehaviour
{
    public PlatformEffector2D platformEffector;
    public LayerMask defaultLayers;
    public LayerMask defaultWithoutPlayer;
    public float toggleOffDuration;

    // Start is called before the first frame update
    void Start()
    {
        platformEffector.colliderMask = defaultLayers;
    }

    public IEnumerator FlipOnOff()
    {
        // Allow player to fall through
        platformEffector.colliderMask = defaultWithoutPlayer;

        // Wait for a little bit
        yield return new WaitForSecondsRealtime(toggleOffDuration);

        // Stop the player from falling through again
        platformEffector.colliderMask = defaultLayers;

        yield return null;
    }
}
