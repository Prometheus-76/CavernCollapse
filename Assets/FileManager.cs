using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Interfaces directly with the files and directories used for storing datasets and samples
public class FileManager : MonoBehaviour
{
    public EditorManager editorManager;
    public float deleteTime;

    private float deleteTimer = 0f;
    private bool deleteDataset = false;

    void Update()
    {
        if (deleteTimer > 0f)
        {
            deleteTimer = Mathf.Max(deleteTimer - Time.deltaTime, 0f);

            if (deleteTimer <= 0f)
            {
                Delete(deleteDataset);
            }
        }
    }

    // Saves the current sample within the appropriate dataset folder in JSON format
    public void Save()
    {

    }

    // Loads the next dataset or sample
    public void LoadNext(bool dataset)
    {

    }

    // Creates a new sample file or dataset folder, new datasets also create a new sample within
    public void New(bool dataset)
    {

    }

    // Deletes the current dataset or sample currently open
    // This is dangerous so we put it behind a timer (run in Update())
    void Delete(bool dataset)
    {

    }

    // Set through UI, 
    public void SetDeletingState(bool dataset, bool isDeleting)
    {
        deleteDataset = dataset;
        deleteTimer = (isDeleting ? deleteTime : 0f);
    }
}
