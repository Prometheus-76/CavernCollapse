using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneLoader : MonoBehaviour
{
    public TextMeshProUGUI progressText;
    public Image progressBar;
    public TextMeshProUGUI flavourText;

    [Range(0f, 1f)] public float progress;

    // Update is called once per frame
    void Update()
    {
        progressText.text = "loading... [" + (progress * 100f).ToString("F1") + "%]";
        progressBar.fillAmount = progress;
    }
}
