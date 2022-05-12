using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MenuMusicPlayer : MonoBehaviour
{
    public AudioClip[] songs;
    public AudioSource audioSource;
    public float fadeOutTime;

    // Cheeky little singleton-ish thing
    private static MenuMusicPlayer instance;

    private List<AudioClip> songQueue;
    private AudioClip previousSong;
    private float fadeOutTimer;
    private bool fadingOut;

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

        DontDestroyOnLoad(this);
        songQueue = new List<AudioClip>();
        fadingOut = false;
    }

    // Update is called once per frame
    void Update()
    {
        // Fade out
        if (fadeOutTimer > 0f && fadingOut)
        {
            fadeOutTimer = Mathf.Clamp(fadeOutTimer - Time.deltaTime, 0f, fadeOutTime);

            // Lower volume over time
            audioSource.volume = Mathf.Clamp01(fadeOutTimer / fadeOutTime);

            if (fadeOutTimer <= 0f)
            {
                // Timer has ended
                instance = null;
                Destroy(gameObject);
            }
        }

        // Song queue
        if (songQueue.Count <= 0)
        {
            // Add all songs to a bag
            List<AudioClip> remainingSongs = new List<AudioClip>();
            for (int i = 0; i < songs.Length; i++)
            {
                remainingSongs.Add(songs[i]);
            }

            // Bag randomise songs, taking them out as we go
            while (remainingSongs.Count > 0)
            {
                int choice = Random.Range(0, remainingSongs.Count);
                songQueue.Add(remainingSongs[choice]);
                remainingSongs.RemoveAt(choice);
            }

            // If a song would have been played twice in a row
            if (previousSong == songQueue[0])
            {
                songQueue.Reverse();
            }
        }

        if (audioSource.isPlaying == false)
        {
            previousSong = audioSource.clip;
            audioSource.clip = songQueue[0];
            songQueue.RemoveAt(0);
            audioSource.Play();
        }
    }

    public void FadeOut()
    {
        fadingOut = true;
        fadeOutTimer = fadeOutTime;
    }
}
