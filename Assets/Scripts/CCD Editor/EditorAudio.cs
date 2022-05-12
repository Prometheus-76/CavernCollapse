using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource musicAudio;
    public AudioSource oneshotAudio;
    public AudioSource stereoAudio;
    public AudioSource loopingAudio;

    [Header("Audio Clips")]
    public AudioClip positiveClip;
    public AudioClip negativeClip;
    public AudioClip toggleClip;
    public AudioClip scratchClip;
    public AudioClip deleteClip;
    public AudioClip placeClip;
    public AudioClip descendingClip;
    public AudioClip deniedClip;

    [Header("Audio Settings")]
    [SerializeField, Range(0f, 1f), Tooltip("How intensely the audio pans from left to right when placing and deleting")] private float stereoIntensity;

    public enum EditorSounds
    {
        Positive,
        Negative,
        Toggle,
        Scratch,
        Delete,
        Place,
        Descending,
        Denied
    }

    // Set the looping audio to start or stop playing
    public void SetLooping(bool playing)
    {
        if (playing) loopingAudio.Play();
        if (playing == false) loopingAudio.Stop();
    }

    public void PlayOneshot(EditorSounds sound)
    {
        // Get the clip
        AudioClip clip = EnumToClip(sound);

        // Play it if not null
        if (clip != null) oneshotAudio.PlayOneShot(clip);
    }

    public void PlayOneshotStereo(EditorSounds sound, float stereo)
    {
        // Get the clip
        AudioClip clip = EnumToClip(sound);

        // Set the stereo setting
        stereoAudio.panStereo = stereo * stereoIntensity;

        // Play it if not null
        if (clip != null) stereoAudio.PlayOneShot(clip);
    }

    // Translates from the enum of sounds to actual sound clip
    public AudioClip EnumToClip(EditorSounds sound)
    {
        switch (sound)
        {
            case EditorSounds.Positive:
                return positiveClip;
            case EditorSounds.Negative:
                return negativeClip;
            case EditorSounds.Toggle:
                return toggleClip;
            case EditorSounds.Scratch:
                return scratchClip;
            case EditorSounds.Delete:
                return deleteClip;
            case EditorSounds.Place:
                return placeClip;
            case EditorSounds.Descending:
                return descendingClip;
            case EditorSounds.Denied:
                return deniedClip;
            default:
                return null;
        }
    }
}
