using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MenuButton : MonoBehaviour
{
    private TextMeshProUGUI textComponent;
    private string buttonText;
    private bool isClicking;
    private bool isHovering;

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
            if (isClicking)
            {
                textComponent.text = "- " + buttonText + " -";
            }
            else
            {
                textComponent.text = "> " + buttonText + " <";
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
