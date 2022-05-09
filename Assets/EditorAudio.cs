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

    [Header("Audio Settings")]
    [SerializeField, Range(0f, 1f), Tooltip("How intensely the audio pans from left to right when placing and deleting")] private float stereoIntensity;

    public enum OneshotSounds
    {
        Positive,
        Negative,
        Toggle,
        Scratch,
        Delete,
        Place,
        Descending
    }

    // Set the looping audio to start or stop playing
    public void SetLooping(bool playing)
    {
        if (playing) loopingAudio.Play();
        if (playing == false) loopingAudio.Stop();
    }

    public void PlayOneshot(OneshotSounds sound)
    {
        // Get the clip
        AudioClip clip = EnumToClip(sound);

        // Play it if not null
        if (clip != null) oneshotAudio.PlayOneShot(clip);
    }

    public void PlayOneshotStereo(OneshotSounds sound, float stereo)
    {
        // Get the clip
        AudioClip clip = EnumToClip(sound);

        // Set the stereo setting
        stereoAudio.panStereo = stereo * stereoIntensity;

        // Play it if not null
        if (clip != null) stereoAudio.PlayOneShot(clip);
    }

    // Translates from the enum of sounds to actual sound clip
    public AudioClip EnumToClip(OneshotSounds sound)
    {
        switch (sound)
        {
            case OneshotSounds.Positive:
                return positiveClip;
            case OneshotSounds.Negative:
                return negativeClip;
            case OneshotSounds.Toggle:
                return toggleClip;
            case OneshotSounds.Scratch:
                return scratchClip;
            case OneshotSounds.Delete:
                return deleteClip;
            case OneshotSounds.Place:
                return placeClip;
            case OneshotSounds.Descending:
                return descendingClip;
            default:
                return null;
        }
    }
}
