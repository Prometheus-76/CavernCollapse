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
    [SerializeField] private TextMeshProUGUI songText;
    [SerializeField] private TextMeshProUGUI gameplayConfigurationText;
    [SerializeField] private TextMeshProUGUI returnToMenuText;

    public AttemptStats currentAttempt;
    public GameplayConfiguration gameplayConfiguration;

    [HideInInspector] public int currentStageNumber;
    [HideInInspector] public string currentStageName;
    [HideInInspector] public int totalCoins;
    [HideInInspector] public int currentScore;
    [HideInInspector] public float remainingTime;
    [HideInInspector] public float returningTimer;

    // Update is called once per frame
    void Update()
    {
        // Show heart sprites
        for (int i = 0; i < 4; i++)
        {
            heartSprites[i].enabled = (i < currentAttempt.currentHealth);
        }

        // Update stage details
        stageNumberText.text = "Stage " + (currentAttempt.stagesCleared + 1) + " of 5";
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

        // Update gameplay config text
        gameplayConfigurationText.text = gameplayConfiguration.difficulty.ToString() + " - " + (gameplayConfiguration.dataset == 0 ? "default" : "custom " + gameplayConfiguration.dataset.ToString());

        // Update current song text
        string songName = "";
        switch (currentAttempt.stagesCleared)
        {
            case 0:
                songName = "soma";
                break;
            case 1:
                songName = "axil";
                break;
            case 2:
                songName = "pluvium";
                break;
            case 3:
                songName = "terra";
                break;
            case 4:
                songName = "nodes";
                break;
        }

        songText.text = "song: " + songName + " - magnofon";

        // Update return to menu text
        returnToMenuText.enabled = (returningTimer < 2f);
        returnToMenuText.text = "returning to menu... [ " + returningTimer.ToString("F1") + "s ]";
    }
}
