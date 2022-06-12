using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Darcy Matheson 2022

// Keeps an audio source alive while playing
public class KeepWhilePlaying : MonoBehaviour
{
    public AudioSource audioSource;
    public bool canBeDestroyed = false;

    // Start is called before the first frame update
    void Awake()
    {
        transform.parent = null;
        DontDestroyOnLoad(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (audioSource.isPlaying == false && canBeDestroyed) Destroy(gameObject);
    }
}
