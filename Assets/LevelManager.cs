using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [HideInInspector] public int currentStageNumber;
    [HideInInspector] public int totalCoins;
    [HideInInspector] public float remainingTime;

    [HideInInspector] public int startingHealth;

    public float collapseTime;

    private bool levelGenerated = false;

    public GameplayUI gameplayUI;
    public AttemptStats currentAttempt;

    public void Start()
    {
        levelGenerated = false;
    }

    public void Update()
    {
        if (levelGenerated)
        {
            remainingTime -= Time.deltaTime;
            remainingTime = Mathf.Max(remainingTime, 0f);

            gameplayUI.currentStageNumber = currentStageNumber;
            gameplayUI.totalCoins = totalCoins;
            gameplayUI.remainingTime = remainingTime;
        }
    }

    public void StageBegin(int startingHealth, int totalCoins)
    {
        this.startingHealth = startingHealth;
        this.totalCoins = totalCoins;
        remainingTime = collapseTime;

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
}
