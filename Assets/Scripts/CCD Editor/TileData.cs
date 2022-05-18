using UnityEngine;

// Storage format for data that represents the type of tile at a position in the editor

// Every distinct type of tile which can appear in the game
public enum BlockType
{
    None,
    Solid,
    OneWay,
    Ladder,
    Spike,
    Coin,
    Vine,
    Foliage,
    Sign,
    Torch,
    Count
}

// SHOULD MATCH ABOVE!
// This is used by the wave function collapse implementation to mark which tiles can be collapsed and which canot
// "Count" enum value is not required here, but should remain the last one in the set above
[System.Flags]
public enum BlockTypeFlags : int
{
    None = 1,
    Solid = 2,
    OneWay = 4,
    Ladder = 8,
    Spike = 16,
    Coin = 32,
    Vine = 64,
    Foliage = 128,
    Sign = 256,
    Torch = 512
}

// Represents each space in the grid
[System.Serializable]
public struct TileData
{
    public BlockType blockType;

    // This is only used for tiles like grass that have variations but don't affect gameplay
    // It looks up into a TileCollection scriptable object to find the tile prefab to use
    public int tileIndex;

    // An offset from a given tile, used for cycling through variations
    public int tileOffset;

    // Whether to add the offset to the index to find the tile
    public bool usingOffset;
}

// Wrapped "JSON-friendly" version of sample data
[System.Serializable]
public class SerialisedSample
{
    // JsonUtility.ToJson() doesn't like:
    // Multidimensional arrays
    // Non-serializable classes and structs
    // Being directly passed an array instead of a singular object (like this class!)

    public Vector2Int sampleSize;
    public TileData[] data;
    // EditorManager is not stored here, as it would be overwritten every time the JSON data is parsed back in on load

    // Constructor
    public SerialisedSample(int sizeX, int sizeY)
    {
        // Allocate the memory for the sample data
        data = new TileData[sizeX * sizeY];
        sampleSize.x = sizeX;
        sampleSize.y = sizeY;
    }

    // Copy constructor
    public SerialisedSample(SerialisedSample sample)
    {
        // Allocate the memory for the sample data
        sampleSize.x = sample.sampleSize.x;
        sampleSize.y = sample.sampleSize.y;
        data = new TileData[sampleSize.x * sampleSize.y];

        // Copy data across to this instance of the object
        for (int y = 0; y < sampleSize.y; y++)
        {
            for (int x = 0; x < sampleSize.x; x++)
            {
                data[y * sampleSize.x + x].blockType = sample.data[y * sampleSize.x + x].blockType;
                data[y * sampleSize.x + x].tileIndex = sample.data[y * sampleSize.x + x].tileIndex;
                data[y * sampleSize.x + x].tileOffset = sample.data[y * sampleSize.x + x].tileOffset;
                data[y * sampleSize.x + x].usingOffset = sample.data[y * sampleSize.x + x].usingOffset;
            }
        }
    }

    // Updates the data array from the 2D editor data array
    public void UpdateData(TileData[,] sampleData)
    {
        // Write to 1D array
        for (int y = 0; y < sampleSize.y; y++)
        {
            for (int x = 0; x < sampleSize.x; x++)
            {
                data[y * sampleSize.x + x] = sampleData[x, y];
            }
        }
    }
}
