using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Attempt Stats")]
public class AttemptStats : ScriptableObject
{
    [Header("Stage")]
    public int currentHealth = 0;
    public int coinsCollectedStage = 0;

    [Header("Attempt")]
    public int stagesCleared = 0;
    public int coinsCollectedTotal = 0;
    public int flawlessStages = 0;
    public int fullCoinStages = 0;
    public int currentScore = 0;

    public void NewAttempt()
    {
        currentHealth = 4;
        coinsCollectedStage = 0;

        stagesCleared = 0;
        coinsCollectedTotal = 0;
        flawlessStages = 0;
        fullCoinStages = 0;
        currentScore = 0;
    }

    public void NewStage()
    {
        coinsCollectedStage = 0;
    }
}
