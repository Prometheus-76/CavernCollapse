using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayUI : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField] private Image[] heartSprites;
    [SerializeField] private TextMeshProUGUI stageNumberText;
    [SerializeField] private TextMeshProUGUI stageNameText;
    [SerializeField] private TextMeshProUGUI coinsCollectedText;
    [SerializeField] private TextMeshProUGUI currentScoreText;
    [SerializeField] private TextMeshProUGUI timeRemainingText;
    [SerializeField] private TextMeshProUGUI songText;
    [SerializeField] private TextMeshProUGUI gameplayConfigurationText;
    [SerializeField] private TextMeshProUGUI returnToMenuText;
    [SerializeField] private MenuButton proceedButton;

    [Header("Completion")]
    [SerializeField] private GameObject stageCompleteUI;
    [SerializeField] private TextMeshProUGUI completedStageText;
    [SerializeField] private TextMeshProUGUI completedCompletionText;
    [SerializeField] private TextMeshProUGUI completedHitsText;
    [SerializeField] private TextMeshProUGUI completedScoreText;

    [Header("Other Data")]
    public AttemptStats currentAttempt;
    public GameplayConfiguration gameplayConfiguration;

    [HideInInspector] public int currentStageNumber;
    [HideInInspector] public string currentStageName;
    [HideInInspector] public int totalCoins;
    [HideInInspector] public int currentScore;
    [HideInInspector] public float remainingTime;
    [HideInInspector] public float returningTimer;

    private void Start()
    {
        stageCompleteUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Show heart sprites
        for (int i = 0; i < 4; i++)
        {
            heartSprites[i].enabled = (i < currentAttempt.currentHealth);
        }

        // Update stage details
        stageNumberText.text = "Stage " + (currentStageNumber) + " of 5";
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
        switch (currentStageNumber)
        {
            case 1:
                songName = "soma";
                break;
            case 2:
                songName = "axil";
                break;
            case 3:
                songName = "pluvium";
                break;
            case 4:
                songName = "terra";
                break;
            case 5:
                songName = "nodes";
                break;
        }

        songText.text = "song: " + songName + " - magnofon";

        // Update return to menu text
        returnToMenuText.enabled = (returningTimer < 2f);
        returnToMenuText.text = "returning to menu... [ " + returningTimer.ToString("F1") + "s ]";
    }

    public void StageCompleteUI()
    {
        completedStageText.text = "STAGE: " + currentAttempt.stagesCleared + " - " + currentStageName + " [ " + gameplayConfiguration.difficulty.ToString().ToUpper() + " ]";
        completedCompletionText.text = "COMPLETION: " + Mathf.RoundToInt((currentAttempt.coinsCollectedStage / (float)totalCoins) * 100f) + "%";
        completedHitsText.text = "HITS TAKEN: " + ((currentAttempt.startingHealth - currentAttempt.currentHealth) > 0 ? (currentAttempt.startingHealth - currentAttempt.currentHealth).ToString() : "Flawless");
        completedScoreText.text = "STAGE SCORE: +" + currentAttempt.stageScore.ToString("N0");

        // Override button text if on the last stage
        if (currentAttempt.stagesCleared >= 5)
        {
            proceedButton.SetDefaultText("Complete Descent");
        }

        stageCompleteUI.SetActive(true);
    }
}
