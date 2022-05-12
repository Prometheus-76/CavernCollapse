using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MusicPlayer : MonoBehaviour
{
    public AudioClip[] songs;
    public AudioSource audioSourceA;
    public AudioSource audioSourceB;
    public float crossfadeTime;

    private float sourceDominance;
    private bool prioritiseA;

    // Cheeky little singleton-ish thing
    private static MusicPlayer instance;

    void Awake()
    {
        // Ensure there is only music player at a time
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        sourceDominance = 0f;
        prioritiseA = true;
        DontDestroyOnLoad(this);
    }

    void Start()
    {
        // Fade in main menu theme and make it loop
        CrossFade(0);
        SetLooping(true);
    }

    // Update is called once per frame
    void Update()
    {
        // In transition (crossfade)
        if ((prioritiseA && sourceDominance > 0f) || (prioritiseA == false && sourceDominance < 1f))
        {
            sourceDominance += (Time.deltaTime / crossfadeTime) * (prioritiseA ? -1f : 1f);
            sourceDominance = Mathf.Clamp01(sourceDominance);
        }

        audioSourceA.volume = 1f - sourceDominance;
        audioSourceB.volume = sourceDominance;
    }

    public void CrossFade(int newSong)
    {
        // Flip to other audio source
        prioritiseA = !prioritiseA;

        // Set new song
        if (prioritiseA)
        {
            audioSourceA.clip = songs[newSong];
            audioSourceA.Play();
        }
        else
        {
            audioSourceB.clip = songs[newSong];
            audioSourceB.Play();
        }
    }

    public void SetLooping(bool state)
    {
        // Set audio source to looping
        if (prioritiseA)
        {
            audioSourceA.loop = state;
        }
        else
        {
            audioSourceB.loop = state;
        }
    }
}
