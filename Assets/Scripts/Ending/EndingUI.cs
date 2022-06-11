using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndingUI : MonoBehaviour
{
    public AttemptStats currentAttempt;
    public GameplayConfiguration gameplayConfiguration;
    public Image screenOverlayImage;

    [Header("Sidebar")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI currentScoreText;
    public TextMeshProUGUI currentTimeText;
    public Image[] heartSprites;

    [Header("Run Completion")]
    public GameObject RunCompleteScreen;
    public TextMeshProUGUI completionText;
    public TextMeshProUGUI durationText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI newBestText;
    public TextMeshProUGUI subtitleText;

    public float sceneTimer;
    private int startingTime;

    // Start is called before the first frame update
    void Start()
    {
        coinsText.text = "[ " + currentAttempt.coinsCollectedTotal.ToString("D3") + " / " + currentAttempt.coinsInRunTotal.ToString("D3") + " ]";

        for (int i = 0; i < heartSprites.Length; i++)
        {
            heartSprites[i].enabled = i < currentAttempt.currentHealth;
        }

        RunCompleteScreen.SetActive(false);
        startingTime = currentAttempt.totalTime;

        subtitleText.text = gameplayConfiguration.difficulty.ToString().ToLower() + " - " + (gameplayConfiguration.dataset == 0 ? "default" : "custom " + gameplayConfiguration.dataset);

        StartCoroutine(FadeFromTransition());
    }

    // Update is called once per frame
    void Update()
    {
        currentScoreText.text = "[ " + currentAttempt.currentScore.ToString("D7") + " ]";
        
        // Update current time
        float totalTime = startingTime + sceneTimer;

        int seconds = Mathf.FloorToInt(totalTime % 60f);
        seconds = Mathf.Max(0, seconds);
        int minutes = Mathf.FloorToInt(totalTime - seconds) / 60;
        minutes = Mathf.Max(0, minutes);
        currentTimeText.text = "[ " + minutes.ToString("D2") + "m " + seconds.ToString("D2") + "s ]";
    }

    public void RunCompleteUI()
    {
        // Update completion text
        completionText.text = "COMPLETION: " + (Mathf.RoundToInt((currentAttempt.coinsCollectedTotal / (float)currentAttempt.coinsInRunTotal) * 1000f) / 10f).ToString("F1") + "%";

        // Update duration text
        float totalTime = startingTime + sceneTimer;
        int seconds = Mathf.FloorToInt(totalTime % 60f);
        seconds = Mathf.Max(0, seconds);
        int minutes = Mathf.FloorToInt(totalTime - seconds) / 60;
        minutes = Mathf.Max(0, minutes);
        durationText.text = "DURATION: " + minutes.ToString("D2") + "m " + seconds.ToString("D2") + "s";

        // Update final score
        finalScoreText.text = "FINAL SCORE: " + currentAttempt.currentScore.ToString("N0");

        // Update high score
        highScoreText.text = "HIGH SCORE: " + PlayerPrefs.GetInt("HighScore", 0).ToString("N0");

        // Show new best when highscore is met or beaten
        newBestText.enabled = PlayerPrefs.GetInt("HighScore", 0) <= currentAttempt.currentScore;

        RunCompleteScreen.SetActive(true);
    }

    public IEnumerator FadeFromTransition()
    {
        Color panelColour = Color.white;
        panelColour.a = 1f;

        while (panelColour.a > 0f)
        {
            panelColour.a -= 0.2f;
            panelColour.a = Mathf.Max(panelColour.a, 0f);
            screenOverlayImage.color = panelColour;

            yield return new WaitForSeconds(0.2f);
        }

        yield return null;
    }
}
