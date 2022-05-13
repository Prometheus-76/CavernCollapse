using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Interfaces with the user, allowing them to build templates
// It also handles determining which sprites to show given neighbouring tiles
public class EditorManager : MonoBehaviour
{
    #region Data Structures

    // For undo/redo stack
    public struct EditorAction
    {
        public enum ActionType
        {
            Place,
            Remove,
            CycleForward,
            CycleBackward
        }

        public Vector2Int position;
        public TileData tileData;
        public ActionType actionType;
        public BlockType previousType;
    }

    // For determining sprite based on surroundings
    [System.Flags]
    public enum TileNeighbours : byte
    {
        TL = 1, // Top-left
        TM = 2, // Top-middle
        TR = 4, // Top-right
        ML = 8, // Middle-left
        MR = 16, // Middle-right
        BL = 32, // Bottom-left
        BM = 64, // Bottom-middle
        BR = 128, // Bottom-right
    }

    #endregion

    #region Variables

    [Header("Build Settings")]
    public Vector2Int buildZoneSize;
    public Vector2 buildZoneOffset;
    public bool treatBorderAsWall;
    public int undoStackSize;

    [Header("Configuration")]
    public TileCollection tileCollection;

    [Header("Components")]
    public FileManager fileManager;
    public EditorAudio editorAudio;
    public Tilemap tilemap;
    public TilemapRenderer tilemapRenderer;
    public Transform tilesParent;
    public KeepWhilePlaying buttonAudioSource;

    [HideInInspector] public TileData[,] sampleData;
    [HideInInspector] public BlockType currentBlockType;
    [HideInInspector] public bool unsavedChanges;

    #region Private 

    // CURSOR
    private Vector2Int gridCursorPos;
    private bool cursorWithinGrid;

    // UNDO/REDO
    private List<EditorAction> undoList;
    private List<EditorAction> redoList;

    // BUILDING

    // OTHER
    private InputMaster inputMaster;
    private Camera mainCamera;

    #endregion

    #endregion

    void Awake()
    {
        inputMaster = new InputMaster();
        mainCamera = Camera.main;

        sampleData = new TileData[buildZoneSize.x, buildZoneSize.y];
        undoList = new List<EditorAction>();
        redoList = new List<EditorAction>();

        currentBlockType = BlockType.Solid;
        unsavedChanges = false;

        // Align tilemap with world position of grid, this means inserting items into the grid matches the tilemap coordinates
        tilesParent.position = buildZoneOffset - ((Vector2)buildZoneSize / 2f);

        ResetSample();
    }

    // Update is called once per frame
    void Update()
    {
        // Where is the cursor in the world?
        Vector2 worldCursorPos = mainCamera.ScreenToWorldPoint(inputMaster.Editor.Cursor.ReadValue<Vector2>());

        // Convert that to grid space...
        gridCursorPos = WorldToGrid(worldCursorPos.x, worldCursorPos.y);

        // Is the cursor within the bounds of the grid?
        cursorWithinGrid = false;
        if (gridCursorPos.x >= 0 && gridCursorPos.x < buildZoneSize.x)
        {
            if (gridCursorPos.y >= 0 && gridCursorPos.y < buildZoneSize.y)
            {
                cursorWithinGrid = true;
            }
        }

        #region Input -> Editor Function Calls

        // If the cursor should be able to interact with things
        if (cursorWithinGrid)
        {
            if (inputMaster.Editor.Place.ReadValue<float>() != 0f)
            {
                if (sampleData[gridCursorPos.x, gridCursorPos.y].blockType != currentBlockType)
                {
                    // Change type and place new tile
                    PlaceTile(gridCursorPos.x, gridCursorPos.y, true);
                }
                else if (inputMaster.Editor.Place.triggered)
                {
                    // Cycle tile of same type when click down
                    CycleTile(gridCursorPos.x, gridCursorPos.y, true, true);
                }
            }
            else if (inputMaster.Editor.Remove.ReadValue<float>() != 0f)
            {
                RemoveTile(gridCursorPos.x, gridCursorPos.y, true);
            }
        }

        // When the player is holding control
        if (inputMaster.Editor.Modifier.ReadValue<float>() != 0f)
        {
            if (inputMaster.Editor.Undo.triggered)
            {
                Undo();
            }
            else if (inputMaster.Editor.Redo.triggered)
            {
                Redo();
            }
            else if (inputMaster.Editor.Save.triggered)
            {
                fileManager.Save();
            }
        }

        // Swapping current block
        if (inputMaster.Editor.Next.triggered)
        {
            SwitchBlockType(true);
        }
        else if (inputMaster.Editor.Previous.triggered)
        {
            SwitchBlockType(false);
        }

        #endregion
    }

    #region Grid/World Conversion

    // Returns the closest tile to the cursor, relative to the bottom left of the grid (0, 0)
    Vector2Int WorldToGrid(float x, float y)
    {
        // Where is the bottom left corner of the edit zone?
        Vector2 worldGridMinPos = buildZoneOffset - ((Vector2)buildZoneSize / 2f);

        // Which tile is the cursor currently over, in grid units?
        Vector2Int gridPos = Vector2Int.zero;
        gridPos.x = Mathf.FloorToInt(x - worldGridMinPos.x);
        gridPos.y = Mathf.FloorToInt(y - worldGridMinPos.y);

        return gridPos;
    }

    // Returns the world-space position of the centre of a given cell in the grid
    Vector2 GridToWorld(int x, int y)
    {
        // Where is the bottom left corner of the edit zone?
        Vector2 worldGridMinPos = buildZoneOffset - ((Vector2)buildZoneSize / 2f);

        // The centre of the tile in grid space
        Vector2 worldPos = new Vector2(x + 0.5f, y + 0.5f);

        // Now convert it to world space
        worldPos += worldGridMinPos;

        return worldPos;
    }

    #endregion

    #region Place/Remove/Cycle/Switch Tile

    // Place a tile at this position in the grid
    void PlaceTile(int x, int y, bool directAction)
    {
        // Don't allow blocks to be placed on top of each other when they are of identical type
        if (sampleData[x, y].blockType == currentBlockType)
            return;

        // Don't allow editing the default dataset
        if (fileManager.currentDataset == 0)
            return;

        if (directAction)
        {
            // Pan audio left/right based on x position in the grid (cool Animal Crossing audio technique!)
            // https://www.youtube.com/watch?v=A4mN7CsAys8 (video referencing this)

            float horizontalAmount = (x + 1f) / buildZoneSize.x;
            float stereoValue = Mathf.Lerp(-1f, 1f, horizontalAmount);
            editorAudio.PlayOneshotStereo(EditorAudio.EditorSounds.Place, stereoValue);

            UndoAdd(x, y, true, EditorAction.ActionType.Place, sampleData[x, y].blockType);
        }

        // Set the new block type
        sampleData[x, y].blockType = currentBlockType;
        unsavedChanges = true;

        // Recalculate all tiles and replace them
        UpdateTiles();
    }

    // Ensure there is no tile at this position in the grid
    void RemoveTile(int x, int y, bool directAction)
    {
        // Don't allow the player to delete a block which is already deleted
        if (sampleData[x, y].blockType == BlockType.None)
            return;

        // Don't allow editing the default dataset
        if (fileManager.currentDataset == 0)
            return;

        if (directAction)
        {
            // Pan audio left/right based on x position in the grid (cool Animal Crossing audio technique!)
            // https://www.youtube.com/watch?v=A4mN7CsAys8 (video referencing this)

            float horizontalAmount = (x + 1f) / buildZoneSize.x;
            float stereoValue = Mathf.Lerp(-1f, 1f, horizontalAmount);
            editorAudio.PlayOneshotStereo(EditorAudio.EditorSounds.Delete, stereoValue);

            UndoAdd(x, y, true, EditorAction.ActionType.Remove, sampleData[x, y].blockType);
        }

        // Reset the block type to none
        sampleData[x, y].blockType = BlockType.None;
        unsavedChanges = true;

        // Recalculate all tiles
        UpdateTiles();
    }

    // Cycles forward through possible tile variations
    void CycleTile(int x, int y, bool directAction, bool forward)
    {
        // Don't allow editing the default dataset
        if (fileManager.currentDataset == 0)
            return;

        // When the tile is changed manually
        sampleData[x, y].tileOffset += (forward ? 1 : -1);

        // When a tile which can be offset is clicked on
        if (directAction)
        {
            // Only play sound when change is actually made
            if (sampleData[x, y].usingOffset)
            {
                // Pan audio left/right based on x position in the grid (cool Animal Crossing audio technique!)
                // https://www.youtube.com/watch?v=A4mN7CsAys8 (video referencing this)

                float horizontalAmount = (x + 1f) / buildZoneSize.x;
                float stereoValue = Mathf.Lerp(-1f, 1f, horizontalAmount);
                editorAudio.PlayOneshotStereo(EditorAudio.EditorSounds.Toggle, stereoValue);
                
                UndoAdd(x, y, true, EditorAction.ActionType.CycleForward, sampleData[x, y].blockType);
                
                unsavedChanges = true;
            }
        }

        // Recalculate all tiles
        UpdateTiles();
    }

    // Goes to the next or the previous block type in the set
    public void SwitchBlockType(bool next)
    {
        // Increment enum as int
        int currentIndex = (int)currentBlockType;
        currentIndex += (next ? 1 : -1);

        // Wrap around if below min or above max ("None" is not an option, as this would be the same as deletion)
        if (currentIndex < 1)
            currentIndex = (int)BlockType.Count - 1;
        if (currentIndex > (int)BlockType.Count - 1)
            currentIndex = 1;

        // Set new current block type
        currentBlockType = (BlockType)currentIndex;
        editorAudio.PlayOneshot(EditorAudio.EditorSounds.Toggle);
    }

    #endregion

    #region Undo/Redo

    // Adds an action to the undo list
    void UndoAdd(int x, int y, bool newAction, EditorAction.ActionType actionType, BlockType previousType)
    {
        EditorAction editorAction = new EditorAction();

        // Where the action took place
        editorAction.position.x = x;
        editorAction.position.y = y;

        // What the data at this position contained
        editorAction.tileData.tileIndex = sampleData[x, y].tileIndex;
        editorAction.tileData.blockType = sampleData[x, y].blockType;
        editorAction.tileData.tileOffset = sampleData[x, y].tileOffset;
        editorAction.tileData.usingOffset = sampleData[x, y].usingOffset;
        editorAction.actionType = actionType;
        editorAction.previousType = previousType;

        undoList.Add(editorAction);

        // Keep undo list at or below the max length
        while (undoList.Count > undoStackSize)
        {
            undoList.RemoveAt(0);
        }

        // Clear redo list when actions are added
        if (newAction) redoList.Clear();
    }

    // Reverts to previous grid state
    // Note: Tile actions are only recorded when a tile is placed or deleted manually
    public void Undo()
    {
        // Only allow undo on non-default samples when list is not empty
        if (undoList.Count <= 0 || fileManager.currentDataset == 0)
        {
            editorAudio.PlayOneshot(EditorAudio.EditorSounds.Denied);
            return;
        }
        
        unsavedChanges = true;

        EditorAction undoAction = undoList[undoList.Count - 1];

        // Add to the redo list
        RedoAdd(undoAction.position.x, undoAction.position.y, undoAction.actionType, undoAction.previousType);

        // Undo the previous tile action (add or remove)
        if (undoAction.actionType == EditorAction.ActionType.CycleForward)
        {
            CycleTile(undoAction.position.x, undoAction.position.y, false, false);
        }
        else if (undoAction.actionType == EditorAction.ActionType.CycleBackward)
        {
            CycleTile(undoAction.position.x, undoAction.position.y, false, true);
        }
        else if (undoAction.actionType == EditorAction.ActionType.Place)
        {
            RemoveTile(undoAction.position.x, undoAction.position.y, false);

            // Replace tile
            if (undoAction.previousType != BlockType.None)
            {
                BlockType typeBeforeUndo = currentBlockType;

                currentBlockType = undoAction.previousType;
                PlaceTile(undoAction.position.x, undoAction.position.y, false);

                currentBlockType = typeBeforeUndo;
            }
        }
        else
        {
            BlockType typeBeforeUndo = currentBlockType;

            currentBlockType = undoAction.tileData.blockType;
            PlaceTile(undoAction.position.x, undoAction.position.y, false);

            currentBlockType = typeBeforeUndo;
        }

        undoList.RemoveAt(undoList.Count - 1);

        editorAudio.PlayOneshot(EditorAudio.EditorSounds.Toggle);
        UpdateTiles();
    }

    // Adds an action to the redo list
    void RedoAdd(int x, int y, EditorAction.ActionType actionType, BlockType previousType)
    {
        EditorAction editorAction = new EditorAction();

        // Where the action took place
        editorAction.position.x = x;
        editorAction.position.y = y;

        // What the data at this position contained
        editorAction.tileData.tileIndex = sampleData[x, y].tileIndex;
        editorAction.tileData.blockType = sampleData[x, y].blockType;
        editorAction.tileData.tileOffset = sampleData[x, y].tileOffset;
        editorAction.tileData.usingOffset = sampleData[x, y].usingOffset;
        editorAction.actionType = actionType;
        editorAction.previousType = previousType;

        redoList.Add(editorAction);

        // Keep undo list at or below the max length
        while (redoList.Count > undoStackSize)
        {
            redoList.RemoveAt(0);
        }
    }

    // Reverts to state before undo-ing an action
    // Note: If a tile is manually placed or removed since the undo, then this is no longer possible
    public void Redo()
    {
        // Only allow redo on non-default samples when list is not empty
        if (redoList.Count <= 0 || fileManager.currentDataset == 0)
        {
            editorAudio.PlayOneshot(EditorAudio.EditorSounds.Denied);
            return;
        }

        unsavedChanges = true;

        // Add back to the undo list
        EditorAction redoAction = redoList[redoList.Count - 1];
        UndoAdd(redoAction.position.x, redoAction.position.y, false, redoAction.actionType, redoAction.previousType);

        // Redo the previous tile action
        if (redoAction.actionType == EditorAction.ActionType.CycleForward)
        {
            CycleTile(redoAction.position.x, redoAction.position.y, false, true);
        }
        else if (redoAction.actionType == EditorAction.ActionType.CycleBackward)
        {
            CycleTile(redoAction.position.x, redoAction.position.y, false, false);
        }
        else if (redoAction.actionType == EditorAction.ActionType.Place)
        {
            BlockType typeBeforeRedo = currentBlockType;

            currentBlockType = redoAction.tileData.blockType;
            PlaceTile(redoAction.position.x, redoAction.position.y, false);

            currentBlockType = typeBeforeRedo;
        }
        else
        {
            RemoveTile(redoAction.position.x, redoAction.position.y, false);
        }

        redoList.RemoveAt(redoList.Count - 1);

        editorAudio.PlayOneshot(EditorAudio.EditorSounds.Toggle);
        UpdateTiles();
    }

    #endregion

    #region Neighbour Analysis

    // For a given space, determine which of its neighbours share its type and which are different
    TileNeighbours EvaluateTileNeighbours(int x, int y, BlockType searchType)
    {
        // Uses a byte to represent flags of surrounding tiles
        // Ordered left to right, bottom to top
        TileNeighbours neighbours = new TileNeighbours();
        neighbours = 0; // Reset all flags

        // Loop over neighbouring cells
        int neighbourIndex = 0;
        for (int yOffset = 1; yOffset >= -1; yOffset--)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                // This is the tile we are meant to be looking *around*, so skip it
                if (xOffset == 0 && yOffset == 0)
                    continue;

                // If x and y are within range
                // Record neighbours block type
                // Else treat as (wall/air)?
                // If neighbour and centre types are same, then true, otherwise false

                BlockType neighbourType = BlockType.None;

                // If within the bounds of the sample
                if ((x + xOffset >= 0 && x + xOffset < buildZoneSize.x) && (y + yOffset >= 0 && y + yOffset < buildZoneSize.y))
                {
                    // Record neighbour block type
                    neighbourType = sampleData[x + xOffset, y + yOffset].blockType;
                }
                else
                {
                    // "Treat border as wall" setting only matters for solid walls
                    if (searchType == BlockType.Solid)
                    {
                        // Set border as wall or air
                        neighbourType = (treatBorderAsWall ? BlockType.Solid : BlockType.None);
                    }
                }

                if (neighbourType == searchType)
                {
                    // Types are matching, set the appropriate ones as true, the rest remain false
                    // 7 - neighbourIndex so that bits are shifted more and more as we get to the bottom right of the neighbours
                    neighbours |= (TileNeighbours)(1 << 7 - neighbourIndex);
                }

                neighbourIndex++;
            }
        }

        return neighbours;
    }

    #endregion

    #region Tile Determination

    // Given a block type and it's surroundings, returns the appropriate tile index
    int GetTileIndexFromNeighbours(int x, int y, BlockType blockType)
    {
        // Refer to Assets>Sprites>TilesetReference.png for guide to tile IDs
        
        // Assume offset is not used, set back to true below if using
        sampleData[x, y].usingOffset = false;

        // Empty tiles are not adapted
        if (blockType == BlockType.None) return 48;

        // Wall tiles only care about other wall tiles surrounding them
        if (blockType == BlockType.Solid)
        {
            // Find surrounding wall tiles
            byte solidNeighbours = (byte)EvaluateTileNeighbours(x, y, BlockType.Solid);

            // Check neighbours circumstance against the possible permutations

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_000_01_010, 0b_010_11_011)) return 0;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_000_10_010, 0b_010_11_110)) return 1;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_01_000, 0b_011_11_010)) return 2;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_10_000, 0b_110_11_010)) return 3;

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_000_00_010, 0b_010_11_010)) return 4;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_000_10_000, 0b_010_11_010)) return 5;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_000_01_000, 0b_010_11_010)) return 6;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_00_000, 0b_010_11_010)) return 7;

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_000_01_011, 0b_010_11_011)) return 8;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_000_10_110, 0b_010_11_110)) return 9;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_011_01_000, 0b_011_11_010)) return 10;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_110_10_000, 0b_110_11_010)) return 11;

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_000_11_010, 0b_010_11_111)) return 12;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_10_010, 0b_110_11_110)) return 13;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_01_010, 0b_011_11_011)) return 14;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_11_000, 0b_111_11_010)) return 15;

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_111_11_010, 0b_111_11_111)) return 16;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_011_11_011, 0b_111_11_111)) return 17;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_110_11_110, 0b_111_11_111)) return 18;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_11_111, 0b_111_11_111)) return 19;

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_11_011, 0b_111_11_111)) return 20;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_11_110, 0b_111_11_111)) return 21;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_011_11_010, 0b_111_11_111)) return 22;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_110_11_010, 0b_111_11_111)) return 23;

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_01_011, 0b_011_11_011)) return 24;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_10_110, 0b_110_11_110)) return 25;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_011_01_010, 0b_011_11_011)) return 26;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_110_10_010, 0b_110_11_110)) return 27;

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_000_11_011, 0b_010_11_111)) return 28;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_000_11_110, 0b_010_11_111)) return 29;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_011_11_000, 0b_111_11_010)) return 30;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_110_11_000, 0b_111_11_010)) return 31;

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_111_11_110, 0b_111_11_111)) return 32;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_111_11_011, 0b_111_11_111)) return 33;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_110_11_111, 0b_111_11_111)) return 34;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_011_11_111, 0b_111_11_111)) return 35;

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_111_11_000, 0b_111_11_010)) return 36;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_011_01_011, 0b_011_11_011)) return 37;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_110_10_110, 0b_110_11_110)) return 38;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_000_11_111, 0b_010_11_111)) return 39;

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_000_11_000, 0b_010_11_010)) return 40;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_00_010, 0b_010_11_010)) return 41;

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_011_11_110, 0b_111_11_111)) return 42;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_110_11_011, 0b_111_11_111)) return 43;

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_11_010, 0b_111_11_111)) return 44;

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_111_11_111, 0b_111_11_111)) return GetOffsetTile(x, y, 45, 46);

            if (CompareSurroundingToCriteria(solidNeighbours, 0b_000_00_000, 0b_010_11_010)) return 47;

            // Default tile for this type
            return 47;
        }

        // Coins are not adapted
        if (blockType == BlockType.Coin) return 49;

        // Foliages are not adapted, but they are randomised
        if (blockType == BlockType.Foliage)
        {
            return GetOffsetTile(x, y, 50, 55);
        }

        // Vines only care about other vines below them
        if (blockType == BlockType.Vine)
        {
            // Find surrounding vine tiles
            byte vineNeighbours = (byte)EvaluateTileNeighbours(x, y, BlockType.Vine);

            // Check neighbours circumstance against the possible permutations

            if (CompareSurroundingToCriteria(vineNeighbours, 0b_000_00_010, 0b_000_00_010)) return 56;
            if (CompareSurroundingToCriteria(vineNeighbours, 0b_000_00_000, 0b_000_00_010)) return 57;

            // Default tile for this type
            return 56;
        }

        // Signs only care about other signs above them
        if (blockType == BlockType.Sign)
        {
            // Find surrounding sign tiles
            byte signNeighbours = (byte)EvaluateTileNeighbours(x, y, BlockType.Sign);

            // Check neighbours circumstance against the possible permutations

            if (CompareSurroundingToCriteria(signNeighbours, 0b_010_00_000, 0b_010_00_000)) return 58;
            if (CompareSurroundingToCriteria(signNeighbours, 0b_000_00_000, 0b_010_00_000)) return GetOffsetTile(x, y, 59, 60);

            // Default tile for this type
            return 59;
        }

        // Spikes care about walls and platforms above and below them
        if (blockType == BlockType.Spike)
        {
            // Find surrounding wall and platform tiles
            byte solidNeighbours = (byte)EvaluateTileNeighbours(x, y, BlockType.Solid);
            byte onewayNeighbours = (byte)EvaluateTileNeighbours(x, y, BlockType.OneWay);

            // Check neighbours circumstance against the possible permutations

            if (CompareSurroundingToCriteria((byte)(solidNeighbours | onewayNeighbours), 0b_000_00_010, 0b_000_00_010)) return 61;
            if (CompareSurroundingToCriteria(solidNeighbours, 0b_010_00_000, 0b_010_00_000)) return 62;

            // Default tile for this type
            return 61;
        }

        // Torches are not adapted
        if (blockType == BlockType.Torch) return 63;

        // Ladders care about solids, platforms and other ladders
        if (blockType == BlockType.Ladder)
        {
            // Find surrounding solid, platform and ladder tiles
            byte solidNeighbours = (byte)EvaluateTileNeighbours(x, y, BlockType.Solid);
            byte onewayNeighbours = (byte)EvaluateTileNeighbours(x, y, BlockType.OneWay);
            byte ladderNeighbours = (byte)EvaluateTileNeighbours(x, y, BlockType.Ladder);

            // Check neighbours circumstance against the possible permutations

            if (CompareSurroundingToCriteria((byte)(solidNeighbours | onewayNeighbours), 0b_000_00_010, 0b_000_00_010)) return 64;
            if (CompareSurroundingToCriteria((byte)(solidNeighbours | ladderNeighbours), 0b_010_00_010, 0b_010_00_010)) return 65;
            if (CompareSurroundingToCriteria(ladderNeighbours, 0b_000_00_010, 0b_000_00_010)) return 66;
            if (CompareSurroundingToCriteria(ladderNeighbours, 0b_010_00_000, 0b_010_00_000)) return 67;

            // Default tile for this type
            return 65;
        }

        // OneWays care about solids and other oneways
        if (blockType == BlockType.OneWay)
        {
            // Find surrounding wall and platform tiles
            byte solidNeighbours = (byte)EvaluateTileNeighbours(x, y, BlockType.Solid);
            byte onewayNeighbours = (byte)EvaluateTileNeighbours(x, y, BlockType.OneWay);

            // Check neighbours circumstance against the possible permutations

            if (CompareSurroundingToCriteria((byte)(solidNeighbours | onewayNeighbours), 0b_000_01_000, 0b_000_11_000)) return 68;
            if (CompareSurroundingToCriteria((byte)(solidNeighbours | onewayNeighbours), 0b_000_11_000, 0b_000_11_000)) return 69;
            if (CompareSurroundingToCriteria((byte)(solidNeighbours | onewayNeighbours), 0b_000_10_000, 0b_000_11_000)) return 70;

            // Default tile for this type
            return 69;
        }

        // Default fail state, something is probably cooked
        return 71;
    }

    // Returns the appropriate tile, taking into account the offset, also ensures that tileOffset is kept within range
    int GetOffsetTile(int x, int y, int start, int end)
    {
        sampleData[x, y].usingOffset = true;

        if (sampleData[x, y].tileOffset < 0)
            sampleData[x, y].tileOffset = (end - start);
        if (sampleData[x, y].tileOffset > (end - start))
            sampleData[x, y].tileOffset = 0;

        return start + sampleData[x, y].tileOffset;
    }

    /// <summary>
    /// Compares the surrounding tiles to a given template, and which spaces in the template are required
    /// </summary>
    /// <param name="instance">The surroundings of a tile</param>
    /// <param name="template">An example of valid surroundings to fit the template</param>
    /// <param name="required">Which surrounding tiles in the template are required to be matched</param>
    /// <returns></returns>
    bool CompareSurroundingToCriteria(byte instance, byte template, byte required)
    {
        // All bytes in here represent neighbouring cells from left to right, then top to bottom, like reading a book

        // Differences between the instance and the template (1 = different)
        byte instanceDifferences = (byte)(instance ^ template);

        // Similarities between the instance and the template (1 = matching)
        byte instanceSimilarities = (byte)(~instanceDifferences);

        // The required similarities which are met
        byte criteriasMet = (byte)(instanceSimilarities & required);

        // Whether all of the required similarities are met
        return (criteriasMet == required);
    }

    // Determine all tile sprites to use, given the arrangement of various types
    void DetermineAllTiles()
    {
        // Iterate for each tile
        for (int y = 0; y < buildZoneSize.y; y++)
        {
            for (int x = 0; x < buildZoneSize.x; x++)
            {
                // Set the tile to use given neighbours at this point
                int tileID = GetTileIndexFromNeighbours(x, y, sampleData[x, y].blockType);
                sampleData[x, y].tileIndex = tileID;
            }
        }
    }

    #endregion

    #region Tile Placement

    // Wipes the grid and replaces all tiles
    void SetAllTiles()
    {
        tilemap.ClearAllTiles();

        // Iterate over every tile in the grid
        for (int y = 0; y < buildZoneSize.y; y++)
        {
            for (int x = 0; x < buildZoneSize.x; x++)
            {
                // Place the tile
                Vector3Int position = new Vector3Int(x, y, 0);
                tilemap.SetTile(position, tileCollection.tiles[sampleData[x, y].tileIndex]);
            }
        }
    }

    public void UpdateTiles()
    {
        // Figure out which tiles should be used
        DetermineAllTiles();

        // Place the tiles on the grid
        SetAllTiles();
    }

    #endregion
    
    // Resets sample data to start from fresh
    public void ResetSample()
    {
        // Iterate over every tile
        for (int y = 0; y < buildZoneSize.y; y++)
        {
            for (int x = 0; x < buildZoneSize.x; x++)
            {
                sampleData[x, y].blockType = BlockType.None;
                sampleData[x, y].tileIndex = 0;
                sampleData[x, y].tileOffset = 0;
                sampleData[x, y].usingOffset = false;
            }
        }

        currentBlockType = (BlockType)1;
        undoList.Clear();
        redoList.Clear();

        // Reload visuals
        UpdateTiles();
    }

    #region Other

    public void OpenTutorial()
    {
        editorAudio.PlayOneshot(EditorAudio.EditorSounds.Positive);

        // This is a total stopgap right now, but it let me cut down on the time to develop an in-engine tutorial for the editor
        Application.OpenURL("https://prometheus-76.github.io");
    }

    public void ReturnToMenu()
    {
        editorAudio.PlayOneshot(EditorAudio.EditorSounds.Negative);
        buttonAudioSource.canBeDestroyed = true;
        SceneManager.LoadScene(0);
    }

    #endregion

    #region Input System

    private void OnEnable()
    {
        inputMaster.Enable();
    }

    private void OnDisable()
    {
        inputMaster.Disable();
    }

    #endregion
}
