using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [HideInInspector] public float remainingTime;
    [HideInInspector] public int totalCoinsInStage;

    public int collapseTimeStage1;
    public int collapseTimeStage2;
    public int collapseTimeStage3;
    public int collapseTimeStage4;
    public int collapseTimeStage5;

    [Header("Audio")]
    public AudioClip[] coinSounds;
    public AudioClip doorEntrySound;
    public AudioSource soundEffectAudioSource;
    public AudioSource loopingReturnAudio;

    private bool levelGenerated = false;
    private float returnToMenuTimer;
    private int currentStageNumber;
    private bool pauseTimer;
    private int stageStartingTime;

    [Header("Components")]
    public AudioSource buttonAudioSource;
    public KeepWhilePlaying keepWhilePlayingButton;
    public GameplayUI gameplayUI;
    public AttemptStats currentAttempt;
    public GameplayConfiguration gameplayConfiguration;
    public Image screenOverlayImage;

    private InputMaster inputMaster;

    private void Awake()
    {
        inputMaster = new InputMaster();
    }

    public void Start()
    {
        levelGenerated = false;
        returnToMenuTimer = 2f;
        pauseTimer = false;
    }

    public void Update()
    {
        if (levelGenerated)
        {
            if (remainingTime > 0f && currentAttempt.currentHealth > 0 && pauseTimer == false)
            {
                remainingTime -= Time.deltaTime;
                remainingTime = Mathf.Max(remainingTime, 0f);

                // When time runs out
                if (remainingTime <= 0f)
                {
                    PlayerController playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
                    playerController.Die();
                }
            }

            gameplayUI.currentStageNumber = currentStageNumber;
            gameplayUI.totalCoins = totalCoinsInStage;
            gameplayUI.remainingTime = remainingTime;

            // Returning to menu
            if (returnToMenuTimer > 0f)
            {
                if (inputMaster.Gameplay.ReturnToMenu.ReadValue<float>() != 0f)
                {
                    if (loopingReturnAudio.isPlaying == false) loopingReturnAudio.Play();

                    // When button is held
                    returnToMenuTimer -= Time.deltaTime;
                    returnToMenuTimer = Mathf.Max(returnToMenuTimer, 0f);

                    // Return to the main menu
                    if (returnToMenuTimer <= 0f)
                    {
                        // Change back to menu music
                        if (MusicPlayer.GetInstance() != null)
                            MusicPlayer.GetInstance().CrossFade(1);

                        loopingReturnAudio.Stop();
                        SceneManager.LoadScene(0);
                    }
                }
                else
                {
                    // When button is released
                    returnToMenuTimer = 2f;
                    loopingReturnAudio.Stop();
                }
            }

            gameplayUI.returningTimer = returnToMenuTimer;
        }
    }

    public void StageBegin()
    {
        switch (currentAttempt.stagesCleared)
        {
            case 0:
                remainingTime = collapseTimeStage1;
                stageStartingTime = collapseTimeStage1;
                break;
            case 1:
                remainingTime = collapseTimeStage2;
                stageStartingTime = collapseTimeStage2;
                break;
            case 2:
                remainingTime = collapseTimeStage3;
                stageStartingTime = collapseTimeStage3;
                break;
            case 3:
                remainingTime = collapseTimeStage4;
                stageStartingTime = collapseTimeStage4;
                break;
            case 4:
                remainingTime = collapseTimeStage5;
                stageStartingTime = collapseTimeStage5;
                break;
        }

        currentStageNumber = currentAttempt.stagesCleared + 1;
        currentAttempt.NewStage();
        levelGenerated = true;
    }

    public void StageComplete()
    {
        currentAttempt.stagesCleared += 1;
        currentAttempt.coinsCollectedTotal += currentAttempt.coinsCollectedStage;

        if (currentAttempt.currentHealth >= currentAttempt.startingHealth) currentAttempt.flawlessStages += 1;
        if (currentAttempt.coinsCollectedStage >= totalCoinsInStage) currentAttempt.fullCoinStages += 1;

        int timeTaken = stageStartingTime - Mathf.FloorToInt(remainingTime);
        currentAttempt.totalTime += timeTaken;

        currentAttempt.coinsInRunTotal += totalCoinsInStage;

        #region Score Calculation

        // Score calculated as sum of the following:
        // 1. coinsCollected * 100
        // 2. secondsRemaining * 75
        // 3. if stage is flawless, 15000 * currentHealth
        // 4. if all coins collected, 500 * coinsCollected
        // ...Then multiplied by stage number and difficulty multiplier

        // Multiplier based on difficulty
        int difficultyMultiplier = 0;
        switch (gameplayConfiguration.difficulty)
        {
            case GameplayConfiguration.DifficultyOptions.beginner:
                difficultyMultiplier = 1;
                break;
            case GameplayConfiguration.DifficultyOptions.standard:
                difficultyMultiplier = 2;
                break;
            case GameplayConfiguration.DifficultyOptions.expert:
                difficultyMultiplier = 3;
                break;
        }

        int coinScore = 100 * currentAttempt.coinsCollectedStage;
        int timeScore = 75 * Mathf.FloorToInt(remainingTime);
        int flawlessScore = (currentAttempt.currentHealth >= currentAttempt.startingHealth ? 15000 * currentAttempt.currentHealth : 0);
        int fullCoinScore = (currentAttempt.coinsCollectedStage >= totalCoinsInStage ? 500 * currentAttempt.coinsCollectedStage : 0);

        int stageScore = coinScore + timeScore + flawlessScore + fullCoinScore;
        stageScore *= difficultyMultiplier * currentAttempt.stagesCleared;

        currentAttempt.stageScore = stageScore;

        #endregion

        currentAttempt.currentScore += stageScore;

        gameplayUI.StageCompleteUI();

        // Restore 1hp for going flawless
        if (currentAttempt.currentHealth >= currentAttempt.startingHealth)
        {
            currentAttempt.currentHealth += 1;
            currentAttempt.currentHealth = Mathf.Min(currentAttempt.currentHealth, 4);
        }

        // Restore another 1hp if all coins were collected
        if (currentAttempt.coinsCollectedStage >= totalCoinsInStage)
        {
            currentAttempt.currentHealth += 1;
            currentAttempt.currentHealth = Mathf.Min(currentAttempt.currentHealth, 4);
        }
        
        currentAttempt.startingHealth = currentAttempt.currentHealth;

        // Fade out music when stage completed
        if (MusicPlayer.GetInstance() != null)
            MusicPlayer.GetInstance().CrossFade(0);

        pauseTimer = true;

        soundEffectAudioSource.PlayOneShot(doorEntrySound);
    }

    public void NextStage()
    {
        // Play button sound
        buttonAudioSource.Play();
        keepWhilePlayingButton.canBeDestroyed = true;

        // Until the final stage, progress to the next one
        if (currentStageNumber < 5)
        {
            // Reloading this scene is the same as progressing to the next level
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            // Go to the run ending scene
            StartCoroutine(FadeThenTransition());
        }
    }

    public void ReturnHome()
    {
        // Play button sound
        buttonAudioSource.Play();
        keepWhilePlayingButton.canBeDestroyed = true;

        // Change back to menu music
        if (MusicPlayer.GetInstance() != null)
            MusicPlayer.GetInstance().CrossFade(1);

        SceneManager.LoadScene(0);
    }

    public void CollectCoin()
    {
        // Add the coin to the collected count
        currentAttempt.coinsCollectedStage++;

        // Play a coin collection sound
        int coinSound = Random.Range(0, coinSounds.Length);
        soundEffectAudioSource.PlayOneShot(coinSounds[coinSound]);
    }

    public IEnumerator GameOver()
    {
        // Fade music out on game over
        if (MusicPlayer.GetInstance() != null)
            MusicPlayer.GetInstance().CrossFade(0);

        yield return new WaitForSeconds(3f);

        // Do calculation stuff
        currentAttempt.coinsCollectedTotal += currentAttempt.coinsCollectedStage;

        int timeTaken = stageStartingTime - Mathf.FloorToInt(remainingTime);
        currentAttempt.totalTime += timeTaken;

        #region Score Calculation

        // Score calculated as sum of the following:
        // 1. coinsCollected * 100
        // 2. secondsRemaining * 75
        // ...Then multiplied by stage number and difficulty multiplier

        // Multiplier based on difficulty
        int difficultyMultiplier = 0;
        switch (gameplayConfiguration.difficulty)
        {
            case GameplayConfiguration.DifficultyOptions.beginner:
                difficultyMultiplier = 1;
                break;
            case GameplayConfiguration.DifficultyOptions.standard:
                difficultyMultiplier = 2;
                break;
            case GameplayConfiguration.DifficultyOptions.expert:
                difficultyMultiplier = 3;
                break;
        }

        int coinScore = 100 * currentAttempt.coinsCollectedStage;

        int stageScore = coinScore;
        stageScore *= difficultyMultiplier * Mathf.Max(currentAttempt.stagesCleared, 1);

        currentAttempt.stageScore = stageScore;

        #endregion

        currentAttempt.currentScore += stageScore;

        pauseTimer = true;

        gameplayUI.GameOverUI();

        // Save new high scores
        if (currentAttempt.currentScore >= PlayerPrefs.GetInt("HighScore", 0)) PlayerPrefs.SetInt("HighScore", currentAttempt.currentScore);
        PlayerPrefs.Save();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player.SetActive(false);

        yield return null;
    }

    public IEnumerator FadeThenTransition()
    {
        Color panelColour = Color.white;
        panelColour.a = 0f;

        while (panelColour.a < 1f)
        {
            panelColour.a += 0.2f;
            panelColour.a = Mathf.Min(panelColour.a, 1f);
            screenOverlayImage.color = panelColour;

            yield return new WaitForSeconds(0.2f);
        }

        SceneManager.LoadScene(3);
        yield return null;
    }

    #region Input System

    private void OnEnable()
    {
        inputMaster.Enable();
    }

    private void OnDisable()
    {
        inputMaster.Disable();
    }

    #endregion
}
