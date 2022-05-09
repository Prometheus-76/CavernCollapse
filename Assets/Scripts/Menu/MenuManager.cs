using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject optionsMenu;
    public GameObject playMenu;
    public GameObject backgroundObjects;

    public MenuButton musicButton;
    public MenuButton soundButton;
    public MenuButton resetButton;

    public MenuButton modeButton;
    public MenuButton difficultyButton;

    public AudioSource buttonAudio;
    public AudioSource resetAudio;
    public AudioMixer audioMixer;

    private int musicVolume;
    private int soundVolume;

    private float resetTimer;
    private bool isResetting;

    void Start()
    {
        LoadSettings();
        resetTimer = 0f;
        isResetting = false;
    }

    private void LateUpdate()
    {
        if (isResetting)
        {
            resetTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(resetTimer / 2f);

            if (progress >= 1f)
            {
                isResetting = false;
                // Delete the player save file
            }
        }
        else
        {
            resetTimer = 0f;
        }

        // Update resetting UI
        if (isResetting)
        {
            resetButton.SetDefaultText("resetting: " + (Mathf.Clamp01(resetTimer / 2f) * 100f).ToString("F0") + "%");
            resetAudio.Play();
        }
        else
        {
            resetButton.SetDefaultText("reset save file");
            resetAudio.Stop();
        }
    }

    // Closes the game, why must I do this Unity...
    public void CloseGame()
    {
        Application.Quit();
    }

    /// <summary>
    /// Changes the menu state
    /// </summary>
    /// <param name="menuIndex">0 = main, 1 = options, 2 = play</param>
    public void SetMenuState(int menuIndex)
    {
        switch (menuIndex)
        {
            case 0:
                mainMenu.SetActive(true);
                optionsMenu.SetActive(false);
                playMenu.SetActive(false);
                backgroundObjects.SetActive(true);
                break;
            case 1:
                mainMenu.SetActive(false);
                optionsMenu.SetActive(true);
                playMenu.SetActive(false);
                backgroundObjects.SetActive(true);
                break;
            case 2:
                mainMenu.SetActive(false);
                optionsMenu.SetActive(false);
                playMenu.SetActive(true);
                backgroundObjects.SetActive(true);
                break;
        }

        UpdateSettings();
    }

    /// <summary>
    /// Loads a new scene to transition to
    /// </summary>
    /// <param name="sceneIndex">The build index of the scene we are transitioning to</param>
    public void SceneTransition(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    void LoadSettings()
    {
        musicVolume = PlayerPrefs.GetInt("MusicVolume", 5);
        soundVolume = PlayerPrefs.GetInt("SoundVolume", 5);

        UpdateSettings();
    }

    void SaveSettings()
    {
        PlayerPrefs.SetInt("MusicVolume", musicVolume);
        PlayerPrefs.SetInt("SoundVolume", soundVolume);
    }

    /// <summary>
    /// Increases the sound index until 10, then resets to 0
    /// </summary>
    /// <param name="settingIndex">0 = music, 1 = sound</param>
    public void IncrementSound(int settingIndex)
    {
        switch(settingIndex)
        {
            case 0:
                musicVolume = ((musicVolume + 1) % 11);
                break;
            case 1:
                soundVolume = ((soundVolume + 1) % 11);
                break;
        }

        UpdateSettings();
        SaveSettings();
    }

    void UpdateSettings()
    {
        // UI
        musicButton.SetDefaultText("music volume: " + musicVolume.ToString());
        soundButton.SetDefaultText("sound volume: " + soundVolume.ToString());

        // Sound
        float scaledMusic = (musicVolume > 0) ? Mathf.Log10(Mathf.Clamp(musicVolume / 10f, 0.001f, 1f)) * 20f : -80f;
        audioMixer.SetFloat("MusicVolume", scaledMusic);

        float scaledSound = (soundVolume > 0) ? Mathf.Log10(Mathf.Clamp(soundVolume / 10f, 0.001f, 1f)) * 20f : -80f;
        audioMixer.SetFloat("SoundVolume", scaledSound);
    }

    public void SetResetState(bool state)
    {
        isResetting = state;
    }

    public void PlayButtonSound(AudioClip clip)
    {
        buttonAudio.PlayOneShot(clip);
    }

    /// <summary>
    /// Loads a new scene, given a build index
    /// </summary>
    /// <param name="sceneIndex">0 = menu, 1 = level, 2 = editor</param>
    public void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
