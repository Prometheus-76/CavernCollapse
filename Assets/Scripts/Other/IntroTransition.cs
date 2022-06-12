using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Darcy Matheson 2022

// Fades in from splash screen
public class IntroTransition : MonoBehaviour
{
    public float transitionTime;
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

            // If the player transitions scenes too quickly then the references to the main scene will break
            if (panelImage != null)
            {
                Color newPanelColour = panelImage.color;
                newPanelColour.a = 1f - (transitionProgress * transitionProgress);
                panelImage.color = newPanelColour;
            }
        }
    }
}
