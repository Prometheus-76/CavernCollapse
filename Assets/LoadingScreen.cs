using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    public Image mainBarImage;
    public Image stepBarImage;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI stepFlavourText;

    private float mainProgress;
    private float stepProgress;

    // Start is called before the first frame update
    void Start()
    {
        mainProgress = 0f;
        stepProgress = 0f;
    }

    public void SetMainProgress(float progress)
    {
        mainProgress = Mathf.Clamp01(progress);
        mainBarImage.fillAmount = mainProgress;
        progressText.text = "Generating... [ " + (mainProgress * 100f).ToString("F1") + "% ]";
    }

    public void SetStepProgress(float progress)
    {
        stepProgress = Mathf.Clamp01(progress);
        stepBarImage.fillAmount = stepProgress;
    }

    public void SetStepText(string stepText)
    {
        stepFlavourText.text = "> " + stepText.ToLower();
    }
}
