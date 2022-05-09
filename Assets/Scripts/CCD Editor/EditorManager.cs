using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Interfaces with the user, allowing them to build templates
// ...it also handles determining which sprites to show given neighbouring tiles
public class EditorManager : MonoBehaviour
{
    #region Variables

    #region Data Structures

    // Every distinct type of tile which can appear in the game
    public enum BlockType
    {
        None,
        Solid,
        OneWay,
        Spike,
        Ladder,
        Coin,
        Vine,
        Grass,
        Flower,
        Count
    }

    // Represents each space in the grid
    public struct TileData
    {
        public BlockType blockType;
        public int tileIndex; // This is only used for tiles like grass that have variations but don't affect gameplay
    }

    // For undo/redo stack
    public struct EditorAction
    {
        public Vector2Int position;
        public TileData newTile;
    }

    // For determining sprite based on surroundings
    [System.Flags]
    public enum TileNeighbours : byte
    {
        TL,
        TM,
        TR,
        ML,
        MR,
        BL,
        BM,
        BR
    }

    #endregion

    [Header("Build Settings")]
    public Vector2Int buildZoneSize;
    public Vector2 buildZoneOffset;
    public bool treatBorderAsWall;
    public int undoStackSize;

    [Header("Configuration")]
    public Tile[] tiles;

    [Header("Components")]
    public FileManager fileManager;
    public EditorAudio editorAudio;

    [HideInInspector] public TileData[,] sampleData;
    [HideInInspector] public BlockType currentBlockType;

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
    }

    void Start()
    {

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

        // If the cursor should be able to interact with things
        if (cursorWithinGrid)
        {
            if (inputMaster.Editor.Place.ReadValue<float>() != 0f)
            {
                PlaceTile(gridCursorPos.x, gridCursorPos.y);
            }
            else if (inputMaster.Editor.Remove.ReadValue<float>() != 0f)
            {
                RemoveTile(gridCursorPos.x, gridCursorPos.y);
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

    #region Place/Remove Tile

    // Place a tile at this position in the grid
    void PlaceTile(int x, int y)
    {
        // Don't allow blocks to be placed on top of each other when they are of identical type
        if (sampleData[x, y].blockType == currentBlockType)
            return;

        // Pan audio left/right based on x position in the grid (cool Animal Crossing audio technique!)
        // https://www.youtube.com/watch?v=A4mN7CsAys8 (video referencing this)

        float horizontalAmount = (x + 1f) / buildZoneSize.x;
        float stereoValue = Mathf.Lerp(-1f, 1f, horizontalAmount);
        editorAudio.PlayOneshotStereo(EditorAudio.OneshotSounds.Place, stereoValue);

        // Set the new block type
        sampleData[x, y].blockType = currentBlockType;

        // Recalculate all tiles and replace them
    }

    // Ensure there is no tile at this position in the grid
    void RemoveTile(int x, int y)
    {
        // Don't allow the player to delete a block which is already deleted
        if (sampleData[x, y].blockType == BlockType.None)
            return;

        // Pan audio left/right based on x position in the grid (cool Animal Crossing audio technique!)
        // https://www.youtube.com/watch?v=A4mN7CsAys8 (video referencing this)

        float horizontalAmount = (x + 1f) / buildZoneSize.x;
        float stereoValue = Mathf.Lerp(-1f, 1f, horizontalAmount);
        editorAudio.PlayOneshotStereo(EditorAudio.OneshotSounds.Delete, stereoValue);

        // Reset the block type to none
        sampleData[x, y].blockType = BlockType.None;

        // Remove tile at this position
        // Recalculate all tiles
    }

    #endregion

    #region Undo/Redo

    // Reverts to previous grid state
    // Note: Tile actions are only recorded when a tile is placed or deleted manually
    public void Undo()
    {
        // Only allow undo when list is not empty
        if (undoList.Count <= 0)
        {
            editorAudio.PlayOneshot(EditorAudio.OneshotSounds.Denied);
            return;
        }

        editorAudio.PlayOneshot(EditorAudio.OneshotSounds.Toggle);
    }

    // Reverts to state before undo-ing an action
    // Note: If a tile is manually placed or removed since the undo, then this is no longer possible
    public void Redo()
    {
        // Only allow redo when list is not empty
        if (redoList.Count <= 0)
        {
            editorAudio.PlayOneshot(EditorAudio.OneshotSounds.Denied);
            return;
        }

        editorAudio.PlayOneshot(EditorAudio.OneshotSounds.Toggle);
    }

    #endregion

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
        editorAudio.PlayOneshot(EditorAudio.OneshotSounds.Toggle);
    }

    #region Other

    public void OpenTutorial()
    {
        editorAudio.PlayOneshot(EditorAudio.OneshotSounds.Positive);

        // This is a total stopgap right now, but it let me cut down on the time to develop an in-engine tutorial for the editor
        Application.OpenURL("https://prometheus-76.github.io");
    }

    public void ReturnToMenu()
    {
        editorAudio.PlayOneshot(EditorAudio.OneshotSounds.Negative);
        
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
