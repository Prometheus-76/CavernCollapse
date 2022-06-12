using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Darcy Matheson 2022

// Controls the UI of the CCD editor scene
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
        // Load buttons
        if (fileManager.currentDataset == 0) datasetButton.SetDefaultText("dataset: [ default ]");
        if (fileManager.currentDataset != 0) datasetButton.SetDefaultText("dataset: [ custom " + fileManager.currentDataset.ToString() + " ]");

        sampleButton.SetDefaultText("sample: [ " + fileManager.currentSample.ToString("D2") + " / " + fileManager.currentSampleCount.ToString("D2") + " ]");

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

        // Save sample button
        saveButton.SetDefaultText("[ save current" + (editorManager.unsavedChanges ? "* ]" : " ]"));
    }
}
