using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayUI : MonoBehaviour
{
    [SerializeField] private Image[] heartSprites;
    [SerializeField] private TextMeshProUGUI stageNumberText;
    [SerializeField] private TextMeshProUGUI stageNameText;
    [SerializeField] private TextMeshProUGUI coinsCollectedText;
    [SerializeField] private TextMeshProUGUI currentScoreText;
    [SerializeField] private TextMeshProUGUI timeRemainingText;

    public AttemptStats currentAttempt;

    [HideInInspector] public int currentStageNumber;
    [HideInInspector] public string currentStageName;
    [HideInInspector] public int totalCoins;
    [HideInInspector] public int currentScore;
    [HideInInspector] public float remainingTime;

    // Update is called once per frame
    void Update()
    {
        // Show heart sprites
        for (int i = 0; i < 3; i++)
        {
            heartSprites[i].enabled = (i < currentAttempt.currentHealth);
        }

        // Update stage details
        stageNumberText.text = "Stage " + (currentAttempt.stagesCleared + 1);
        stageNameText.text = currentStageName;

        // Update coin counter
        coinsCollectedText.text = "[ " + currentAttempt.coinsCollectedStage.ToString("D3") + " / " + totalCoins.ToString("D3") + " ]";

        // Update score display
        currentScoreText.text = "[ " + currentAttempt.currentScore.ToString("D7") + " ]";

        // Update remaining time
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        seconds = Mathf.Max(0, seconds);
        int minutes = Mathf.FloorToInt(remainingTime - seconds) / 60;
        minutes = Mathf.Max(0, minutes);
        timeRemainingText.text = "[ " + minutes.ToString("D2") + "m " + seconds.ToString("D2") + "s ]";
    }
}
