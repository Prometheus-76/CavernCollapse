using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

// Manages the UI and screen transitions of the main menu
// Also coordinates calling settings related functions in the SettingsManager for the main menu

public class MenuManager : MonoBehaviour
{
    [Header("Menu Screens")]
    public GameObject mainMenu;
    public GameObject optionsMenu;
    public GameObject playMenu;

    [Header("Components")]
    // Options menu
    public MenuButton musicButton;
    public MenuButton soundButton;
    public MenuButton postProcessButton;

    // Play menu
    public MenuButton styleButton;
    public MenuButton difficultyButton;

    [Header("Settings")]
    public GameSettings gameSettings;
    public SettingsManager settingsManager;

    [Header("Audio")]
    public MenuAudio menuAudio;
    public KeepWhilePlaying buttonAudioSource;

    [Header("Camera")]
    public MenuCamera menuCamera;

    [Header("Gameplay")]
    public GameplayConfiguration gameplayConfiguration;
    public AttemptStats currentAttempt;
    private int datasetStyle;

    void Start()
    {
        // Load the settings and update the UI to reflect the changes
        settingsManager.LoadSettings();
        settingsManager.ApplySettings();
        UpdateSettingsUI();

        // Default gameplay config
        datasetStyle = 0;
        UpdateGameplayConfiguration();
    }

    #region Menu Navigation

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
                menuAudio.PlaySound(MenuAudio.MenuSounds.Negative);
                break;
            case 1:
                mainMenu.SetActive(false);
                optionsMenu.SetActive(true);
                playMenu.SetActive(false);
                menuAudio.PlaySound(MenuAudio.MenuSounds.Positive);
                break;
            case 2:
                mainMenu.SetActive(false);
                optionsMenu.SetActive(false);
                playMenu.SetActive(true);
                menuAudio.PlaySound(MenuAudio.MenuSounds.Positive);
                break;
        }

        UpdateSettingsUI();
    }

    // Closes the game, why must I do this Unity...
    public void CloseGame()
    {
        menuAudio.PlaySound(MenuAudio.MenuSounds.Negative);
        Application.Quit();
    }

    #endregion  

    #region Scene Navigation

    public void LoadEditor()
    {
        menuAudio.PlaySound(MenuAudio.MenuSounds.Positive);
        buttonAudioSource.canBeDestroyed = true;
        SceneManager.LoadScene(1);
    }

    public void StartGameTransition()
    {
        menuAudio.PlaySound(MenuAudio.MenuSounds.Play);
        MusicPlayer.GetInstance().CrossFade(0);
        MusicPlayer.GetInstance().SetLooping(true);

        menuCamera.FlyUpwards();

        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playMenu.SetActive(false);
    }

    public void LoadGame()
    {
        currentAttempt.FreshStart();
        SceneManager.LoadScene(2);
    }

    #endregion

    #region Modifying Settings

    /// <summary>
    /// Increases the sound index until 10, then resets to 0
    /// </summary>
    /// <param name="settingIndex">0 = music, 1 = sound</param>
    public void IncrementSound(int settingIndex)
    {
        switch(settingIndex)
        {
            case 0:
                gameSettings.musicVolume = ((gameSettings.musicVolume + 1) % 11);
                break;
            case 1:
                gameSettings.soundVolume = ((gameSettings.soundVolume + 1) % 11);
                break;
        }

        menuAudio.PlaySound(MenuAudio.MenuSounds.Toggle);
        UpdateSettingsUI();
        settingsManager.ApplySettings();
        settingsManager.SaveSettings();
    }

    void UpdateSettingsUI()
    {
        musicButton.SetDefaultText("music volume: " + gameSettings.musicVolume.ToString());
        soundButton.SetDefaultText("sound volume: " + gameSettings.soundVolume.ToString());
    }

    #endregion

    #region Gameplay Configuration

    // Cycles through dataset options
    public void IncrementStyle()
    {
        datasetStyle += 1;
        int datasetCount = Directory.GetDirectories(Application.persistentDataPath + "/SampleData").Length;
        datasetStyle %= datasetCount + 1;

        menuAudio.PlaySound(MenuAudio.MenuSounds.Toggle);
        UpdateGameplayConfiguration();
    }

    // Updates gameplay config ScriptableObject
    void UpdateGameplayConfiguration()
    {
        gameplayConfiguration.dataset = datasetStyle;

        UpdatePlayMenuUI();
    }

    // Updates the UI used on the play menu (selecting difficulty, style, etc)
    void UpdatePlayMenuUI()
    {
        styleButton.SetDefaultText("style: " + (gameplayConfiguration.dataset == 0 ? "default" : "custom " + gameplayConfiguration.dataset.ToString()));
    }

    #endregion
}
