using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EditorUI : MonoBehaviour
{
    public EditorManager editorManager;

    [Header("UI Components")]
    public TextMeshProUGUI datasetText;
    public TextMeshProUGUI sampleText;
    public TextMeshProUGUI datasetDeleteText;
    public TextMeshProUGUI sampleDeleteText;

    public MenuButton blockTypeText;

    public TextMeshProUGUI saveText;

    // Update is called once per frame
    void Update()
    {
        blockTypeText.SetDefaultText("block type: [ " + editorManager.currentBlockType.ToString().ToUpper() + " ]");
    }
}
