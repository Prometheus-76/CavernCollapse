using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [HideInInspector] public int currentStageNumber;
    [HideInInspector] public int totalCoins;
    [HideInInspector] public float remainingTime;

    [HideInInspector] public int startingHealth;

    public int collapseTimeStage1;
    public int collapseTimeStage2;
    public int collapseTimeStage3;
    public int collapseTimeStage4;
    public int collapseTimeStage5;

    private bool levelGenerated = false;
    private float returnToMenuTimer;

    public GameplayUI gameplayUI;
    public AttemptStats currentAttempt;

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
            gameplayUI.totalCoins = totalCoins;
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

    public void StageBegin(int startingHealth, int totalCoins)
    {
        this.startingHealth = startingHealth;
        this.totalCoins = totalCoins;

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

    public void StageComplete(int coinsCollected, int currentHealth)
    {
        currentAttempt.stagesCleared += 1;
        currentAttempt.coinsCollectedTotal += totalCoins;

        if (currentHealth >= startingHealth) currentAttempt.flawlessStages += 1;
        if (coinsCollected >= totalCoins) currentAttempt.fullCoinStages += 1;
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
