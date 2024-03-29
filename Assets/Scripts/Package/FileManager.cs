using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// Darcy Matheson 2022

// Interfaces on behalf of the level editor directly with the files and directories used for storing datasets and samples
public class FileManager : MonoBehaviour
{
    #region Variables

    [Header("Parameters")]
    [Range(0f, 3f), Tooltip("How long it takes to delete a dataset or sample")] public float deleteTime;
    
    [Header("Components")]
    public EditorManager editorManager;
    public EditorAudio editorAudio;

    [HideInInspector]
    public SerialisedSample serialisedSample;

    // PROPERTIES
    public float deleteTimer { get; private set; }
    public int currentDataset { get; private set; } // If this is 0, then load the default dataset from the resources folder
    public int currentSample { get; private set; } // This starts at 1, since a "default sample" for each dataset doesn't really make sense
    public int currentSampleCount { get; private set; } // How many samples are in the current dataset
    public bool deleteDataset { get; private set; } // Whether the timer should delete a dataset or a sample

    #endregion

    private void Start()
    {
        // Start on the first sample in the default dataset (as this is guaranteed to exist)
        currentDataset = 0;
        currentSample = 1;

        deleteTimer = 0f;

        // Create the customs directory if it doesn't already exist
        if (Directory.Exists(Application.persistentDataPath + "/SampleData") == false)
            Directory.CreateDirectory(Application.persistentDataPath + "/SampleData");

        serialisedSample = new SerialisedSample(editorManager.buildZoneSize.x, editorManager.buildZoneSize.y);

        // Load the first sample in the default dataset
        // This MUST be done in Start() and not Awake(), or it could be reset by the EditorManager
        // calling ResetSample() when the sample data is first instantiated
        LoadSample();
    }

    // Update is called once per frame
    void Update()
    {
        if (deleteTimer > 0f)
        {
            deleteTimer = Mathf.Max(deleteTimer - Time.deltaTime, 0f);

            // If the timer was running and was reduced to 0 this frame (timer has ended)
            if (deleteTimer <= 0f)
            {
                Delete(deleteDataset);

                editorAudio.SetLooping(false);
            }
        }
    }

    // Saves the current sample within the appropriate dataset folder in JSON format
    public void Save()
    {
        if (currentDataset == 0)
        {
            // Changes are not allowed to be made to the default dataset
            editorAudio.PlayOneshot(EditorAudio.EditorSounds.Denied);
            return;
        }
        else
        {
            editorAudio.PlayOneshot(EditorAudio.EditorSounds.Positive);
        }

        // 1. Convert sampleData in EditorManager to 1D array and serialize
        // 2. Parse to JSON
        // 3. Write JSON to current file

        // Convert sample to 1D array and store in our serialisedSample variable
        serialisedSample.UpdateData(editorManager.sampleData);

        // Serialise the object to JSON string
        string sampleDataJSON = JsonUtility.ToJson(serialisedSample, true);

        // Save to the current sample file
        string fileDirectory = Application.persistentDataPath + "/SampleData/Dataset" + currentDataset + "/Sample" + currentSample + ".ccd";
        StreamWriter streamWriter = new StreamWriter(fileDirectory);
        streamWriter.Write(sampleDataJSON);
        streamWriter.Close();
        streamWriter.Dispose();

        // No more unsaved changes!
        editorManager.unsavedChanges = false;
    }

    // Finds the next dataset or sample and sets it appropriately with the currentDataset and currentSample variables
    public void LoadNext(bool dataset)
    {
        editorAudio.PlayOneshot(EditorAudio.EditorSounds.Toggle);

        if (dataset)
        {
            // Dataset:
            // 1. Find next dataset
            // 2. Find first sample within this dataset
            // 3. Load the sample within the dataset

            currentDataset += 1;
            int datasetCount = Directory.Exists(Application.persistentDataPath + "/SampleData") ? Directory.GetDirectories(Application.persistentDataPath + "/SampleData").Length : 0;

            // Wrap around if above the maximum number of custom datasets
            if (currentDataset > datasetCount)
                currentDataset = 0; // Return to default dataset

            currentSample = 1;
        }
        else
        {
            // Sample:
            // 1. Find the next sample
            // 2. Load the sample within the current dataset

            currentSample += 1;
            int sampleCount;
            if (currentDataset == 0)
                sampleCount = Resources.LoadAll("SampleData/").Length;
            else
                sampleCount = Directory.GetFiles(Application.persistentDataPath + "/SampleData/Dataset" + currentDataset).Length;

            // Wrap around if above the maximum number of samples within this dataset
            if (currentSample > sampleCount)
                currentSample = 1; // Return to first sample
        }

        // Now actually load the file we have chosen
        LoadSample();
    }

    // Loads the file at the given directory, of a given index
    void LoadSample()
    {
        // 1. Open the file
        // 2. Parse back as JSON
        // 3. Convert to 2D array and populate sampleData in the EditorManager
        // 4. Call EditorManager to recalculate tiles

        // Clean slate for when loading a new sample
        editorManager.ResetSample();

        // Open the file and read the data
        string fileData;
        if (currentDataset == 0)
        {
            // Loading default dataset sample
            // String syntax here for Resources.Load is EXTREMELY fussy!
            fileData = Resources.Load<TextAsset>("SampleData/Sample" + currentSample).text;
        }
        else
        {
            // Loading custom dataset sample
            string fileDirectory = Application.persistentDataPath + "/SampleData/Dataset" + currentDataset + "/Sample" + currentSample + ".ccd";

            StreamReader streamReader = new StreamReader(fileDirectory);
            fileData = streamReader.ReadToEnd();
            streamReader.Close();
            streamReader.Dispose();
        }

        // Parse data as JSON and assign to serialisedSample
        serialisedSample = JsonUtility.FromJson<SerialisedSample>(fileData);

        // Convert back to 2D array and assign to EditorManager
        for (int y = 0; y < editorManager.buildZoneSize.y; y++)
        {
            for (int x = 0; x < editorManager.buildZoneSize.x; x++)
            {
                editorManager.sampleData[x, y] = serialisedSample.data[y * editorManager.buildZoneSize.x + x];
            }
        }

        // No more unsaved changes!
        editorManager.unsavedChanges = false;

        // How many samples are in this dataset?
        if (currentDataset == 0)
            currentSampleCount = Resources.LoadAll("SampleData/").Length;
        else
            currentSampleCount = Directory.GetFiles(Application.persistentDataPath + "/SampleData/Dataset" + currentDataset).Length;

        // Recalculate and load in tiles, making sure everything is okay
        editorManager.UpdateTiles();
    }

    // Creates a new sample file or dataset folder, new datasets also create a new sample within
    public void New(bool dataset)
    {
        editorAudio.PlayOneshot(EditorAudio.EditorSounds.Scratch);
        editorManager.ResetSample();

        // If we should create a new dataset
        // Either directly by choice or if the player is trying to create something new while on the default dataset
        if (dataset || currentDataset == 0)
        {
            // Create a new dataset:
            // 1. Find the amount of folders within the Datasets parent folder (n)
            // 2. Create a new file (Sample 1) within a new folder (Dataset n+1)
            // 3. Load this new file

            int datasetCount = Directory.GetDirectories(Application.persistentDataPath + "/SampleData").Length;
            Directory.CreateDirectory(Application.persistentDataPath + "/SampleData/Dataset" + (datasetCount + 1).ToString());

            // Select the first sample in the latest dataset (just created)
            currentDataset = datasetCount + 1;
            currentSample = 1;
        }
        else
        {
            // Create a new sample:
            // 1. Create a new file within the current folder
            // 2. Load this new file

            // Just make a new sample normally, within the current dataset
            int sampleCount = Directory.GetFiles(Application.persistentDataPath + "/SampleData/Dataset" + currentDataset).Length;
            currentSample = sampleCount + 1;
        }

        // Save (and inherently, create if necessary) the file with default values (empty samples still store info!)
        Save();

        // Load the new file
        LoadSample();
    }

    // Deletes the current dataset or sample currently open
    // This is dangerous so we put it behind a timer (run in Update())
    // Cannot be triggered when on a sample within the default dataset (see StartDeleting() function)
    void Delete(bool dataset)
    {
        editorAudio.PlayOneshot(EditorAudio.EditorSounds.Descending);

        if (dataset)
        {
            // Dataset:
            // 1. Delete all files within folder
            // 2. Delete folder and load previous (this will always succeed because default is locked)
            // 3. Rename subsequent folders to collapse list into cleanly ordered set again (Directory.Move)

            Directory.Delete(Application.persistentDataPath + "/SampleData/Dataset" + currentDataset, true);
            int datasetCount = Directory.GetDirectories(Application.persistentDataPath + "/SampleData").Length;

            // Rename subsequent files
            for (int i = 0; i <= datasetCount - currentDataset; i++)
            {
                // Copy all files down one space
                string source = Application.persistentDataPath + "/SampleData/Dataset" + (currentDataset + i + 1).ToString();
                string destination = Application.persistentDataPath + "/SampleData/Dataset" + (currentDataset + i).ToString();
                Directory.Move(source, destination);
                if (Directory.Exists(source)) Directory.Delete(source, true);
            }

            // When the final directory is deleted, select the previous one, otherwise allowing the subsequent to fall into place
            currentSample = 1;
            if (datasetCount - currentDataset <= -1)
            {
                currentDataset -= 1;
            }
        }
        else
        {
            // Sample:
            // 1. Delete file within the folder
            // 2. Check if there are other files within the dataset, if not, delete the folder and load previous dataset
            // 3. If the file was deleted, rename subsequent files to collapse list into cleanly ordered set again
            // 4. If the folder was deleted, rename subsequent folders to collapse list into cleanly ordered set again

            // Delete the file and count remaining samples in this folder
            File.Delete(Application.persistentDataPath + "/SampleData/Dataset" + currentDataset + "/Sample" + currentSample + ".ccd");
            int sampleCount = Directory.GetFiles(Application.persistentDataPath + "/SampleData/Dataset" + currentDataset).Length;

            if (sampleCount <= 0)
            {
                // If there are no samples left in this dataset

                // Delete this dataset and rename other folders to retain order
                Directory.Delete(Application.persistentDataPath + "/SampleData/Dataset" + currentDataset.ToString());
                int datasetCount = Directory.GetDirectories(Application.persistentDataPath + "/SampleData").Length;

                // Rename subsequent folders
                for (int i = 0; i <= datasetCount - currentDataset; i++)
                {
                    // Copy all folders down one space
                    string source = Application.persistentDataPath + "/SampleData/Dataset" + (currentDataset + i + 1).ToString();
                    string destination = Application.persistentDataPath + "/SampleData/Dataset" + (currentDataset + i).ToString();
                    Directory.Move(source, destination);
                    if (Directory.Exists(source)) Directory.Delete(source, true);
                }

                // When the final directory is deleted, select the previous one, otherwise allowing the subsequent to fall into place
                currentSample = 1;
                if (datasetCount - currentDataset <= -1)
                {
                    currentDataset -= 1;
                }
            }
            else
            {
                // Rename subsequent files
                for (int i = 0; i <= sampleCount - currentSample; i++)
                {
                    // Copy all files down one space
                    string source = Application.persistentDataPath + "/SampleData/Dataset" + currentDataset + "/Sample" + (currentSample + i + 1).ToString() + ".ccd";
                    string destination = Application.persistentDataPath + "/SampleData/Dataset" + currentDataset + "/Sample" + (currentSample + i).ToString() + ".ccd";
                    File.Move(source, destination);
                    if (File.Exists(source)) File.Delete(source);
                }

                // When the final sample is deleted, select the previous one
                if (sampleCount - currentSample <= -1)
                {
                    currentSample -= 1;
                }

                // Load the new file that falls into the deleted files place (no increment)
            }
        }

        // Load the sample to fall back on
        LoadSample();
    }

    #region Deletion Timers

    // Set through UI, starts deletion timer and sets type
    public void StartDeleting(bool dataset)
    {
        if (currentDataset == 0)
        {
            // Don't allow deleting the default samples
            editorAudio.PlayOneshot(EditorAudio.EditorSounds.Denied);
            return;
        }

        editorAudio.SetLooping(true);
        deleteDataset = dataset;
        deleteTimer = deleteTime;
    }

    // Set through UI, stops deletion timer, irrelevant of type
    public void StopDeleting()
    {
        editorAudio.SetLooping(false);
        deleteTimer = 0f;
    }

    #endregion
}
