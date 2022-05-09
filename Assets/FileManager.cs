using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// Interfaces directly with the files and directories used for storing datasets and samples
public class FileManager : MonoBehaviour
{
    public EditorManager editorManager;
    public EditorAudio editorAudio;
    public float deleteTime;
    [SerializeField, Tooltip("The file extension following the .")] public string fileExtension;

    public float deleteTimer { get; private set; }
    public int currentDataset;
    public int currentSample;
    public bool deleteDataset { get; private set; }

    private void Start()
    {
        currentDataset = 1;
        currentSample = 1;
        deleteTimer = 0f;
    }

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
        editorAudio.PlayOneshot(EditorAudio.OneshotSounds.Positive);

        // 1. Convert sampleData in EditorManager to 1D array and serialize
        // 2. Parse to JSON
        // 3. Write JSON to current file
    }

    // Loads the next dataset or sample
    public void LoadNext(bool dataset)
    {
        editorAudio.PlayOneshot(EditorAudio.OneshotSounds.Toggle);

        // Dataset:
            // 1. Find next dataset
            // 2. Find first sample within this dataset
            // 3. Load the sample within the dataset

        // Sample:
            // 1. Find the next sample
            // 2. Load the sample within the current dataset

        LoadSample();
    }

    // Loads the file at the given directory, of a given index
    void LoadSample()
    {
        // 1. Open the file
        // 2. Parse back as JSON
        // 3. Convert to 2D array and populate sampleData in the EditorManager
        // 4. Update UI
        // 5. Reset tilemap and place these new tiles
    }

    // Creates a new sample file or dataset folder, new datasets also create a new sample within
    public void New(bool dataset)
    {
        editorAudio.PlayOneshot(EditorAudio.OneshotSounds.Scratch);

        if (dataset)
        {
            // Create a new dataset:
            // 1. Find the amount of folders within the Datasets parent folder (n)
            // 2. Create a new file (Sample 1) within a new folder (Dataset n+1)
            // 3. Load this new file

            int datasetCount = Directory.GetDirectories(Application.persistentDataPath + "/SampleData").Length;
            Directory.CreateDirectory(Application.persistentDataPath + "/SampleData/Dataset" + (datasetCount + 1).ToString());
            File.Create(Application.persistentDataPath + "/SampleData/Dataset" + (datasetCount + 1).ToString() + "/Sample1." + fileExtension);

            currentDataset = datasetCount + 1;
            currentSample = 1;
        }
        else
        {
            // Create a new sample:
            // 1. Create a new file within the current folder
            // 2. Load this new file

            int sampleCount = Directory.GetFiles(Application.persistentDataPath + "/SampleData/Dataset" + currentDataset).Length;
            File.Create(Application.persistentDataPath + "/SampleData/Dataset" + currentDataset + "/Sample" + (sampleCount + 1).ToString() + "." + fileExtension);

            currentSample = sampleCount + 1;
        }

        // Load the new file
        LoadSample();
    }

    // Deletes the current dataset or sample currently open
    // This is dangerous so we put it behind a timer (run in Update())
    void Delete(bool dataset)
    {
        editorAudio.PlayOneshot(EditorAudio.OneshotSounds.Descending);

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

            File.Delete(Application.persistentDataPath + "/SampleData/Dataset" + currentDataset + "/Sample" + currentSample + "." + fileExtension);
            int sampleCount = Directory.GetFiles(Application.persistentDataPath + "/SampleData/Dataset" + currentDataset).Length;
            if (sampleCount <= 0)
            {
                Directory.Delete(Application.persistentDataPath + "/SampleData/Dataset" + currentDataset.ToString());
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
                // Rename subsequent files
                for (int i = 0; i <= sampleCount - currentSample; i++)
                {
                    // Copy all files down one space
                    string source = Application.persistentDataPath + "/SampleData/Dataset" + currentDataset + "/Sample" + (currentSample + i + 1).ToString() + "." + fileExtension;
                    string destination = Application.persistentDataPath + "/SampleData/Dataset" + currentDataset + "/Sample" + (currentSample + i).ToString() + "." + fileExtension;
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

        LoadSample();
    }

    #region Deletion Timers

    // Set through UI, starts deletion timer and sets type
    public void StartDeleting(bool dataset)
    {
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
