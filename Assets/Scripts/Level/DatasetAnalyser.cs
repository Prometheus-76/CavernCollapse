using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// Loads in all samples within a dataset and constructs a ruleset for them, which can be queried by external scripts

public class DatasetAnalyser : MonoBehaviour
{
    public TileCollection tileCollection;
    public GameplayConfiguration gameplayConfiguration;
    private SerialisedSample[] datasetSamples;
    private int[,,] constructedRuleset; // Centre tile, neighbour space, neighbour tile -> weight/possibility of tile at position

    // Start is called before the first frame update
    void Start()
    {
        // Allocate ruleset memory
        constructedRuleset = new int[tileCollection.tiles.Length, 8, tileCollection.tiles.Length];

        LoadDataset(gameplayConfiguration.dataset);
        ConstructRuleset();
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Loads in all samples from a dataset and stores within the datasetSamples array
    public void LoadDataset(int datasetIndex)
    {
        // How many samples are in this dataset?
        int sampleCount;
        if (datasetIndex == 0)
            sampleCount = Resources.LoadAll("SampleData/").Length;
        else
            sampleCount = Directory.GetFiles(Application.persistentDataPath + "/SampleData/Dataset" + datasetIndex).Length;

        // Clear the dataset to load in a new one
        datasetSamples = new SerialisedSample[sampleCount];

        // Load in each sample one at a time
        for (int sampleIndex = 1; sampleIndex <= sampleCount; sampleIndex++)
        {
            // Open the file and read the data
            string fileData;
            if (datasetIndex == 0)
            {
                // Loading default dataset sample
                // String syntax here for Resources.Load is EXTREMELY fussy!
                fileData = Resources.Load<TextAsset>("SampleData/Sample" + sampleIndex).text;
            }
            else
            {
                // Loading custom dataset sample
                string fileDirectory = Application.persistentDataPath + "/SampleData/Dataset" + datasetIndex + "/Sample" + sampleIndex + ".ccd";

                StreamReader streamReader = new StreamReader(fileDirectory);
                fileData = streamReader.ReadToEnd();
                streamReader.Close();
                streamReader.Dispose();
            }

            // Parse data as JSON and assign to serialisedSample
            datasetSamples[sampleIndex - 1] = new SerialisedSample(JsonUtility.FromJson<SerialisedSample>(fileData));
        }
    }

    public void ConstructRuleset()
    {
        // Requires the dataset to be loaded in already
        if (datasetSamples == null)
            return;

        #region Reset Ruleset Data

        // For each centre tile
        for (int centreIndex = 0; centreIndex < constructedRuleset.GetLength(0); centreIndex++)
        {
            int neighbourIndex = 0;

            // For each neighbour space of this tile
            for (int yOffset = 1; yOffset >= -1; yOffset--)
            {
                for (int xOffset = -1; xOffset <= 1; xOffset++)
                {
                    // Skip centre tile
                    if (xOffset == 0 && yOffset == 0)
                        continue;

                    // For each neighbour that could be at this position
                    for (int neighbourTileIndex = 0; neighbourTileIndex < constructedRuleset.GetLength(2); neighbourTileIndex++)
                    {
                        // Reset ruleset data
                        constructedRuleset[centreIndex, neighbourIndex, neighbourTileIndex] = 0;
                    }

                    neighbourIndex++;
                }
            }
        }

        #endregion

        #region Analyse Samples

        // For each sample in the dataset...
        for (int sampleIndex = 0; sampleIndex < datasetSamples.Length; sampleIndex++)
        {
            // For each cell in the 1D sample...
            for (int tilePos1D = 0; tilePos1D < datasetSamples[sampleIndex].data.Length; tilePos1D++)
            {
                // What the position of the tile would be in 2D within the sample
                Vector2Int pos2D = Vector2Int.zero;
                pos2D.x = tilePos1D % datasetSamples[sampleIndex].sampleSize.x;
                pos2D.y = (tilePos1D - pos2D.x) / datasetSamples[sampleIndex].sampleSize.x;

                // The tile index used by the centre tile
                int centreTileIndex = datasetSamples[sampleIndex].data[tilePos1D].tileIndex;

                // For each of the neighbours of this cell...
                int neighbourIndex = 0;
                for (int yOffset = 1; yOffset >= -1; yOffset--)
                {
                    for (int xOffset = -1; xOffset <= 1; xOffset++)
                    {
                        // Skip centre tile
                        if (xOffset == 0 && yOffset == 0)
                            continue;

                        // Add offset to find 2D neighbour position...
                        Vector2Int neighbourPos2D = Vector2Int.zero;
                        neighbourPos2D.x = pos2D.x + xOffset;
                        neighbourPos2D.y = pos2D.y + yOffset;

                        if (neighbourPos2D.x >= 0 && neighbourPos2D.x < datasetSamples[sampleIndex].sampleSize.x)
                        {
                            if (neighbourPos2D.y >= 0 && neighbourPos2D.y < datasetSamples[sampleIndex].sampleSize.y)
                            {
                                // ...and back to 1D again!
                                int neighbourPos1D = (neighbourPos2D.y * datasetSamples[sampleIndex].sampleSize.x) + neighbourPos2D.x;

                                // The tile index used by the neighbour tile
                                int neighbourTileIndex = datasetSamples[sampleIndex].data[neighbourPos1D].tileIndex;

                                // This specific case has now occurred once, so add some weight
                                constructedRuleset[centreTileIndex, neighbourIndex, neighbourTileIndex] += 1;
                            }
                        }

                        neighbourIndex++;
                    }
                }
            }
        }

        #endregion
    }
}
