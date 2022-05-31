using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Responsible for saving and loading settings in all scenes

public class SettingsManager : MonoBehaviour
{
    [Header("Settings")]
    public GameSettings gameSettings;
    public AudioMixer audioMixer;
    public bool loadSettingsOnStart;

    void Start()
    {
        if (loadSettingsOnStart)
        {
            LoadSettings();
            ApplySettings();
        }
    }

    public void ApplySettings()
    {
        // Sound
        float scaledMusic = (gameSettings.musicVolume > 0) ? Mathf.Log10(Mathf.Clamp(gameSettings.musicVolume / 10f, 0.001f, 1f)) * 40f : -80f;
        audioMixer.SetFloat("MusicVolume", scaledMusic);

        float scaledSound = (gameSettings.soundVolume > 0) ? Mathf.Log10(Mathf.Clamp(gameSettings.soundVolume / 10f, 0.001f, 1f)) * 20f : -80f;
        audioMixer.SetFloat("SoundVolume", scaledSound);

        // FPS Limit
        Application.targetFrameRate = gameSettings.targetFPS;
    }

    public void LoadSettings()
    {
        gameSettings.musicVolume = PlayerPrefs.GetInt("MusicVolume", 8);
        gameSettings.soundVolume = PlayerPrefs.GetInt("SoundVolume", 8);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("MusicVolume", gameSettings.musicVolume);
        PlayerPrefs.SetInt("SoundVolume", gameSettings.soundVolume);
    }
}
