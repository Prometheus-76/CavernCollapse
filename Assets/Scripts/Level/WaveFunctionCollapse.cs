using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapse : MonoBehaviour
{
    public TileCollection tileCollection;
    public DatasetAnalyser datasetAnalyser;

    public struct WaveFunctionTile
    {
        // All remaining possibilities of this tile space
        public DatasetAnalyser.RuleData[] tileSuperpositions;
        public int totalSuperpositionWeight; // Cached data to save calculating every time the tile is collapsed
        public int totalSuperpositionTypes; // Cached data to save calculating every time the lowest tile entropy is searched

        public BlockType blockType;
        public int tileIndex;
        public bool canCollapse;
    }

    private WaveFunctionTile[,] waveFunctionGrid;
    private Vector2Int gridSize;
    private BlockTypeFlags blockPalette;
    private ulong lowestFindIterations = 0;
    private ulong collapseIterations = 0;
    private ulong recalculateIterations = 0;

    public BlockType GetCollapsedType(int x, int y)
    {
        return waveFunctionGrid[x, y].blockType;
    }

    public int GetCollapsedTileIndex(int x, int y)
    {
        return waveFunctionGrid[x, y].tileIndex;
    }

    // Called from external script to set internal array to correct size
    public void InitialiseWaveFunction(int xSize, int ySize)
    {
        waveFunctionGrid = new WaveFunctionTile[xSize, ySize];

        gridSize.x = xSize;
        gridSize.y = ySize;

        // Default grid values
        for (int y = 0; y < ySize; y++)
        {
            for (int x = 0; x < xSize; x++)
            {
                waveFunctionGrid[x, y] = new WaveFunctionTile();
                waveFunctionGrid[x, y].tileSuperpositions = new DatasetAnalyser.RuleData[tileCollection.tiles.Length];
                ResetTile(x, y);
            }
        }

        // Reset the palette to have no block types selected
        ResetBlockPalette();
    }

    // Allows external scripts to predetermine a tile before wave function collapse
    // Also called internal when collapsing tiles automatically
    public void SetTile(int x, int y, BlockType blockType, int tileIndex)
    {
        waveFunctionGrid[x, y].blockType = blockType;
        waveFunctionGrid[x, y].tileIndex = tileIndex;
        waveFunctionGrid[x, y].canCollapse = false;
    }

    // Resets a tile to the default values, so it can be collapsed
    public void ResetTile(int x, int y)
    {
        waveFunctionGrid[x, y].blockType = BlockType.None;
        waveFunctionGrid[x, y].tileIndex = -1;
        waveFunctionGrid[x, y].canCollapse = true;

        // Reset entropy
        for (int i = 0; i < waveFunctionGrid[x, y].tileSuperpositions.Length; i++)
        {
            waveFunctionGrid[x, y].tileSuperpositions[i].tileWeight = 0;
            waveFunctionGrid[x, y].tileSuperpositions[i].blockType = BlockType.None;
        }
    }

    // Recalculates the entropy for a given tile
    public void RecalculateEntropy(int x, int y)
    {
        // Skip if the tile is already collapsed
        if (waveFunctionGrid[x, y].canCollapse == false)
            return;

        // Reset existing weights
        waveFunctionGrid[x, y].totalSuperpositionWeight = 0;
        for (int superpositionIndex = 0; superpositionIndex < waveFunctionGrid[x, y].tileSuperpositions.Length; superpositionIndex++)
        {
            waveFunctionGrid[x, y].tileSuperpositions[superpositionIndex].tileWeight = 0;
        }

        // For each neighbour (left->right, top->bottom)
        int neighbourIndex = -1;
        for (int yOffset = 1; yOffset >= -1; yOffset--)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                // Skip centre tile
                if (xOffset == 0 && yOffset == 0)
                    continue;

                neighbourIndex++;

                // Skip the tile if its out of range of the grid
                if (x + xOffset < 0 || x + xOffset >= gridSize.x || y + yOffset < 0 || y + yOffset >= gridSize.y)
                    continue;

                // Skip uncollapsed neighbours, as they don't have any valid information
                if (waveFunctionGrid[x + xOffset, y + yOffset].canCollapse)
                    continue;

                // For every tile superposition for this neighbour
                for (int tileIndex = 0; tileIndex < tileCollection.tiles.Length; tileIndex++)
                {
                    // Get the rule data from the perspective of the neighbour tile, inward to the centre tile
                    int ruleWeight = datasetAnalyser.GetWeightFromRuleset(waveFunctionGrid[x + xOffset, y + yOffset].tileIndex, 7 - neighbourIndex, tileIndex);
                    BlockType ruleType = datasetAnalyser.GetTypeFromRuleset(waveFunctionGrid[x + xOffset, y + yOffset].tileIndex, 7 - neighbourIndex, tileIndex);

                    // Only consider entropy of a set of superpositions
                    if (IsBlockInPalette(ruleType))
                    {
                        //bool newSuperpositionAdded = false;

                        // Add to the weight if this configuration is supported so far
                        if (waveFunctionGrid[x, y].tileSuperpositions[tileIndex].tileWeight != -1)
                        {
                            waveFunctionGrid[x, y].tileSuperpositions[tileIndex].tileWeight += ruleWeight;
                            waveFunctionGrid[x, y].totalSuperpositionWeight += ruleWeight;
                        }

                        // If this configuration has now been confirmed as illegal, disable it
                        if (ruleWeight <= 0)
                        {
                            waveFunctionGrid[x, y].totalSuperpositionWeight -= waveFunctionGrid[x, y].tileSuperpositions[tileIndex].tileWeight;
                            waveFunctionGrid[x, y].tileSuperpositions[tileIndex].tileWeight = -1;
                        }

                        // Set the block type
                        waveFunctionGrid[x, y].tileSuperpositions[tileIndex].blockType = ruleType;
                        recalculateIterations++;
                    }
                }
            }
        }

        // For each superposition
        waveFunctionGrid[x, y].totalSuperpositionTypes = 0;
        for (int superpositionIndex = 0; superpositionIndex < tileCollection.tiles.Length; superpositionIndex++)
        {
            // If the superposition has meaningful weight
            if (waveFunctionGrid[x, y].tileSuperpositions[superpositionIndex].tileWeight > 0)
            {
                waveFunctionGrid[x, y].totalSuperpositionTypes++;
            }
        }
    }

    // Recalculates entropy of the entire grid
    public void RecalculateAllEntropy()
    {
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                RecalculateEntropy(x, y);
            }
        }
    }

    // Finds the uncollapsed, collapsable tile with the smallest number of remaining possibilities
    // Generally speaking, this one should be collapsed sooner rather than later
    public Vector2Int GetLowestEntropyTile(bool allowZero)
    {
        Vector2Int lowestEntropySpace = new Vector2Int(-1, -1);

        // Important note, the value tracked by this is the number of different superpositions, NOT the sum of superposition weights
        int lowestEntropyValue = int.MaxValue;

        // For each tile in the level grid
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                if (waveFunctionGrid[x, y].canCollapse == false)
                    continue;

                int tileEntropyTypes = waveFunctionGrid[x, y].totalSuperpositionTypes;
                
                lowestFindIterations++;

                // New lowest, non 0 entropy tile
                if ((tileEntropyTypes != 0 || allowZero) && tileEntropyTypes < lowestEntropyValue)
                {
                    lowestEntropyValue = tileEntropyTypes;
                    lowestEntropySpace.x = x;
                    lowestEntropySpace.y = y;

                    // If this tile has an entropy of 1 and is not allowed to be lower
                    if (allowZero == false && lowestEntropyValue == 1)
                    {
                        return lowestEntropySpace;
                    }

                    // If this tile has an entropy of 0 and as such, cannot be lower
                    if (allowZero && lowestEntropyValue == 0)
                    {
                        return lowestEntropySpace;
                    }
                }
            }
        }

        return lowestEntropySpace;
    }

    // Collapses a tile at a position, given the palette and weight values
    public bool CollapseTile(int x, int y)
    {
        // Get the block type and tile index from weighted random

        // Add all weights together
        int weightSum = waveFunctionGrid[x, y].totalSuperpositionWeight;

        if (weightSum <= 0) return false;

        // Iterative subtraction to find weighted random
        int weightedRandom = Random.Range(0, weightSum + 1);
        for (int superpositionIndex = 0; superpositionIndex < waveFunctionGrid[x, y].tileSuperpositions.Length; superpositionIndex++)
        {
            int ruleWeight = waveFunctionGrid[x, y].tileSuperpositions[superpositionIndex].tileWeight;
            BlockType ruleType = waveFunctionGrid[x, y].tileSuperpositions[superpositionIndex].blockType;

            // If the block type is allowed and the tile weight is meaningful
            if (ruleWeight > 0)
            {
                weightedRandom -= ruleWeight;
                collapseIterations++;

                // If this superposition is what the random number landed on
                if (weightedRandom <= 0)
                {
                    // Collapse this tile to superpositionIndex
                    SetTile(x, y, ruleType, superpositionIndex);
                    return true;
                }
            }
        }

        return false;
    }

    // Returns the number of tiles which haven't been collapsed yet
    public int GetUncollapsedCount()
    {
        int count = 0;

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                if (waveFunctionGrid[x, y].canCollapse)
                    count++;
            }
        }

        return count;
    }

    #region Block Palette

    // Resets the block palette to allow no block types (should then be followed by AddToBlockPalette calls to configure palette as desired)
    public void ResetBlockPalette()
    {
        blockPalette = 0;
    }

    // Adds the block type to the palette so the algorithm will use it when collapsing tiles
    public void AddToBlockPalette(BlockType blockType)
    {
        // BlockType is stored as normal enum (0, 1, 2, 3, etc)
        // BlockTypeFlags is stored as flags enum (0, 1, 2, 4, 8, 16, etc)

        // For this reason, the BlockType is converted to offset binary so it is compatible with the palette format
        // Add the type to the palette, if it isn't already enabled on there
        blockPalette |= (BlockTypeFlags)(1 << (int)blockType);
    }

    // Returns true if the palette contains a given block type, otherwise returns false
    public bool IsBlockInPalette(BlockType blockType)
    {
        // BlockType is stored as normal enum (0, 1, 2, 3, etc)
        // BlockTypeFlags is stored as flags enum (0, 1, 2, 4, 8, 16, etc)

        // For this reason, the BlockType is converted to offset binary so it is compatible with the palette format
        // Check if the palette has the current type and return the result
        return blockPalette.HasFlag((BlockTypeFlags)(1 << (int)blockType));
    }

    #endregion
}
