using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EditorUI : MonoBehaviour
{
    public EditorManager editorManager;
    public FileManager fileManager;

    [Header("UI Components")]
    public MenuButton datasetButton;
    public MenuButton sampleButton;
    public MenuButton datasetDeleteButton;
    public MenuButton sampleDeleteButton;
    public MenuButton blockTypeButton;
    public MenuButton saveButton;

    // Update is called once per frame
    void Update()
    {
        // Delete buttons
        datasetDeleteButton.SetDefaultText("[ DELETE DATASET ]");
        sampleDeleteButton.SetDefaultText("[ DELETE SAMPLE ]");
        if (fileManager.deleteTimer > 0f && fileManager.deleteDataset)
        {
            datasetDeleteButton.SetDefaultText("[ DELETING " + ((1f - Mathf.Clamp01(fileManager.deleteTimer / fileManager.deleteTime)) * 100f).ToString("F0") + "% ]");
        }
        else if (fileManager.deleteTimer > 0f && fileManager.deleteDataset == false)
        {
            sampleDeleteButton.SetDefaultText("[ DELETING " + ((1f - Mathf.Clamp01(fileManager.deleteTimer / fileManager.deleteTime)) * 100f).ToString("F0") + "% ]");
        }

        // Block type button
        blockTypeButton.SetDefaultText("block type: [ " + editorManager.currentBlockType.ToString().ToUpper() + " ]");
    }
}
