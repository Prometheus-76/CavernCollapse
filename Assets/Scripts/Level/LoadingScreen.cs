using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    [Header("Configuration")]
    public float transitionTime;
    private float transitionTimer;

    [Header("Components")]
    public Image topBarImage;
    public Image bottomBarImage;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI stepFlavourText;
    public TextMeshProUGUI elapsedTimeText;
    public RectTransform topPanel;
    public RectTransform bottomPanel;
    public GameObject loadingScreen;

    private float mainProgress;
    private float stepProgress;
    private float elapsedTime;
    private bool hasStartedTransition;
    private string stepDescription;

    // Start is called before the first frame update
    void Start()
    {
        mainProgress = 0f;
        stepProgress = 0f;
        elapsedTime = 0f;
        hasStartedTransition = false;
        loadingScreen.SetActive(true);
    }

    void Update()
    {
        topBarImage.fillAmount = mainProgress;
        bottomBarImage.fillAmount = mainProgress;

        if (mainProgress < 1f)
        {
            progressText.text = "Generating Cavern   [ " + (mainProgress * 100f).ToString("F0") + "% ]";
            elapsedTime += Time.deltaTime;
        }
        else
        {
            progressText.text = "Generation Complete!";
        }

        stepFlavourText.text = "> " + stepDescription.ToLower();
        elapsedTimeText.text = "> " + elapsedTime.ToString("F2") + "s";

        // Play animation when generation has completed
        if (mainProgress >= 1f && hasStartedTransition == false) StartCoroutine(SeparatePanels());
    }

    public void SetMainProgress(float progress)
    {
        mainProgress = Mathf.Clamp01(progress);
    }

    public void SetStepProgress(float progress)
    {
        stepProgress = Mathf.Clamp01(progress);
    }

    public void SetStepText(string stepText)
    {
        stepDescription = stepText;
    }

    // Separates the UI panels on the loading screen when the main generation progress reaches 100%
    IEnumerator SeparatePanels()
    {
        hasStartedTransition = true;

        // Where the panel starts and finishes during the animation
        Vector2 startPos = Vector3.zero;
        startPos.y = -500f;

        Vector2 targetPos = Vector3.zero;
        targetPos.y = -1100f;

        // Wait a little before animating
        yield return new WaitForSeconds(0.7f);

        // Loop until transition time has passed
        while (transitionTimer <= transitionTime)
        {
            transitionTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(transitionTimer / transitionTime);
            progress *= progress;

            Vector2 newPosition = Vector2.Lerp(startPos, targetPos, progress);
            bottomPanel.anchoredPosition = newPosition;
            topPanel.anchoredPosition = -newPosition;

            // Wait until next frame
            yield return null;
        }

        loadingScreen.SetActive(false);
        yield return null;
    }
}
