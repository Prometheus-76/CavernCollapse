using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    private bool levelGenerated = false;
    private float returnToMenuTimer;
    private int currentStageNumber;

    public GameplayUI gameplayUI;
    public AttemptStats currentAttempt;
    public GameplayConfiguration gameplayConfiguration;

    private InputMaster inputMaster;

    private void Awake()
    {
        inputMaster = new InputMaster();
    }

    public void Start()
    {
        levelGenerated = false;
        returnToMenuTimer = 2f;
    }

    public void Update()
    {
        if (levelGenerated)
        {
            if (remainingTime > 0f && currentAttempt.currentHealth > 0)
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
                    // When button is held
                    returnToMenuTimer -= Time.deltaTime;
                    returnToMenuTimer = Mathf.Max(returnToMenuTimer, 0f);

                    // Return to the main menu
                    if (returnToMenuTimer <= 0f)
                    {
                        // Change back to menu music
                        if (MusicPlayer.GetInstance() != null)
                            MusicPlayer.GetInstance().CrossFade(1);

                        SceneManager.LoadScene(0);
                    }
                }
                else
                {
                    // When button is released
                    returnToMenuTimer = 2f;
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
                break;
            case 1:
                remainingTime = collapseTimeStage2;
                break;
            case 2:
                remainingTime = collapseTimeStage3;
                break;
            case 3:
                remainingTime = collapseTimeStage4;
                break;
            case 4:
                remainingTime = collapseTimeStage5;
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
    }

    public void NextStage()
    {
        // Reloading this scene is the same as progressing to the next level, at least for now
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void CollectCoin()
    {
        // Add the coin to the collected count
        currentAttempt.coinsCollectedStage++;
    }

    public void GameOver()
    {
        // Fade music out on game over
        if (MusicPlayer.GetInstance() != null)
            MusicPlayer.GetInstance().CrossFade(0);
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
