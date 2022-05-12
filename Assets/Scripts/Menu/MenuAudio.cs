using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource buttonAudio;

    [Header("Audio Clips")]
    public AudioClip positiveClip;
    public AudioClip negativeClip;
    public AudioClip toggleClip;
    public AudioClip playClip;

    public enum MenuSounds
    {
        Positive,
        Negative,
        Toggle,
        Play
    }

    public void PlaySound(MenuSounds sound)
    {
        // Get the clip
        AudioClip clip = EnumToClip(sound);

        // Play it if not null
        if (clip != null) buttonAudio.PlayOneShot(clip);
    }

    AudioClip EnumToClip(MenuSounds sound)
    {
        switch (sound)
        {
            case MenuSounds.Positive:
                return positiveClip;
            case MenuSounds.Negative:
                return negativeClip;
            case MenuSounds.Toggle:
                return toggleClip;
            case MenuSounds.Play:
                return playClip;
            default:
                return null;
        }
    }
}
