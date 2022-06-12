using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Darcy Matheson 2022

// Generic custom button class, handles the unique "pinching" effect with concatenated chevrons on button click
public class MenuButton : MonoBehaviour
{
    private TextMeshProUGUI textComponent;
    private string buttonText;
    private bool isClicking;
    private bool isHovering;

    public int buttonSide = 0;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        buttonText = textComponent.text;
        isClicking = false;
        isHovering = false;
    }

    public void SetDefaultText(string newDefault)
    {
        buttonText = newDefault;
    }

    void LateUpdate()
    {
        if (isHovering)
        {
            if (buttonSide == 0)
            {
                if (isClicking)
                {
                    textComponent.text = "- " + buttonText + " -";
                }
                else
                {
                    textComponent.text = "> " + buttonText + " <";
                }
            }
            else if (buttonSide == 1)
            {
                if (isClicking)
                {
                    textComponent.text = buttonText + " -";
                }
                else
                {
                    textComponent.text = buttonText + " <";
                }
            }
            else if (buttonSide == -1)
            {
                if (isClicking)
                {
                    textComponent.text = "- " + buttonText;
                }
                else
                {
                    textComponent.text = "> " + buttonText;
                }
            }
        }
        else
        {
            textComponent.text = buttonText;
        }
    }

    public void SetHoverState(bool state)
    {
        isHovering = state;
    }

    public void SetClickState(bool state)
    {
        isClicking = state;
    }
}
