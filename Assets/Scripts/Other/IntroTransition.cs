using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

// Fades in from splash screen

public class IntroTransition : MonoBehaviour
{
    public float transitionTime;
    public Volume postProcessing;
    public Image panelImage;

    private static IntroTransition instance;
    private float transitionProgress;

    // Start is called before the first frame update
    void Awake()
    {
        // Ensure the intro transition only happens once
        if (!instance)
        {
            instance = this;

            transitionProgress = 0f;
            postProcessing.weight = 0f;
            Color newPanelColour = panelImage.color;
            newPanelColour.a = 1f;
            panelImage.color = newPanelColour;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (transitionProgress < 1f)
        {
            transitionProgress += Time.deltaTime / transitionTime;
            transitionProgress = Mathf.Clamp01(transitionProgress);

            Color newPanelColour = panelImage.color;
            newPanelColour.a = 1f - (transitionProgress * transitionProgress);
            panelImage.color = newPanelColour;

            postProcessing.weight = transitionProgress * transitionProgress;
        }
    }
}
