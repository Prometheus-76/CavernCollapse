using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject optionsMenu;
    public GameObject playMenu;
    public GameObject loadingMenu;
    public GameObject backgroundObjects;

    public MenuButton musicButton;
    public MenuButton soundButton;
    public MenuButton resetButton;

    public MenuButton modeButton;
    public MenuButton difficultyButton;

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
            float progress = Mathf.Clamp01(resetTimer / 3f);

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
            resetButton.SetDefaultText("resetting: " + (Mathf.Clamp01(resetTimer / 3f) * 100f).ToString("F0") + "%");
        else
            resetButton.SetDefaultText("reset save file");
    }

    // Closes the game, why must I do this Unity...
    public void CloseGame()
    {
        Application.Quit();
    }

    /// <summary>
    /// Changes the menu state
    /// </summary>
    /// <param name="menuIndex">0 = main, 1 = options, 2 = play, 3 = loading</param>
    public void SetMenuState(int menuIndex)
    {
        switch (menuIndex)
        {
            case 0:
                mainMenu.SetActive(true);
                optionsMenu.SetActive(false);
                playMenu.SetActive(false);
                loadingMenu.SetActive(false);
                backgroundObjects.SetActive(true);
                break;
            case 1:
                mainMenu.SetActive(false);
                optionsMenu.SetActive(true);
                playMenu.SetActive(false);
                loadingMenu.SetActive(false);
                backgroundObjects.SetActive(true);
                break;
            case 2:
                mainMenu.SetActive(false);
                optionsMenu.SetActive(false);
                playMenu.SetActive(true);
                loadingMenu.SetActive(false);
                backgroundObjects.SetActive(true);
                break;
            case 3:
                mainMenu.SetActive(false);
                optionsMenu.SetActive(false);
                playMenu.SetActive(false);
                loadingMenu.SetActive(true);
                backgroundObjects.SetActive(false);
                break;
        }

        UpdateSettingsUI();
    }

    void LoadSettings()
    {
        musicVolume = PlayerPrefs.GetInt("MusicVolume", 10);
        soundVolume = PlayerPrefs.GetInt("SoundVolume", 10);

        UpdateSettingsUI();
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

        UpdateSettingsUI();
        SaveSettings();
    }

    void UpdateSettingsUI()
    {
        musicButton.SetDefaultText("music volume: " + musicVolume.ToString());
        soundButton.SetDefaultText("sound volume: " + soundVolume.ToString());
    }

    public void SetResetState(bool state)
    {
        isResetting = state;
    }
}
