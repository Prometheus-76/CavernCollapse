using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

// Generates the data of the level and coordinates all the steps required for the process
public class LevelGenerator : MonoBehaviour
{
    #region Structs

    // Represents one tile in the level
    struct LevelTile
    {
        // Whether a tile is reserved for the critical path through a room
        public bool reservedTile;

        // Used by flood fill algorithm, represents whether floodfill has hit this tile
        public bool marked;

        public int[] possibleTiles;
        public BlockType blockType;
        public int tileIndex;
    }

    // Represents a room within the level
    struct LevelRoom
    {
        public enum RoomType
        {
            Unassigned,
            Hallway,
            Dropdown,
            Landing,
            Spawn,
            Exit
        }

        public LevelTile[,] tiles;

        // Variable between stages
        public bool reservedRoom;
        public RoomType roomType;

        public int verticalAccessMin; // Represents the left-most column of any gaps in the ceiling or floor used for room transitions
        public int verticalAccessMax; // Represents the right-most column of any gaps in the ceiling or floor used for room transitions
    }

    #endregion

    #region Variables

    enum GenerationStep
    {
        ResetStageData,
        CreateStageName,
        LoadDataset,
        ConstructRuleset,
        AssembleRoomSequence,
        ReserveRoomPaths,
        CreateMapBorder,
        ConnectVerticalRooms,
        CreateRoomBorders,
        WaveFunctionCollapseWalls,
        CleanupWalls,
        PlaceBonusCrates,
        WaveFunctionCollapsePlatforming,
        CleanupLadders,
        CleanupPlatforms,
        WaveFunctionCollapseGameplay,
        ReconnectGameplay,
        CleanupCoins,
        WaveFunctionCollapseDeco,
        ReconnectDeco,
        CleanupDeco,
        PlaceDoors,
        CleanupDoors,
        RedirectSigns,
        VerifyPaths,
        FillEmptyAreas,
        ConfigureStage,
        SubstitutePrefabs,
        GenerateColliders,
        GenerationComplete
    }

    private GenerationStep currentStep;
    private float mainProgress;
    [HideInInspector] public float stepProgress;
    private bool awaitingNewStep;
    private int seed;
    private Vector2Int spawnPosition;
    private Vector2Int exitPosition;
    private Stopwatch stopwatch;

    private int totalCoins;

    [Header("Configuration")]
    [SerializeField, Tooltip("The target length of each frame during generation")] private float maxTimePerFrame;
    [SerializeField, Tooltip("The dimensions of the stage (in rooms)")] private Vector2Int stageSize;
    [SerializeField, Tooltip("The dimensions of each room (in tiles)")] private Vector2Int roomSize;
    [SerializeField, Tooltip("The potential adjectives used in the first word of the level name")] private string[] firstWords;
    [SerializeField, Tooltip("The potential nouns used in the second word of the level name")] private string[] secondWords;

    [Header("Components")]
    public TileCollection tileCollection;
    public GameplayConfiguration gameplayConfiguration;
    public LoadingScreen loadingScreen;
    public DatasetAnalyser datasetAnalyser;
    public LevelTileManager levelTileManager;
    public WaveFunctionCollapse waveFunctionCollapse;
    public DijkstraPathfinding dijkstraPathfinding;
    public CameraController cameraController;
    public GameplayUI gameplayUI;
    public LevelManager levelManager;

    #region Private

    private LevelRoom[,] level;
    private List<Vector2Int> criticalPath;

    #endregion

    #endregion

    // Awake is called when the script instance is loaded
    void Awake()
    {
        criticalPath = new List<Vector2Int>();
        stopwatch = new Stopwatch();

        // Allocate and initialise the level, all rooms and all default tiles within those rooms
        InitialiseLevel();
    }

    void Start()
    {
        StartCoroutine(GenerateLevel());
    }

    void Update()
    {
        UpdateLoadingUI();
    }
    
    // Updates the loading screen UI
    void UpdateLoadingUI()
    {
        // Update loading bar
        stepProgress = Mathf.Clamp01(stepProgress);
        mainProgress = (float)currentStep / (float)GenerationStep.GenerationComplete;
        mainProgress += (1f / (float)GenerationStep.GenerationComplete) * stepProgress;

        loadingScreen.SetMainProgress(mainProgress);

        // Update loading step flavour text
        switch (currentStep)
        {
            case GenerationStep.ResetStageData:
                loadingScreen.SetStepText("Resetting stage data...");
                break;
            case GenerationStep.CreateStageName:
                loadingScreen.SetStepText("Creating stage name...");
                break;
            case GenerationStep.LoadDataset:
                loadingScreen.SetStepText("Loading dataset samples...");
                break;
            case GenerationStep.ConstructRuleset:
                loadingScreen.SetStepText("Constructing placement ruleset...");
                break;
            case GenerationStep.AssembleRoomSequence:
                loadingScreen.SetStepText("Assembling room sequence...");
                break;
            case GenerationStep.ReserveRoomPaths:
                loadingScreen.SetStepText("Reserving room paths...");
                break;
            case GenerationStep.CreateMapBorder:
                loadingScreen.SetStepText("Allocating map border...");
                break;
            case GenerationStep.ConnectVerticalRooms:
                loadingScreen.SetStepText("Connecting vertical rooms...");
                break;
            case GenerationStep.CreateRoomBorders:
                loadingScreen.SetStepText("Allocating room borders...");
                break;
            case GenerationStep.WaveFunctionCollapseWalls:
                loadingScreen.SetStepText("Building cavern walls...");
                break;
            case GenerationStep.CleanupWalls:
                loadingScreen.SetStepText("Polishing surfaces...");
                break;
            case GenerationStep.PlaceBonusCrates:
                loadingScreen.SetStepText("Burying assorted crates...");
                break;
            case GenerationStep.WaveFunctionCollapsePlatforming:
                loadingScreen.SetStepText("Constructing scaffolding...");
                break;
            case GenerationStep.CleanupLadders:
                loadingScreen.SetStepText("Adjusting ladders...");
                break;
            case GenerationStep.CleanupPlatforms:
                loadingScreen.SetStepText("Checking platforms...");
                break;
            case GenerationStep.WaveFunctionCollapseGameplay:
                loadingScreen.SetStepText("Setting challenges...");
                break;
            case GenerationStep.ReconnectGameplay:
                loadingScreen.SetStepText("Sharpening spikes...");
                break;
            case GenerationStep.CleanupCoins:
                loadingScreen.SetStepText("Collecting coins...");
                break;
            case GenerationStep.WaveFunctionCollapseDeco:
                loadingScreen.SetStepText("Growing foliage...");
                break;
            case GenerationStep.ReconnectDeco:
                loadingScreen.SetStepText("Nurturing soil...");
                break;
            case GenerationStep.CleanupDeco:
                loadingScreen.SetStepText("Pruning vegetation...");
                break;
            case GenerationStep.PlaceDoors:
                loadingScreen.SetStepText("Placing doors...");
                break;
            case GenerationStep.CleanupDoors:
                loadingScreen.SetStepText("Sweeping around doors...");
                break;
            case GenerationStep.RedirectSigns:
                loadingScreen.SetStepText("Adjusting sign posts...");
                break;
            case GenerationStep.VerifyPaths:
                loadingScreen.SetStepText("Verifying paths...");
                break;
            case GenerationStep.ConfigureStage:
                loadingScreen.SetStepText("Counting coins...");
                break;
            case GenerationStep.SubstitutePrefabs:
                loadingScreen.SetStepText("Replacing imposter tiles...");
                break;
            case GenerationStep.FillEmptyAreas:
                loadingScreen.SetStepText("Pumping oxygen into caverns...");
                break;
            case GenerationStep.GenerateColliders:
                loadingScreen.SetStepText("Generating colliders...");
                break;
            case GenerationStep.GenerationComplete:
                loadingScreen.SetStepText("Entering caverns...");
                break;
        }
    }

    #region Grid/Stage/Room Conversion

    Vector2Int StageToGrid(int stageX, int stageY, int roomX, int roomY)
    {
        Vector2Int result = Vector2Int.zero;

        result.x = stageX * roomSize.x + roomX;
        result.y = stageY * roomSize.y + roomY;

        return result;
    }

    Vector2Int GridToRoom(int x, int y)
    {
        Vector2Int result = Vector2Int.zero;

        result.x = x % roomSize.x;
        result.y = y % roomSize.y;

        return result;
    }

    Vector2Int GridToStage(int x, int y)
    {
        Vector2Int result = Vector2Int.zero;
        Vector2Int room = GridToRoom(x, y);

        result.x = (x - room.x) / roomSize.x;
        result.y = (y - room.y) / roomSize.y;

        return result;
    }

    #endregion

    #region Initialisation

    void InitialiseLevel()
    {
        // Create the level (an array of rooms)
        level = new LevelRoom[stageSize.x, stageSize.y];

        // ...and all rooms within it
        for (int y = 0; y < stageSize.y; y++)
        {
            for (int x = 0; x < stageSize.x; x++)
            {
                level[x, y] = new LevelRoom();

                InitialiseRoom(x, y);
            }
        }

        // Set variable level data to default state
        ResetStageData();
    }   
    
    void InitialiseRoom(int roomX, int roomY)
    {
        // Create this room
        level[roomX, roomY].tiles = new LevelTile[roomSize.x, roomSize.y];

        // ...and all tiles within that
        for (int y = 0; y < roomSize.y; y++)
        {
            for (int x = 0; x < roomSize.x; x++)
            {
                level[roomX, roomY].tiles[x, y] = new LevelTile();
                level[roomX, roomY].tiles[x, y].possibleTiles = new int[tileCollection.tiles.Length];
            }
        }
    }

    #endregion
    
    // Places a tile in the scene, and within all grids that track tiles
    void PlaceTile(int stageX, int stageY, int roomX, int roomY, int tileIndex, BlockType blockType)
    {        
        level[stageX, stageY].tiles[roomX, roomY].blockType = blockType;
        level[stageX, stageY].tiles[roomX, roomY].tileIndex = tileIndex;

        Vector2Int gridPosition = StageToGrid(stageX, stageY, roomX, roomY);
        levelTileManager.PlaceTileOfType(gridPosition.x, gridPosition.y, tileIndex, blockType);
        waveFunctionCollapse.SetTile(gridPosition.x, gridPosition.y, blockType, tileIndex);
    }

    // Removes a tile from all tilemaps at this position, and resets tile within all grid that track tiles
    void RemoveTile(int stageX, int stageY, int roomX, int roomY)
    {
        level[stageX, stageY].tiles[roomX, roomY].blockType = BlockType.None;
        level[stageX, stageY].tiles[roomX, roomY].tileIndex = -1;

        Vector2Int gridPosition = StageToGrid(stageX, stageY, roomX, roomY);
        levelTileManager.RemoveTile(gridPosition.x, gridPosition.y);
        waveFunctionCollapse.ResetTile(gridPosition.x, gridPosition.y);
    }

    #region Flood Fill

    // Recursively fills a space, while ignoring walls
    void FloodFill(int x, int y)
    {
        // Mark the cell
        Vector2Int stagePos = GridToStage(x, y);
        Vector2Int roomPos = GridToRoom(x, y);
        level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].marked = true;

        // Expand to valid neighbours
        for (int yOffset = 1; yOffset >= -1; yOffset--)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                // Skip centre tile
                if (xOffset == 0 && yOffset == 0)
                    continue;

                // ...and corners
                if (Mathf.Abs(xOffset) == 1 && Mathf.Abs(yOffset) == 1)
                    continue;

                // Skip the tile if its out of range of the grid
                if (x + xOffset < 0 || x + xOffset >= stageSize.x * roomSize.x || y + yOffset < 0 || y + yOffset >= stageSize.y * roomSize.y)
                    continue;

                // Expand flood fill to this tile, if it hasn't already
                Vector2Int neighbourStagePos = GridToStage(x + xOffset, y + yOffset);
                Vector2Int neighbourRoomPos = GridToRoom(x + xOffset, y + yOffset);
                if (level[neighbourStagePos.x, neighbourStagePos.y].tiles[neighbourRoomPos.x, neighbourRoomPos.y].marked == false &&
                    level[neighbourStagePos.x, neighbourStagePos.y].tiles[neighbourRoomPos.x, neighbourRoomPos.y].blockType != BlockType.Solid)
                    FloodFill(x + xOffset, y + yOffset);

            }
        }
    }

    // Ensures all cells are unmarked
    void ResetFloodFill()
    {
        // For every room...
        for (int stageY = 0; stageY < stageSize.y; stageY++)
        {
            for (int stageX = 0; stageX < stageSize.x; stageX++)
            {
                // ...and every tile in that room
                for (int roomY = 0; roomY < roomSize.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.x; roomX++)
                    {
                        level[stageX, stageY].tiles[roomX, roomY].marked = false;
                    }
                }
            }
        }
    }

    #endregion

    // Called whenever a step is completed
    public void CompleteStep()
    {
        // Move to next step
        int newStep = (int)currentStep + 1;
        newStep = Mathf.Clamp(newStep, 0, (int)GenerationStep.GenerationComplete);
        currentStep = (GenerationStep)newStep;

        // When preparing for the next step
        if (currentStep != GenerationStep.GenerationComplete)
        {
            // Allow next step to occur
            awaitingNewStep = true;
            stepProgress = 0f;
        }
    }

    // Called when all generation steps are completed
    void GenerationCompleted()
    {
        // Play level music when generation is completed
        if (MusicPlayer.GetInstance() != null)
            MusicPlayer.GetInstance().CrossFade(2);

        levelManager.StageBegin(3, totalCoins);
    }

    // Responsible for calling coroutines sequentially to generate the level asynchronously
    IEnumerator GenerateLevel()
    {
        // Randomise the seed
        seed = Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(seed);

        // Start the stop watch
        stopwatch.Reset();
        stopwatch.Start();

        currentStep = (GenerationStep)0;
        awaitingNewStep = true;

        waveFunctionCollapse.InitialiseWaveFunction(stageSize.x * roomSize.x, stageSize.y * roomSize.y);
        dijkstraPathfinding.InitialiseDijkstraPath(stageSize.x * roomSize.x, stageSize.y * roomSize.y);

        // Run until the generation is completed
        while (currentStep != GenerationStep.GenerationComplete)
        {
            // Only start a new coroutine when the previous one is finished (resetting awaiting new step will do this)
            if (awaitingNewStep)
            {
                awaitingNewStep = false;

                // Find the next step to do
                switch (currentStep)
                {
                    case GenerationStep.ResetStageData:
                        StartCoroutine(ResetStageData());
                        break;
                    case GenerationStep.CreateStageName:
                        StartCoroutine(CreateStageName());
                        break;
                    case GenerationStep.LoadDataset:
                        StartCoroutine(datasetAnalyser.LoadDataset(gameplayConfiguration.dataset));
                        break;
                    case GenerationStep.ConstructRuleset:
                        StartCoroutine(datasetAnalyser.ConstructRuleset());
                        break;
                    case GenerationStep.AssembleRoomSequence:
                        StartCoroutine(AssembleRoomSequence());
                        break;
                    case GenerationStep.ReserveRoomPaths:
                        StartCoroutine(ReserveRoomPaths());
                        break;
                    case GenerationStep.CreateMapBorder:
                        StartCoroutine(CreateMapBorder());
                        break;
                    case GenerationStep.ConnectVerticalRooms:
                        StartCoroutine(ConnectVerticalRooms());
                        break;
                    case GenerationStep.CreateRoomBorders:
                        StartCoroutine(CreateRoomBorders());
                        break;
                    case GenerationStep.WaveFunctionCollapseWalls:
                        StartCoroutine(WaveFunctionCollapseWalls());
                        break;
                    case GenerationStep.CleanupWalls:
                        StartCoroutine(CleanupWalls());
                        break;
                    case GenerationStep.PlaceBonusCrates:
                        StartCoroutine(PlaceBonusCrates());
                        break;
                    case GenerationStep.WaveFunctionCollapsePlatforming:
                        StartCoroutine(WaveFunctionCollapsePlatforming());
                        break;
                    case GenerationStep.CleanupLadders:
                        StartCoroutine(CleanupLadders());
                        break;
                    case GenerationStep.CleanupPlatforms:
                        StartCoroutine(CleanupPlatforms());
                        break;
                    case GenerationStep.WaveFunctionCollapseGameplay:
                        StartCoroutine(WaveFunctionCollapseGameplay());
                        break;
                    case GenerationStep.ReconnectGameplay:
                        StartCoroutine(ReconnectGameplay());
                        break;
                    case GenerationStep.CleanupCoins:
                        StartCoroutine(CleanupCoins());
                        break;
                    case GenerationStep.WaveFunctionCollapseDeco:
                        StartCoroutine(WaveFunctionCollapseDeco());
                        break;
                    case GenerationStep.ReconnectDeco:
                        StartCoroutine(ReconnectDeco());
                        break;
                    case GenerationStep.CleanupDeco:
                        StartCoroutine(CleanupDeco());
                        break;
                    case GenerationStep.PlaceDoors:
                        StartCoroutine(PlaceDoors());
                        break;
                    case GenerationStep.CleanupDoors:
                        StartCoroutine(CleanupDoors());
                        break;
                    case GenerationStep.RedirectSigns:
                        StartCoroutine(RedirectSigns());
                        break;
                    case GenerationStep.VerifyPaths:
                        StartCoroutine(VerifyPaths());
                        break;
                    case GenerationStep.ConfigureStage:
                        StartCoroutine(ConfigureStage());
                        break;
                    case GenerationStep.SubstitutePrefabs:
                        StartCoroutine(SubstitutePrefabs());
                        break;
                    case GenerationStep.FillEmptyAreas:
                        StartCoroutine(FillEmptyAreas());
                        break;
                    case GenerationStep.GenerateColliders:
                        StartCoroutine(GenerateColliders());
                        break;
                }
            }

            yield return null;
        }

        GenerationCompleted();
        yield return null;
    }

    #region Level Generation Steps

    // Step 1 of level generation
    // Resets all local data within this script
    IEnumerator ResetStageData()
    {
        int iterationsCompleted = 0;

        // For every room...
        for (int stageY = 0; stageY < stageSize.y; stageY++)
        {
            for (int stageX = 0; stageX < stageSize.x; stageX++)
            {
                level[stageX, stageY].reservedRoom = false;
                level[stageX, stageY].roomType = LevelRoom.RoomType.Unassigned;
                level[stageX, stageY].verticalAccessMin = -1;
                level[stageX, stageY].verticalAccessMax = -1;

                // ...and every tile in that room
                for (int roomY = 0; roomY < roomSize.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.x; roomX++)
                    {
                        level[stageX, stageY].tiles[roomX, roomY].tileIndex = -1;
                        level[stageX, stageY].tiles[roomX, roomY].reservedTile = false;
                        level[stageX, stageY].tiles[roomX, roomY].marked = false;
                        level[stageX, stageY].tiles[roomX, roomY].blockType = BlockType.None;

                        iterationsCompleted++;
                        stepProgress = (float)iterationsCompleted / (stageSize.y * stageSize.x * roomSize.y * roomSize.x);
                        if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                        {
                            stopwatch.Restart();
                            yield return null;
                        }
                    }
                }

            }
        }
        
        criticalPath.Clear();
        totalCoins = 0;

        yield return null;
        CompleteStep();
    }

    // Step 2 of level generation
    // Creates a name for the level using the existing list of adjectives and nouns
    IEnumerator CreateStageName()
    {
        int firstWordChoice = Random.Range(0, firstWords.Length);
        int secondWordChoice = Random.Range(0, secondWords.Length);

        string levelName = "";
        levelName += firstWords[firstWordChoice];
        levelName += " ";
        levelName += secondWords[secondWordChoice];

        gameplayUI.currentStageName = levelName;

        yield return null;
        CompleteStep();
    }

    // Step 3 is done in DatasetAnalyser class
    // Step 4 is done in DatasetAnalyser class

    // Step 5 of level generation
    // Moves from the top of the stage to the bottom, placing rooms to the left and right as it goes down
    IEnumerator AssembleRoomSequence()
    {
        int levelsCreated = 0;

        criticalPath.Clear();

        // The x value of the room which will be the starting point of the level
        int startRoomIndex = Random.Range(0, stageSize.x);

        // Starting from the spawn room, develop the room order moving downwards
        Vector2Int currentRoom = new Vector2Int(startRoomIndex, stageSize.y - 1);
        level[currentRoom.x, currentRoom.y].roomType = LevelRoom.RoomType.Spawn;

        int moveDirection = 0;

        // Until we hit the floor of the stage
        while(currentRoom.y >= 0)
        {
            if (currentRoom.x <= 0)
            {
                // Must move right or down
                if (moveDirection == 0)
                {
                    // Go right
                    moveDirection = 1;
                }
                else
                {
                    // We hit a wall so go down
                    moveDirection = 0;
                }
            }
            else if (currentRoom.x >= stageSize.x - 1)
            {
                // Must move left or down
                if (moveDirection == 0)
                {
                    // Go left
                    moveDirection = -1;
                }
                else
                {
                    // We hit a wall so go down
                    moveDirection = 0;
                }
            }
            else
            {
                if (moveDirection != 0)
                {
                    // Chance to drop down, otherwise continue in same direction
                    moveDirection = Random.Range(0, Mathf.CeilToInt(stageSize.x / 2f) + 1) == 0 ? 0 : moveDirection;
                }
                else
                {
                    // Must have dropped down without hitting a wall
                    moveDirection = Random.Range(0, 2) == 0 ? -1 : 1;
                }
            }

            // Mark room as being on critical path
            level[currentRoom.x, currentRoom.y].reservedRoom = true;
            criticalPath.Add(currentRoom);

            // Move in the direction and connect a room
            if (moveDirection == 0)
            {
                // Go down

                // Mark as dropdown or exit
                if (currentRoom.y > 0)
                {
                    level[currentRoom.x, currentRoom.y].roomType = LevelRoom.RoomType.Dropdown;

                    currentRoom.y -= 1;

                    // Mark as landing
                    level[currentRoom.x, currentRoom.y].roomType = LevelRoom.RoomType.Landing;
                }
                else
                {
                    // We tried to go down on the bottom level, this must be the exit
                    level[currentRoom.x, currentRoom.y].roomType = LevelRoom.RoomType.Exit;
                    break;
                }

                levelsCreated++;
                stepProgress = (float)levelsCreated / stageSize.y;
                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
            else
            {
                // Move to the left or right

                // If unassigned, mark as hallway
                if (level[currentRoom.x, currentRoom.y].roomType == LevelRoom.RoomType.Unassigned)
                    level[currentRoom.x, currentRoom.y].roomType = LevelRoom.RoomType.Hallway;

                currentRoom.x += moveDirection;
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 6 of level generation
    // Builds a path through the room using a biased drunk walking algorithm
    IEnumerator ReserveRoomPaths()
    {
        int roomPathsCreated = 0;

        Vector2Int lastTile = Vector2Int.zero;
        int targetX = -1;

        // For every room in the level
        for (int r = 0; r < criticalPath.Count; r++)
        {
            int roomProgressDirection = 0; // The x direction passing through this room
            Vector2Int lastMoveDirection = Vector2Int.zero;

            // Find the direction of the room on the x axis
            if (r + 1 < criticalPath.Count && criticalPath[r + 1].x - criticalPath[r].x != 0)
            {
                // Check next room
                roomProgressDirection = criticalPath[r + 1].x - criticalPath[r].x;
            }
            else if (r - 1 >= 0)
            {
                // Check previous room
                roomProgressDirection = criticalPath[r].x - criticalPath[r - 1].x;
            }

            // If the current room requires a fresh start point for the algorithm
            if (level[criticalPath[r].x, criticalPath[r].y].roomType == LevelRoom.RoomType.Landing ||
                level[criticalPath[r].x, criticalPath[r].y].roomType == LevelRoom.RoomType.Spawn)
            {
                if (r + 1 < criticalPath.Count)
                {
                    // Set the start tile to the back of the room, furthest from the next one
                    if (roomProgressDirection == -1)
                        lastTile.x = roomSize.x - 1;
                    else
                        lastTile.x = 0;
                    
                    lastTile.y = Random.Range(2, roomSize.y - 2); // Inclusive, Exclusive

                    // The x value which, when reached, concludes the drunk walking algorithm
                    targetX = (roomSize.x - 1) - lastTile.x;
                }
            }

            // Mark the first tile as being on critical path
            level[criticalPath[r].x, criticalPath[r].y].tiles[lastTile.x, lastTile.y].reservedTile = true;

            // Drunk walking algorithm until the path trace inside the room is completed
            while (true)
            {
                // If the walker should change directions
                if (Random.Range(0, 3) <= 1 || lastMoveDirection == Vector2Int.zero)
                {
                    // Move in a new direction
                    int newDirection = Random.Range(-1, 2);
                    if (newDirection == 0)
                    {
                        // Go towards next room
                        lastMoveDirection.x = roomProgressDirection;
                        lastMoveDirection.y = 0;
                    }
                    else
                    {
                        // Go up/down
                        lastMoveDirection.x = 0;
                        lastMoveDirection.y = newDirection;
                    }
                }

                lastTile += lastMoveDirection;
                lastTile.y = Mathf.Clamp(lastTile.y, 2, roomSize.y - 3);

                // Mark the tile as being on critical path
                level[criticalPath[r].x, criticalPath[r].y].tiles[lastTile.x, lastTile.y].reservedTile = true;

                // If the room path has completely finished being drawn
                if (lastTile.x == targetX)
                {
                    // Set the start tile to the back of the room, furthest from the next one
                    if (roomProgressDirection == -1)
                        lastTile.x = roomSize.x - 1;
                    else
                        lastTile.x = 0;

                    break;
                }
            }

            // Continue on the next frame
            roomPathsCreated++;
            stepProgress = (float)roomPathsCreated / criticalPath.Count;
            if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
            {
                stopwatch.Restart();
                yield return null;
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 7 of level generation
    // Builds a border of inner walls and blank spaces around the map to be grown inward by wave function collapse
    IEnumerator CreateMapBorder()
    {
        int roomMapBordersCreated = 0;

        // For every cell in the map
        for (int stageY = 0; stageY < stageSize.y; stageY++)
        {
            for (int stageX = 0; stageX < stageSize.x; stageX++)
            {
                for (int roomY = 0; roomY < roomSize.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.x; roomX++)
                    {
                        Vector2Int gridPos = StageToGrid(stageX, stageY, roomX, roomY);
                        
                        // Outer edge of border (place inner wall)
                        if (gridPos.x == 0 || gridPos.x == (roomSize.x * stageSize.x) - 1 || gridPos.y == 0 || gridPos.y == (roomSize.y * stageSize.y) - 1)
                        {
                            PlaceTile(stageX, stageY, roomX, roomY, 45, BlockType.Solid);
                            level[stageX, stageY].tiles[roomX, roomY].reservedTile = false;
                            continue;
                        }

                        // Inner edge of border (set as blank space)
                        if (gridPos.x == 1 || gridPos.x == (roomSize.x * stageSize.x) - 2 || gridPos.y == 1 || gridPos.y == (roomSize.y * stageSize.y) - 2)
                        {
                            RemoveTile(stageX, stageY, roomX, roomY);
                            level[stageX, stageY].tiles[roomX, roomY].reservedTile = false;
                        }
                    }
                }

                roomMapBordersCreated++;
                stepProgress = (float)roomMapBordersCreated / (stageSize.x * stageSize.y);
                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 8 of level generation
    // Creates an aligned vertical access point between dropdown and landing rooms, marking it as critical path
    IEnumerator ConnectVerticalRooms()
    {
        int roomCutoutsCompleted = 0;

        // Mark areas to cut in dropdown and landing rooms
        for (int r = 0; r < criticalPath.Count; r++)
        {
            // For dropdown rooms, find the lowest point in the critical path and mark them
            if (level[criticalPath[r].x, criticalPath[r].y].roomType == LevelRoom.RoomType.Dropdown)
            {
                bool groundMarked = false;

                // Run through the tiles in the room from left to right, bottom to top
                for (int y = 0; y < roomSize.y; y++)
                {
                    bool previousTileState = false;
                    for (int x = 0; x < roomSize.x; x++)
                    {
                        if (level[criticalPath[r].x, criticalPath[r].y].tiles[x, y].reservedTile && previousTileState == false)
                        {
                            // If this is the start of a trough in the critical path
                            level[criticalPath[r].x, criticalPath[r].y].verticalAccessMin = x;
                            previousTileState = true;
                        }
                        
                        if (x + 1 < roomSize.x && level[criticalPath[r].x, criticalPath[r].y].tiles[x + 1, y].reservedTile == false && previousTileState)
                        {
                            // If this is the natural end of a trough in the critical path
                            level[criticalPath[r].x, criticalPath[r].y].verticalAccessMax = x;
                            groundMarked = true;
                            break;
                        }

                        if (x + 1 >= roomSize.x && level[criticalPath[r].x, criticalPath[r].y].tiles[x, y].reservedTile == true)
                        {
                            // If this is the forced end of a trough in the critical path
                            level[criticalPath[r].x, criticalPath[r].y].verticalAccessMax = x;
                            groundMarked = true;
                            break;
                        }

                        previousTileState = level[criticalPath[r].x, criticalPath[r].y].tiles[x, y].reservedTile;
                    }

                    if (groundMarked)
                        break;
                }
            }

            // For landing rooms, mark points in the ceiling matching the drilled hole in the ground in the room above
            if (level[criticalPath[r].x, criticalPath[r].y].roomType == LevelRoom.RoomType.Landing)
            {
                level[criticalPath[r].x, criticalPath[r].y].verticalAccessMin = level[criticalPath[r].x, criticalPath[r].y + 1].verticalAccessMin;
                level[criticalPath[r].x, criticalPath[r].y].verticalAccessMax = level[criticalPath[r].x, criticalPath[r].y + 1].verticalAccessMax;
            }

            if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
            {
                stopwatch.Restart();
                yield return null;
            }
        }

        // Cut holes between dropdown and landing rooms
        for (int r = 0; r < criticalPath.Count; r++)
        {
            // For dropdown rooms, cut up from the bottom of the room
            if (level[criticalPath[r].x, criticalPath[r].y].roomType == LevelRoom.RoomType.Dropdown)
            {
                // Left to right for each marked column
                for (int x = level[criticalPath[r].x, criticalPath[r].y].verticalAccessMin; x <= level[criticalPath[r].x, criticalPath[r].y].verticalAccessMax; x++)
                {
                    // Dig up from the ground until hitting the main path
                    for (int y = 0; y < roomSize.y - 1; y++)
                    {
                        level[criticalPath[r].x, criticalPath[r].y].tiles[x, y].reservedTile = true;

                        // Check if the column has completed
                        if (level[criticalPath[r].x, criticalPath[r].y].tiles[x, y + 1].reservedTile)
                            break;
                    }
                }
            }

            // For landing rooms, cut down from the top of the room
            if (level[criticalPath[r].x, criticalPath[r].y].roomType == LevelRoom.RoomType.Landing)
            {
                // Left to right for each marked column
                for (int x = level[criticalPath[r].x, criticalPath[r].y].verticalAccessMin; x <= level[criticalPath[r].x, criticalPath[r].y].verticalAccessMax; x++)
                {
                    // Dig down from the ceiling until hitting the main path
                    for (int y = roomSize.y - 1; y > 0; y--)
                    {
                        level[criticalPath[r].x, criticalPath[r].y].tiles[x, y].reservedTile = true;

                        // Check if the column has completed
                        if (level[criticalPath[r].x, criticalPath[r].y].tiles[x, y - 1].reservedTile)
                            break;
                    }
                }
            }

            roomCutoutsCompleted++;
            stepProgress = (float)roomCutoutsCompleted / criticalPath.Count;
            if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
            {
                stopwatch.Restart();
                yield return null;
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 9 of level generation
    // Places border tiles along the bottom of *every* room (not only on critical path), except for places that contact the vertical access points
    IEnumerator CreateRoomBorders()
    {
        int roomBordersCreated = 0;

        // For each room in the stage...
        for (int stageY = 0; stageY < stageSize.y; stageY++)
        {
            for (int stageX = 0; stageX < stageSize.x; stageX++)
            {
                // For each tile in the bottom row of this room
                for (int roomX = 0; roomX < roomSize.x; roomX++)
                {
                    // Scan along the bottom row, this bool is true if there is a critical path tile to the left/right or on top of the current tile
                    bool criticalPathNearby = false;
                    if (level[stageX, stageY].tiles[roomX, 0].reservedTile) criticalPathNearby = true;
                    if (roomX - 1 >= 0 && level[stageX, stageY].tiles[roomX - 1, 0].reservedTile) criticalPathNearby = true;
                    if (roomX + 1 <= roomSize.x - 1 && level[stageX, stageY].tiles[roomX + 1, 0].reservedTile) criticalPathNearby = true;
                    if (roomX == roomSize.x - 1 && stageX + 1 <= stageSize.x - 1 && level[stageX + 1, stageY].tiles[0, 0].reservedTile) criticalPathNearby = true;
                    if (roomX == 0 && stageX - 1 >= 0 && level[stageX - 1, stageY].tiles[roomSize.x - 1, 0].reservedTile) criticalPathNearby = true;

                    // Set wall tile on this space since it's not near a vertical access
                    if (criticalPathNearby == false)
                    {
                        PlaceTile(stageX, stageY, roomX, 0, 45, BlockType.Solid);
                    }
                }

                // Update progress
                roomBordersCreated++;
                stepProgress = (float)roomBordersCreated / (stageSize.x * stageSize.y);
                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 10 of level generation
    // Wave function collapse pass for walls and air
    IEnumerator WaveFunctionCollapseWalls()
    {
        int positionsCollapsed = 0;
        
        // Set up grid for wave function collapse
        for (int stageY = 0; stageY < stageSize.y; stageY++)
        {
            for (int stageX = 0; stageX < stageSize.x; stageX++)
            {
                for (int roomY = 0; roomY < roomSize.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.x; roomX++)
                    {
                        if (level[stageX, stageY].tiles[roomX, roomY].reservedTile)
                            PlaceTile(stageX, stageY, roomX, roomY, 48, BlockType.None);

                        if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                        {
                            stopwatch.Restart();
                            yield return null;
                        }
                    }
                }
            }
        }

        // Set wave function type
        waveFunctionCollapse.ResetBlockPalette();
        waveFunctionCollapse.AddToBlockPalette(BlockType.None);
        waveFunctionCollapse.AddToBlockPalette(BlockType.Solid);

        // Calculate entropy of entire wave function collapse grid
        for (int y = 0; y < (stageSize.y * roomSize.y); y++)
        {
            for (int x = 0; x < (stageSize.x * roomSize.x); x++)
            {
                waveFunctionCollapse.RecalculateEntropy(x, y);

                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        int uncollapsedTiles = waveFunctionCollapse.GetUncollapsedCount();

        Vector2Int positionToCollapse;

        #region Cache

        BlockType collapsedType = BlockType.None;
        int collapsedTileIndex = 0;
        Vector2Int stagePos = Vector2Int.zero;
        Vector2Int roomPos = Vector2Int.zero;

        #endregion

        while (true)
        {
            positionToCollapse = waveFunctionCollapse.GetLowestEntropyTile(false);

            if (positionToCollapse.x != -1)
            {
                // Collapse this position
                if (waveFunctionCollapse.CollapseTile(positionToCollapse.x, positionToCollapse.y))
                {
                    collapsedType = waveFunctionCollapse.GetCollapsedType(positionToCollapse.x, positionToCollapse.y);
                    collapsedTileIndex = waveFunctionCollapse.GetCollapsedTileIndex(positionToCollapse.x, positionToCollapse.y);
                    stagePos = GridToStage(positionToCollapse.x, positionToCollapse.y);
                    roomPos = GridToRoom(positionToCollapse.x, positionToCollapse.y);
                    PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, collapsedTileIndex, collapsedType);

                    // Recalculate neighbours
                    for (int yOffset = 1; yOffset >= -1; yOffset--)
                    {
                        for (int xOffset = -1; xOffset <= 1; xOffset++)
                        {
                            // Skip the centre tile
                            if (xOffset == 0 && yOffset == 0)
                                continue;

                            // Update this neighbour tile
                            waveFunctionCollapse.RecalculateEntropy(positionToCollapse.x + xOffset, positionToCollapse.y + yOffset);
                        }
                    }

                    positionsCollapsed++;
                    stepProgress = (float)positionsCollapsed / uncollapsedTiles;
                }
            }
            else
            {
                // No positions left to collapse
                break;
            }

            if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
            {
                stopwatch.Restart();
                yield return null;
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 11 of level generation
    // Find all uncollapsed spaces and place crates to fill the space
    IEnumerator CleanupWalls()
    {
        int cleanupIterations = 0;
        
        // Calculate entropy of entire wave function collapse grid
        for (int y = 0; y < (stageSize.y * roomSize.y); y++)
        {
            for (int x = 0; x < (stageSize.x * roomSize.x); x++)
            {
                waveFunctionCollapse.RecalculateEntropy(x, y);

                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        int uncollapsedTiles = waveFunctionCollapse.GetUncollapsedCount();

        #region Cache

        Vector2Int stagePos;
        Vector2Int roomPos;
        Vector2Int neighbourStagePos;
        Vector2Int neighbourRoomPos;

        #endregion

        Vector2Int positionToCollapse;
        while (true)
        {
            // Find tiles which have not been collapsed, only 0 entropy tiles will be remaining after the previous step
            positionToCollapse = waveFunctionCollapse.GetLowestEntropyTile(true);

            if (positionToCollapse.x != -1)
            {
                // Ensure this space cannot collapse
                if (waveFunctionCollapse.CollapseTile(positionToCollapse.x, positionToCollapse.y) == false)
                {
                    // Force collapse it into a crate
                    stagePos = GridToStage(positionToCollapse.x, positionToCollapse.y);
                    roomPos = GridToRoom(positionToCollapse.x, positionToCollapse.y);
                    PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, 72, BlockType.Solid);

                    // Recollapse neighbouring walls into crates as well
                    for (int yOffset = 1; yOffset >= -1; yOffset--)
                    {
                        for (int xOffset = -1; xOffset <= 1; xOffset++)
                        {
                            // Skip the centre tile
                            if (xOffset == 0 && yOffset == 0)
                                continue;

                            neighbourStagePos = GridToStage(positionToCollapse.x + xOffset, positionToCollapse.y + yOffset);
                            neighbourRoomPos = GridToRoom(positionToCollapse.x + xOffset, positionToCollapse.y + yOffset);

                            // Only recollapse walls
                            if (level[neighbourStagePos.x, neighbourStagePos.y].tiles[neighbourRoomPos.x, neighbourRoomPos.y].blockType == BlockType.Solid)
                            {
                                // Only recollapse on border
                                if (level[neighbourStagePos.x, neighbourStagePos.y].tiles[neighbourRoomPos.x, neighbourRoomPos.y].tileIndex != 45 &&
                                    level[neighbourStagePos.x, neighbourStagePos.y].tiles[neighbourRoomPos.x, neighbourRoomPos.y].tileIndex != 46)
                                {
                                    RemoveTile(neighbourStagePos.x, neighbourStagePos.y, neighbourRoomPos.x, neighbourRoomPos.y);
                                    PlaceTile(neighbourStagePos.x, neighbourStagePos.y, neighbourRoomPos.x, neighbourRoomPos.y, 72, BlockType.Solid);
                                }
                            }
                        }
                    }
                }

                cleanupIterations++;
                stepProgress = (float)cleanupIterations / uncollapsedTiles;
                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
            else
            {
                // No positions left to collapse
                break;
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 12 of level generation
    // Places a bunch of bonus crates throughout the level to break up the wall structure
    IEnumerator PlaceBonusCrates()
    {
        int spacesEvaluated = 0;

        // Add crates to random spaces using perlin noise
        for (int y = 0; y < stageSize.y * roomSize.y; y++)
        {
            for (int x = 0; x < stageSize.x * roomSize.x; x++)
            {
                Vector2Int stagePos = GridToStage(x, y);
                Vector2Int roomPos = GridToRoom(x, y);

                // Only convert solid walls
                if (level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].blockType == BlockType.Solid)
                {
                    float perlinValue = Mathf.PerlinNoise(x / (Mathf.PI), y / (Mathf.PI));

                    if (perlinValue < 0.15f)
                    {
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                        PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, 72, BlockType.Solid);
                    }
                }

                spacesEvaluated++;
                stepProgress = (float)spacesEvaluated / (stageSize.x * stageSize.y * roomSize.x * roomSize.y * 2);
                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        // When surrounded by crates, convert isolated walls to crates
        for (int y = 0; y < stageSize.y * roomSize.y; y++)
        {
            for (int x = 0; x < stageSize.x * roomSize.x; x++)
            {
                Vector2Int stagePos = GridToStage(x, y);
                Vector2Int roomPos = GridToRoom(x, y);

                // Only convert walls
                if (level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].blockType == BlockType.Solid)
                {
                    bool convertToCrate = true;
                    int crateNeighbours = 0;
                    for (int yOffset = -1; yOffset <= 1; yOffset++)
                    {
                        for (int xOffset = -1; xOffset <= 1; xOffset++)
                        {
                            // Skip centre tile
                            if (xOffset == 0 && yOffset == 0)
                                continue;

                            // Skip corners
                            if (Mathf.Abs(xOffset) == 1 && Mathf.Abs(yOffset) == 1)
                                continue;

                            // Skip if out of bounds of the map
                            if (x + xOffset < 0 || x + xOffset >= roomSize.x * stageSize.x || y + yOffset < 0 || y + yOffset >= roomSize.y * stageSize.y)
                                continue;

                            Vector2Int neighbourStagePos = GridToStage(x + xOffset, y + yOffset);
                            Vector2Int neighbourRoomPos = GridToRoom(x + xOffset, y + yOffset);

                            // If this is a non-crate wall tile
                            if (level[neighbourStagePos.x, neighbourStagePos.y].tiles[neighbourRoomPos.x, neighbourRoomPos.y].blockType == BlockType.Solid &&
                                level[neighbourStagePos.x, neighbourStagePos.y].tiles[neighbourRoomPos.x, neighbourRoomPos.y].tileIndex != 72)
                            {
                                convertToCrate = false;
                                break;
                            }

                            // If this is a crate wall tile
                            if (level[neighbourStagePos.x, neighbourStagePos.y].tiles[neighbourRoomPos.x, neighbourRoomPos.y].tileIndex == 72)
                            {
                                crateNeighbours += 1;
                            }
                        }

                        if (convertToCrate == false) break;
                    }

                    // Convert the wall to a crate, if there was at least one crate touching it
                    if (convertToCrate && crateNeighbours > 0)
                    {
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                        PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, 72, BlockType.Solid);
                    }
                }

                spacesEvaluated++;
                stepProgress = (float)spacesEvaluated / (stageSize.x * stageSize.y * roomSize.x * roomSize.y * 2);
                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 13 of level generation
    // Wave function collapse pass for ladders and platforms
    IEnumerator WaveFunctionCollapsePlatforming()
    {
        int positionsCollapsed = 0;
        
        // Set up grid for wave function collapse
        for (int stageY = 0; stageY < stageSize.y; stageY++)
        {
            for (int stageX = 0; stageX < stageSize.x; stageX++)
            {
                for (int roomY = 0; roomY < roomSize.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.x; roomX++)
                    {
                        // Remove all air
                        if (level[stageX, stageY].tiles[roomX, roomY].blockType == BlockType.None)
                            RemoveTile(stageX, stageY, roomX, roomY);

                        if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                        {
                            stopwatch.Restart();
                            yield return null;
                        }
                    }
                }
            }
        }

        // Set wave function type
        waveFunctionCollapse.ResetBlockPalette();
        waveFunctionCollapse.AddToBlockPalette(BlockType.None);
        waveFunctionCollapse.AddToBlockPalette(BlockType.OneWay);
        waveFunctionCollapse.AddToBlockPalette(BlockType.Ladder);

        // Calculate entropy of entire wave function collapse grid
        for (int y = 0; y < (stageSize.y * roomSize.y); y++)
        {
            for (int x = 0; x < (stageSize.x * roomSize.x); x++)
            {
                waveFunctionCollapse.RecalculateEntropy(x, y);

                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        int uncollapsedTiles = waveFunctionCollapse.GetUncollapsedCount();

        Vector2Int positionToCollapse;

        #region Cache

        BlockType collapsedType = BlockType.None;
        int collapsedTileIndex = 0;
        Vector2Int stagePos = Vector2Int.zero;
        Vector2Int roomPos = Vector2Int.zero;

        #endregion

        while (true)
        {
            positionToCollapse = waveFunctionCollapse.GetLowestEntropyTile(false);

            if (positionToCollapse.x != -1)
            {
                // Collapse this position
                if (waveFunctionCollapse.CollapseTile(positionToCollapse.x, positionToCollapse.y))
                {
                    collapsedType = waveFunctionCollapse.GetCollapsedType(positionToCollapse.x, positionToCollapse.y);
                    collapsedTileIndex = waveFunctionCollapse.GetCollapsedTileIndex(positionToCollapse.x, positionToCollapse.y);
                    stagePos = GridToStage(positionToCollapse.x, positionToCollapse.y);
                    roomPos = GridToRoom(positionToCollapse.x, positionToCollapse.y);
                    PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, collapsedTileIndex, collapsedType);

                    // Recalculate neighbours
                    for (int yOffset = 1; yOffset >= -1; yOffset--)
                    {
                        for (int xOffset = -1; xOffset <= 1; xOffset++)
                        {
                            // Skip the centre tile
                            if (xOffset == 0 && yOffset == 0)
                                continue;

                            // Update this neighbour tile
                            waveFunctionCollapse.RecalculateEntropy(positionToCollapse.x + xOffset, positionToCollapse.y + yOffset);
                        }
                    }

                    positionsCollapsed++;
                    stepProgress = (float)positionsCollapsed / uncollapsedTiles;
                }
            }
            else
            {
                // No positions left to collapse
                break;
            }

            if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
            {
                stopwatch.Restart();
                yield return null;
            }
        }

        yield return null;
        CompleteStep();
    }
    
    // Step 14 of level generation
    // Removes disconnected ladders and ensures they are capped correctly
    IEnumerator CleanupLadders()
    {
        int cleanupIterations = 0;

        // Iterate over every tile
        for (int y = 0; y < (stageSize.y * roomSize.y); y++)
        {
            for (int x = 0; x < (stageSize.x * roomSize.x); x++)
            {
                Vector2Int stagePos = GridToStage(x, y);
                Vector2Int roomPos = GridToRoom(x, y);

                if (level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].blockType == BlockType.Ladder)
                {
                    Vector2Int stageAbove = GridToStage(x, y + 1);
                    Vector2Int roomAbove = GridToRoom(x, y + 1);
                    BlockType blockAbove = level[stageAbove.x, stageAbove.y].tiles[roomAbove.x, roomAbove.y].blockType;

                    Vector2Int stageBelow = GridToStage(x, y - 1);
                    Vector2Int roomBelow = GridToRoom(x, y - 1);
                    BlockType blockBelow = level[stageBelow.x, stageBelow.y].tiles[roomBelow.x, roomBelow.y].blockType;

                    // Ensure middle ladders are set correctly
                    if ((blockAbove == BlockType.Solid || blockAbove == BlockType.Ladder) && blockBelow == BlockType.Ladder)
                    {
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                        PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, 65, BlockType.Ladder);
                    }

                    // Ensure top ladders are capped correctly
                    if (blockAbove != BlockType.Ladder && blockAbove != BlockType.Solid && blockBelow == BlockType.Ladder)
                    {
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                        PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, 66, BlockType.Ladder);
                    }

                    // Ensure bottom ladders are capped correctly
                    if (blockAbove == BlockType.Ladder && blockBelow != BlockType.Ladder && blockBelow != BlockType.Solid && blockBelow != BlockType.OneWay)
                    {
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                        PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, 67, BlockType.Ladder);
                    }

                    // Remove floating ladders, not connected by the top or bottom
                    if (blockBelow != BlockType.Ladder && blockBelow != BlockType.Solid && blockBelow != BlockType.OneWay)
                    {
                        // First find the bottom of the ladder, and continue if it's not connected
                        // Move up one space at a time until reaching the top of the ladder
                        // If the top of the ladder is also not connected, delete the whole ladder

                        Vector2Int nextStageAbove = GridToStage(x, y);
                        Vector2Int nextRoomAbove = GridToRoom(x, y);
                        BlockType nextBlockAbove = level[stageAbove.x, stageAbove.y].tiles[roomAbove.x, roomAbove.y].blockType;
                        bool disconnectedLadder = true;

                        Vector2Int startPoint = new Vector2Int(x, y);
                        int ladderLengthSoFar = 1;

                        List<Vector2Int> ladderPoints = new List<Vector2Int>();
                        ladderPoints.Add(new Vector2Int(x, y));

                        while (true)
                        {
                            nextStageAbove = GridToStage(startPoint.x, startPoint.y + ladderLengthSoFar);
                            nextRoomAbove = GridToRoom(startPoint.x, startPoint.y + ladderLengthSoFar);
                            nextBlockAbove = level[nextStageAbove.x, nextStageAbove.y].tiles[nextRoomAbove.x, nextRoomAbove.y].blockType;

                            ladderLengthSoFar++;

                            if (nextBlockAbove == BlockType.Ladder)
                            {
                                // If there is a ladder above, add it to the list
                                Vector2Int gridPos = StageToGrid(nextStageAbove.x, nextStageAbove.y, nextRoomAbove.x, nextRoomAbove.y);
                                ladderPoints.Add(gridPos);
                            }
                            else if (nextBlockAbove == BlockType.Solid)
                            {
                                // If there is a wall above, the ladder is connected
                                disconnectedLadder = false;
                                break;
                            }
                            else if (nextBlockAbove != BlockType.Solid && nextBlockAbove != BlockType.Ladder)
                            {
                                // If there is not a wall or a ladder above, the ladder is disconnected
                                disconnectedLadder = true;
                                break;
                            }
                        }

                        // If the ladder is disconnected to a surface, delete it
                        if (disconnectedLadder)
                        {
                            for (int i = 0; i < ladderPoints.Count; i++)
                            {
                                Vector2Int ladderStagePos = GridToStage(ladderPoints[i].x, ladderPoints[i].y);
                                Vector2Int ladderRoomPos = GridToRoom(ladderPoints[i].x, ladderPoints[i].y);
                                RemoveTile(ladderStagePos.x, ladderStagePos.y, ladderRoomPos.x, ladderRoomPos.y);
                            }
                        }
                    }

                    // Remove single ladders
                    if (blockAbove != BlockType.Ladder && blockBelow != BlockType.Ladder)
                    {
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                    }
                }

                cleanupIterations++;
                stepProgress = (float)cleanupIterations / (stageSize.x * stageSize.y * roomSize.x * roomSize.y);
                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 15 of level generation
    // Ensures platforms are capped correctly, and deletes isolated ones
    IEnumerator CleanupPlatforms()
    {
        int cleanupIterations = 0;

        // Iterate over every tile
        for (int y = 0; y < (stageSize.y * roomSize.y); y++)
        {
            for (int x = 0; x < (stageSize.x * roomSize.x); x++)
            {
                Vector2Int stagePos = GridToStage(x, y);
                Vector2Int roomPos = GridToRoom(x, y);

                if (level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].blockType == BlockType.OneWay)
                {
                    Vector2Int stageLeft = GridToStage(x - 1, y);
                    Vector2Int roomLeft = GridToRoom(x - 1, y);
                    BlockType blockLeft = level[stageLeft.x, stageLeft.y].tiles[roomLeft.x, roomLeft.y].blockType;

                    Vector2Int stageRight = GridToStage(x + 1, y);
                    Vector2Int roomRight = GridToRoom(x + 1, y);
                    BlockType blockRight = level[stageRight.x, stageRight.y].tiles[roomRight.x, roomRight.y].blockType;

                    // Ensure middle platforms are set correctly
                    if ((blockLeft == BlockType.Solid || blockLeft == BlockType.OneWay) && (blockRight == BlockType.Solid || blockRight == BlockType.OneWay))
                    {
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                        PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, 69, BlockType.OneWay);
                    }

                    // Ensure left platforms are capped correctly
                    if ((blockLeft != BlockType.Solid && blockLeft != BlockType.OneWay) && (blockRight == BlockType.Solid || blockRight == BlockType.OneWay))
                    {
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                        PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, 68, BlockType.OneWay);
                    }

                    // Ensure right platforms are capped correctly
                    if ((blockLeft == BlockType.Solid || blockLeft == BlockType.OneWay) && (blockRight != BlockType.Solid && blockRight != BlockType.OneWay))
                    {
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                        PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, 70, BlockType.OneWay);
                    }

                    // Remove single platforms
                    if (blockLeft != BlockType.OneWay && blockRight != BlockType.OneWay && blockLeft != BlockType.Solid && blockRight != BlockType.Solid)
                    {
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                    }
                }

                cleanupIterations++;
                stepProgress = (float)cleanupIterations / (stageSize.x * stageSize.y * roomSize.x * roomSize.y);
                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 16 of level generation
    // Wave function collapse pass for spikes and coins
    IEnumerator WaveFunctionCollapseGameplay()
    {
        int positionsCollapsed = 0;
        
        // Set up grid for wave function collapse
        for (int stageY = 0; stageY < stageSize.y; stageY++)
        {
            for (int stageX = 0; stageX < stageSize.x; stageX++)
            {
                for (int roomY = 0; roomY < roomSize.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.x; roomX++)
                    {
                        // Remove all air
                        if (level[stageX, stageY].tiles[roomX, roomY].blockType == BlockType.None)
                            RemoveTile(stageX, stageY, roomX, roomY);
                    }
                }
            }
        }

        // Set wave function type
        waveFunctionCollapse.ResetBlockPalette();
        waveFunctionCollapse.AddToBlockPalette(BlockType.None);
        waveFunctionCollapse.AddToBlockPalette(BlockType.Spike);
        waveFunctionCollapse.AddToBlockPalette(BlockType.Coin);

        // Calculate entropy of entire wave function collapse grid
        for (int y = 0; y < (stageSize.y * roomSize.y); y++)
        {
            for (int x = 0; x < (stageSize.x * roomSize.x); x++)
            {
                waveFunctionCollapse.RecalculateEntropy(x, y);

                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        int uncollapsedTiles = waveFunctionCollapse.GetUncollapsedCount();

        Vector2Int positionToCollapse;

        #region Cache

        BlockType collapsedType = BlockType.None;
        int collapsedTileIndex = 0;
        Vector2Int stagePos = Vector2Int.zero;
        Vector2Int roomPos = Vector2Int.zero;

        #endregion

        while (true)
        {
            positionToCollapse = waveFunctionCollapse.GetLowestEntropyTile(false);

            if (positionToCollapse.x != -1)
            {
                // Collapse this position
                if (waveFunctionCollapse.CollapseTile(positionToCollapse.x, positionToCollapse.y))
                {
                    collapsedType = waveFunctionCollapse.GetCollapsedType(positionToCollapse.x, positionToCollapse.y);
                    collapsedTileIndex = waveFunctionCollapse.GetCollapsedTileIndex(positionToCollapse.x, positionToCollapse.y);
                    stagePos = GridToStage(positionToCollapse.x, positionToCollapse.y);
                    roomPos = GridToRoom(positionToCollapse.x, positionToCollapse.y);
                    PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, collapsedTileIndex, collapsedType);

                    // Recalculate neighbours
                    for (int yOffset = 1; yOffset >= -1; yOffset--)
                    {
                        for (int xOffset = -1; xOffset <= 1; xOffset++)
                        {
                            // Skip the centre tile
                            if (xOffset == 0 && yOffset == 0)
                                continue;

                            // Update this neighbour tile
                            waveFunctionCollapse.RecalculateEntropy(positionToCollapse.x + xOffset, positionToCollapse.y + yOffset);
                        }
                    }

                    positionsCollapsed++;
                    stepProgress = (float)positionsCollapsed / uncollapsedTiles;
                }
            }
            else
            {
                // No positions left to collapse
                break;
            }

            if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
            {
                stopwatch.Restart();
                yield return null;
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 17 of level generation
    // Marches floating spikes and coins towards the appropriate surfaces
    IEnumerator ReconnectGameplay()
    {
        int cleanupIterations = 0;

        // Check every tile in the level
        for (int stageY = 0; stageY < stageSize.y; stageY++)
        {
            for (int stageX = 0; stageX < stageSize.x; stageX++)
            {
                for (int roomY = 0; roomY < roomSize.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.x; roomX++)
                    {
                        if (level[stageX, stageY].tiles[roomX, roomY].blockType == BlockType.Coin)
                        {
                            // March downward as far as possible
                            RemoveTile(stageX, stageY, roomX, roomY);
                            Vector2Int currentPos = StageToGrid(stageX, stageY, roomX, roomY);
                            Vector2Int stagePos = GridToStage(currentPos.x, currentPos.y);
                            Vector2Int roomPos = GridToRoom(currentPos.x, currentPos.y);
                            
                            while (level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].blockType == BlockType.None)
                            {
                                currentPos.y -= 1;
                                stagePos = GridToStage(currentPos.x, currentPos.y);
                                roomPos = GridToRoom(currentPos.x, currentPos.y);
                            }

                            currentPos.y += 1;
                            stagePos = GridToStage(currentPos.x, currentPos.y);
                            roomPos = GridToRoom(currentPos.x, currentPos.y);
                            PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, 49, BlockType.Coin);
                        }
                        else if (level[stageX, stageY].tiles[roomX, roomY].tileIndex == 61)
                        {
                            // Remove up-facing spike if it isn't connected to a wall or platform below
                            Vector2Int gridPos = StageToGrid(stageX, stageY, roomX, roomY);
                            Vector2Int stagePosBelow = GridToStage(gridPos.x, gridPos.y - 1);
                            Vector2Int roomPosBelow = GridToRoom(gridPos.x, gridPos.y - 1);

                            BlockType typeBelow = level[stagePosBelow.x, stagePosBelow.y].tiles[roomPosBelow.x, roomPosBelow.y].blockType;

                            if (typeBelow != BlockType.Solid && typeBelow != BlockType.OneWay)
                            {
                                // Delete the original
                                RemoveTile(stageX, stageY, roomX, roomY);

                                // Attempt to find position to reconnect spike
                                Vector2Int currentPos = gridPos;
                                Vector2Int stageBelowCurrent = GridToStage(currentPos.x, currentPos.y - 1);
                                Vector2Int roomBelowCurrent = GridToRoom(currentPos.x, currentPos.y - 1);
                                BlockType typeBelowCurrent = level[stageBelowCurrent.x, stageBelowCurrent.y].tiles[roomBelowCurrent.x, roomBelowCurrent.y].blockType;
                                
                                // March down and check below
                                while (typeBelowCurrent != BlockType.Solid && typeBelowCurrent != BlockType.OneWay)
                                {
                                    currentPos.y -= 1;
                                    stageBelowCurrent = GridToStage(currentPos.x, currentPos.y - 1);
                                    roomBelowCurrent = GridToRoom(currentPos.x, currentPos.y - 1);
                                    typeBelowCurrent = level[stageBelowCurrent.x, stageBelowCurrent.y].tiles[roomBelowCurrent.x, roomBelowCurrent.y].blockType;
                                }

                                // Current pos is now above a oneway or solid
                                Vector2Int stageAtCurrent = GridToStage(currentPos.x, currentPos.y);
                                Vector2Int roomAtCurrent = GridToRoom(currentPos.x, currentPos.y);
                                BlockType typeAtCurrent = level[stageAtCurrent.x, stageAtCurrent.y].tiles[roomAtCurrent.x, roomAtCurrent.y].blockType;

                                // Place the marched spike
                                if (typeAtCurrent == BlockType.None || typeAtCurrent == BlockType.Coin)
                                {
                                    RemoveTile(stageAtCurrent.x, stageAtCurrent.y, roomAtCurrent.x, roomAtCurrent.y);
                                    PlaceTile(stageAtCurrent.x, stageAtCurrent.y, roomAtCurrent.x, roomAtCurrent.y, 61, BlockType.Spike);
                                }
                            }
                        }
                        else if (level[stageX, stageY].tiles[roomX, roomY].tileIndex == 62)
                        {
                            // Remove down-facing spike if it isn't connected to a wall above
                            Vector2Int gridPos = StageToGrid(stageX, stageY, roomX, roomY);
                            Vector2Int stagePosAbove = GridToStage(gridPos.x, gridPos.y + 1);
                            Vector2Int roomPosAbove = GridToRoom(gridPos.x, gridPos.y + 1);

                            BlockType typeAbove = level[stagePosAbove.x, stagePosAbove.y].tiles[roomPosAbove.x, roomPosAbove.y].blockType;

                            if (typeAbove != BlockType.Solid)
                            {
                                // Delete the original
                                RemoveTile(stageX, stageY, roomX, roomY);

                                // Attempt to find position to reconnect spike
                                Vector2Int currentPos = gridPos;
                                Vector2Int stageAboveCurrent = GridToStage(currentPos.x, currentPos.y + 1);
                                Vector2Int roomAboveCurrent = GridToRoom(currentPos.x, currentPos.y + 1);
                                BlockType typeAboveCurrent = level[stageAboveCurrent.x, stageAboveCurrent.y].tiles[roomAboveCurrent.x, roomAboveCurrent.y].blockType;

                                // March up and check above
                                while (typeAboveCurrent != BlockType.Solid)
                                {
                                    currentPos.y += 1;
                                    stageAboveCurrent = GridToStage(currentPos.x, currentPos.y + 1);
                                    roomAboveCurrent = GridToRoom(currentPos.x, currentPos.y + 1);
                                    typeAboveCurrent = level[stageAboveCurrent.x, stageAboveCurrent.y].tiles[roomAboveCurrent.x, roomAboveCurrent.y].blockType;
                                }

                                // Current pos is now below a oneway or solid
                                Vector2Int stageAtCurrent = GridToStage(currentPos.x, currentPos.y);
                                Vector2Int roomAtCurrent = GridToRoom(currentPos.x, currentPos.y);
                                BlockType typeAtCurrent = level[stageAtCurrent.x, stageAtCurrent.y].tiles[roomAtCurrent.x, roomAtCurrent.y].blockType;

                                // Place the marched spike
                                if (typeAtCurrent == BlockType.None || typeAtCurrent == BlockType.Coin)
                                {
                                    RemoveTile(stageAtCurrent.x, stageAtCurrent.y, roomAtCurrent.x, roomAtCurrent.y);
                                    PlaceTile(stageAtCurrent.x, stageAtCurrent.y, roomAtCurrent.x, roomAtCurrent.y, 62, BlockType.Spike);
                                }
                            }
                        }

                        cleanupIterations++;
                        stepProgress = (float)cleanupIterations / (stageSize.x * stageSize.y * roomSize.x * roomSize.y);
                        if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                        {
                            stopwatch.Restart();
                            yield return null;
                        }
                    }
                }
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 18 of level generation
    // Removes isolated and unreachable coins
    IEnumerator CleanupCoins()
    {
        int cleanupIterations = 0;

        // Fresh flood fill from somewhere on the critical path
        ResetFloodFill();
        bool floodfillStarted = false;
        for (int y = 0; y < roomSize.y; y++)
        {
            for (int x = 0; x < roomSize.x; x++)
            {
                if (level[criticalPath[0].x, criticalPath[0].y].tiles[x, y].reservedTile)
                {
                    Vector2Int gridPos = StageToGrid(criticalPath[0].x, criticalPath[0].y, x, y);
                    FloodFill(gridPos.x, gridPos.y);
                    floodfillStarted = true;
                    break;
                }
            }

            if (floodfillStarted) break;
        }

        // Remove isolated/unreachable coins
        for (int stageY = 0; stageY < stageSize.y; stageY++)
        {
            for (int stageX = 0; stageX < stageSize.x; stageX++)
            {
                for (int roomY = 0; roomY < roomSize.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.x; roomX++)
                    {
                        if (level[stageX, stageY].tiles[roomX, roomY].blockType == BlockType.Coin)
                        {
                            // Remove unreachable coins
                            if (level[stageX, stageY].tiles[roomX, roomY].marked == false)
                            {
                                RemoveTile(stageX, stageY, roomX, roomY);
                                continue;
                            }

                            // Count edge-to-edge neighbours to determine if coin is isolated
                            int edgeNeighbours = 0;
                            for (int yOffset = 1; yOffset >= -1; yOffset--)
                            {
                                for (int xOffset = -1; xOffset <= 1; xOffset++)
                                {
                                    // Skip centre tile
                                    if (xOffset == 0 && yOffset == 0)
                                        continue;

                                    // Skip corner tiles
                                    if (Mathf.Abs(xOffset) == 1 && Mathf.Abs(yOffset) == 1)
                                        continue;

                                    Vector2Int neighbourGridPos = StageToGrid(stageX, stageY, roomX, roomY);
                                    Vector2Int neighbourStagePos = GridToStage(neighbourGridPos.x + xOffset, neighbourGridPos.y + yOffset);
                                    Vector2Int neighbourRoomPos = GridToRoom(neighbourGridPos.x + xOffset, neighbourGridPos.y + yOffset);

                                    if (level[neighbourStagePos.x, neighbourStagePos.y].tiles[neighbourRoomPos.x, neighbourRoomPos.y].blockType == BlockType.Coin)
                                    {
                                        edgeNeighbours++;
                                    }
                                }
                            }

                            // If the coin has no neighbours on its edges
                            if (edgeNeighbours <= 0)
                            {
                                RemoveTile(stageX, stageY, roomX, roomY);
                            }
                        }

                        cleanupIterations++;
                        stepProgress = (float)cleanupIterations / (stageSize.x * stageSize.y * roomSize.x * roomSize.y);
                        if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                        {
                            stopwatch.Restart();
                            yield return null;
                        }
                    }
                }
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 19 of level generation
    // Wave function collapse pass for foliage, vines, signs and torches
    IEnumerator WaveFunctionCollapseDeco()
    {
        int positionsCollapsed = 0;
        
        // Set up grid for wave function collapse
        for (int stageY = 0; stageY < stageSize.y; stageY++)
        {
            for (int stageX = 0; stageX < stageSize.x; stageX++)
            {
                for (int roomY = 0; roomY < roomSize.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.x; roomX++)
                    {
                        // Remove all air
                        if (level[stageX, stageY].tiles[roomX, roomY].blockType == BlockType.None)
                            RemoveTile(stageX, stageY, roomX, roomY);
                    }
                }
            }
        }

        // Set wave function type
        waveFunctionCollapse.ResetBlockPalette();
        waveFunctionCollapse.AddToBlockPalette(BlockType.None);
        waveFunctionCollapse.AddToBlockPalette(BlockType.Vine);
        waveFunctionCollapse.AddToBlockPalette(BlockType.Foliage);
        waveFunctionCollapse.AddToBlockPalette(BlockType.Sign);
        waveFunctionCollapse.AddToBlockPalette(BlockType.Torch);

        // Calculate entropy of entire wave function collapse grid
        for (int y = 0; y < (stageSize.y * roomSize.y); y++)
        {
            for (int x = 0; x < (stageSize.x * roomSize.x); x++)
            {
                waveFunctionCollapse.RecalculateEntropy(x, y);

                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        int uncollapsedTiles = waveFunctionCollapse.GetUncollapsedCount();

        Vector2Int positionToCollapse;

        #region Cache

        BlockType collapsedType = BlockType.None;
        int collapsedTileIndex = 0;
        Vector2Int stagePos = Vector2Int.zero;
        Vector2Int roomPos = Vector2Int.zero;

        #endregion

        while (true)
        {
            positionToCollapse = waveFunctionCollapse.GetLowestEntropyTile(false);

            if (positionToCollapse.x != -1)
            {
                // Collapse this position
                if (waveFunctionCollapse.CollapseTile(positionToCollapse.x, positionToCollapse.y))
                {
                    collapsedType = waveFunctionCollapse.GetCollapsedType(positionToCollapse.x, positionToCollapse.y);
                    collapsedTileIndex = waveFunctionCollapse.GetCollapsedTileIndex(positionToCollapse.x, positionToCollapse.y);
                    stagePos = GridToStage(positionToCollapse.x, positionToCollapse.y);
                    roomPos = GridToRoom(positionToCollapse.x, positionToCollapse.y);
                    PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, collapsedTileIndex, collapsedType);

                    // Recalculate neighbours
                    for (int yOffset = 1; yOffset >= -1; yOffset--)
                    {
                        for (int xOffset = -1; xOffset <= 1; xOffset++)
                        {
                            // Skip the centre tile
                            if (xOffset == 0 && yOffset == 0)
                                continue;

                            // Update this neighbour tile
                            waveFunctionCollapse.RecalculateEntropy(positionToCollapse.x + xOffset, positionToCollapse.y + yOffset);
                        }
                    }

                    positionsCollapsed++;
                    stepProgress = (float)positionsCollapsed / uncollapsedTiles;
                }
            }
            else
            {
                // No positions left to collapse
                break;
            }

            if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
            {
                stopwatch.Restart();
                yield return null;
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 20 of level generation
    // Marches floating torches/foliage towards the nearest surface, and attempts to reconnect it
    IEnumerator ReconnectDeco()
    {
        int cleanupIterations = 0;

        // The tile below the current one
        BlockType previousTile = BlockType.Solid;

        // Loop upward through each column, left to right
        for (int x = 0; x < (stageSize.x * roomSize.x); x++)
        {
            for (int y = 0; y < (stageSize.y * roomSize.y); y++)
            {
                Vector2Int stagePos = GridToStage(x, y);
                Vector2Int roomPos = GridToRoom(x, y);

                // If the tile is not grounded
                if (previousTile != BlockType.Solid && previousTile != BlockType.OneWay)
                {
                    if (level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].blockType == BlockType.Foliage ||
                        level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].blockType == BlockType.Torch)
                    {
                        // Store info about this tile before removing it
                        int tileIndex = level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].tileIndex;
                        BlockType tileType = level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].blockType;
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);

                        // March downward as far as possible
                        Vector2Int currentPos = StageToGrid(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                        Vector2Int marchedStagePos = GridToStage(currentPos.x, currentPos.y);
                        Vector2Int marchedRoomPos = GridToRoom(currentPos.x, currentPos.y);

                        // Go down until a standable surface is reached
                        while (level[marchedStagePos.x, marchedStagePos.y].tiles[marchedRoomPos.x, marchedRoomPos.y].blockType != BlockType.Solid &&
                               level[marchedStagePos.x, marchedStagePos.y].tiles[marchedRoomPos.x, marchedRoomPos.y].blockType != BlockType.OneWay)
                        {
                            currentPos.y -= 1;
                            marchedStagePos = GridToStage(currentPos.x, currentPos.y);
                            marchedRoomPos = GridToRoom(currentPos.x, currentPos.y);
                        }

                        currentPos.y += 1;
                        marchedStagePos = GridToStage(currentPos.x, currentPos.y);
                        marchedRoomPos = GridToRoom(currentPos.x, currentPos.y);

                        // If there is nothing in this place, allow the tile to be placed
                        if (level[marchedStagePos.x, marchedStagePos.y].tiles[marchedRoomPos.x, marchedRoomPos.y].blockType == BlockType.None)
                        {
                            PlaceTile(marchedStagePos.x, marchedStagePos.y, marchedRoomPos.x, marchedRoomPos.y, tileIndex, tileType);
                        }
                    }
                }

                // Delete floating signs
                if (previousTile != BlockType.Solid && previousTile != BlockType.OneWay && previousTile != BlockType.Sign)
                {
                    if (level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].blockType == BlockType.Sign)
                    {
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                    }
                }

                previousTile = level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].blockType;

                cleanupIterations++;
                stepProgress = (float)cleanupIterations / (stageSize.x * stageSize.y * roomSize.x * roomSize.y);
                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 21 of level generation
    // Removes floating vines and disconnected signs, adds air everywhere else
    IEnumerator CleanupDeco()
    {
        int cleanupIterations = 0;

        // The tile above the current one
        BlockType previousTile = BlockType.Solid;

        // Loop downward through each column, left to right
        for (int x = 0; x < (stageSize.x * roomSize.x); x++)
        {
            for (int y = (stageSize.y * roomSize.y) - 1; y >= 0; y--)
            {
                Vector2Int stagePos = GridToStage(x, y);
                Vector2Int roomPos = GridToRoom(x, y);

                // Delete floating vines
                if (previousTile != BlockType.Solid && previousTile != BlockType.Vine)
                {
                    if (level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].blockType == BlockType.Vine)
                    {
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                    }
                }

                // Delete uncapped signs
                if (previousTile != BlockType.Sign)
                {
                    if (level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].tileIndex == 58)
                    {
                        RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);
                    }
                }

                previousTile = level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].blockType;

                cleanupIterations++;
                stepProgress = (float)cleanupIterations / (stageSize.x * stageSize.y * roomSize.x * roomSize.y);
                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 22 of level generation
    // Sets the player's spawn and exit door points in the level
    IEnumerator PlaceDoors()
    {
        bool floodfillCompleted = false;
        ResetFloodFill();

        // Set spawn

        // Find a safe space in the room, above solid ground and not within a platform
        bool spawnPlaced = false;
        for (int y = roomSize.y - 2; y > 0; y--)
        {
            for (int x = 1; x < roomSize.x - 2; x++)
            {
                // Found a safe space in the first room
                if (level[criticalPath[0].x, criticalPath[0].y].tiles[x, y].reservedTile && floodfillCompleted == false)
                {
                    // Flood fill the map from this point to find all legal positions
                    Vector2Int gridPos = StageToGrid(criticalPath[0].x, criticalPath[0].y, x, y);
                    FloodFill(gridPos.x, gridPos.y);
                    floodfillCompleted = true;
                }

                // If this cell is within the safe area of the room, above a few tiles of solid ground and not within a platform
                if (level[criticalPath[0].x, criticalPath[0].y].tiles[x, y].marked &&
                    level[criticalPath[0].x, criticalPath[0].y].tiles[x, y - 1].blockType == BlockType.Solid &&
                    level[criticalPath[0].x, criticalPath[0].y].tiles[x, y].blockType != BlockType.OneWay &&
                    (level[criticalPath[0].x, criticalPath[0].y].tiles[x - 1, y - 1].blockType == BlockType.Solid ||
                    level[criticalPath[0].x, criticalPath[0].y].tiles[x + 1, y - 1].blockType == BlockType.Solid))
                {
                    // Remove anything that might have been in this space resting on the ground
                    RemoveTile(criticalPath[0].x, criticalPath[0].y, x, y);

                    // Place spawn door
                    spawnPosition = StageToGrid(criticalPath[0].x, criticalPath[0].y, x, y);
                    levelTileManager.PlaceSpecialTile(spawnPosition.x, spawnPosition.y, LevelTileManager.SpecialTile.EntryDoor);

                    // Set camera starting position
                    Vector3 spawnPointAsVec3 = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
                    cameraController.SetStartPosition(spawnPointAsVec3);

                    spawnPlaced = true;
                    break;
                }
            }

            if (spawnPlaced) break;
        }

        stepProgress = 0.5f;
        yield return null;

        floodfillCompleted = false;

        // Set exit

        // Find a safe space in the room, above solid ground and not within a platform
        bool exitPlaced = false;
        int criticalPathLastRoom = criticalPath.Count - 1;
        for (int y = roomSize.y - 1; y > 0; y--)
        {
            for (int x = roomSize.x - 2; x > 0; x--)
            {
                // Found a safe space in the last room
                if (level[criticalPath[criticalPathLastRoom].x, criticalPath[criticalPathLastRoom].y].tiles[x, y].reservedTile && floodfillCompleted == false)
                {
                    // Flood fill the map from this point to find all legal positions
                    Vector2Int gridPos = StageToGrid(criticalPath[criticalPathLastRoom].x, criticalPath[criticalPathLastRoom].y, x, y);
                    FloodFill(gridPos.x, gridPos.y);
                    floodfillCompleted = true;
                }

                // If this cell is within the safe area of the room, above solid ground and not within a platform
                if (level[criticalPath[criticalPathLastRoom].x, criticalPath[criticalPathLastRoom].y].tiles[x, y].marked &&
                    level[criticalPath[criticalPathLastRoom].x, criticalPath[criticalPathLastRoom].y].tiles[x, y - 1].blockType == BlockType.Solid &&
                    level[criticalPath[criticalPathLastRoom].x, criticalPath[criticalPathLastRoom].y].tiles[x, y].blockType != BlockType.OneWay &&
                    (level[criticalPath[criticalPathLastRoom].x, criticalPath[criticalPathLastRoom].y].tiles[x - 1, y - 1].blockType == BlockType.Solid ||
                    level[criticalPath[criticalPathLastRoom].x, criticalPath[criticalPathLastRoom].y].tiles[x + 1, y - 1].blockType == BlockType.Solid))
                {
                    // Remove anything that might have been in this space resting on the ground
                    RemoveTile(criticalPath[criticalPathLastRoom].x, criticalPath[criticalPathLastRoom].y, x, y);

                    // Place exit door
                    exitPosition = StageToGrid(criticalPath[criticalPathLastRoom].x, criticalPath[criticalPathLastRoom].y, x, y);
                    levelTileManager.PlaceSpecialTile(exitPosition.x, exitPosition.y, LevelTileManager.SpecialTile.ExitDoor);

                    exitPlaced = true;
                    break;
                }
            }

            if (exitPlaced) break;
        }

        stepProgress = 1f;

        yield return null;
        CompleteStep();
    }

    // Step 23 of level generation
    // Clears the area around doors and ensures tiles still appear correctly
    IEnumerator CleanupDoors()
    {
        // Cleanup spawn

        // For each neighbour of this cell, at level or above
        for (int y = 1; y >= 0; y--)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                Vector2Int neighbourStagePos = GridToStage(spawnPosition.x + x, spawnPosition.y + y);
                Vector2Int neighbourRoomPos = GridToRoom(spawnPosition.x + x, spawnPosition.y + y);
                BlockType neighbourType = level[neighbourStagePos.x, neighbourStagePos.y].tiles[neighbourRoomPos.x, neighbourRoomPos.y].blockType;

                if (neighbourType == BlockType.Spike ||
                    neighbourType == BlockType.Coin ||
                    neighbourType == BlockType.Foliage ||
                    neighbourType == BlockType.Torch)
                {
                    // Delete neighbouring spikes, coins, foliage, torches to all sides
                    RemoveTile(neighbourStagePos.x, neighbourStagePos.y, neighbourRoomPos.x, neighbourRoomPos.y);
                }

                if (x == 0 && y == 1)
                {
                    // Directly above the door
                    
                    if (neighbourType == BlockType.Ladder)
                    {
                        // Delete neighbouring ladders above
                        RemoveTile(neighbourStagePos.x, neighbourStagePos.y, neighbourRoomPos.x, neighbourRoomPos.y);

                        // Continuously delete ladders above until none remain
                        int offset = 1;
                        Vector2Int neighbourAboveStagePos = GridToStage(spawnPosition.x + x, spawnPosition.y + y + offset);
                        Vector2Int neighbourAboveRoomPos = GridToRoom(spawnPosition.x + x, spawnPosition.y + y + offset);
                        BlockType neighbourAboveType = level[neighbourAboveStagePos.x, neighbourAboveStagePos.y].tiles[neighbourAboveRoomPos.x, neighbourAboveRoomPos.y].blockType;

                        while (neighbourAboveType == BlockType.Ladder)
                        {
                            // Cap the ladder off correctly
                            RemoveTile(neighbourAboveStagePos.x, neighbourAboveStagePos.y, neighbourAboveRoomPos.x, neighbourAboveRoomPos.y);

                            offset += 1;
                            neighbourAboveStagePos = GridToStage(spawnPosition.x + x, spawnPosition.y + y + offset);
                            neighbourAboveRoomPos = GridToRoom(spawnPosition.x + x, spawnPosition.y + y + offset);
                            neighbourAboveType = level[neighbourAboveStagePos.x, neighbourAboveStagePos.y].tiles[neighbourAboveRoomPos.x, neighbourAboveRoomPos.y].blockType;
                        }
                    }
                    else if (neighbourType == BlockType.Vine)
                    {
                        // Correctly cap off vines above
                        RemoveTile(neighbourStagePos.x, neighbourStagePos.y, neighbourRoomPos.x, neighbourRoomPos.y);
                        PlaceTile(neighbourStagePos.x, neighbourStagePos.y, neighbourRoomPos.x, neighbourRoomPos.y, 57, BlockType.Vine);
                    }
                    else if (neighbourType == BlockType.Sign)
                    {
                        // Delete neighbouring signs above
                        RemoveTile(neighbourStagePos.x, neighbourStagePos.y, neighbourRoomPos.x, neighbourRoomPos.y);

                        // Keep deleting signs above that, until none remain
                        int offset = 1;
                        Vector2Int neighbourAboveStagePos = GridToStage(spawnPosition.x + x, spawnPosition.y + y + offset);
                        Vector2Int neighbourAboveRoomPos = GridToRoom(spawnPosition.x + x, spawnPosition.y + y + offset);
                        BlockType neighbourAboveType = level[neighbourAboveStagePos.x, neighbourAboveStagePos.y].tiles[neighbourAboveRoomPos.x, neighbourAboveRoomPos.y].blockType;

                        while (neighbourAboveType == BlockType.Sign)
                        {
                            RemoveTile(neighbourAboveStagePos.x, neighbourAboveStagePos.y, neighbourAboveRoomPos.x, neighbourAboveRoomPos.y);

                            offset += 1;
                            neighbourAboveStagePos = GridToStage(spawnPosition.x + x, spawnPosition.y + y + offset);
                            neighbourAboveRoomPos = GridToRoom(spawnPosition.x + x, spawnPosition.y + y + offset);
                            neighbourAboveType = level[neighbourAboveStagePos.x, neighbourAboveStagePos.y].tiles[neighbourAboveRoomPos.x, neighbourAboveRoomPos.y].blockType;
                        }
                    }
                }
            }
        }

        stepProgress = 0.5f;
        yield return null;

        // Cleanup exit

        // For each neighbour of this cell, at level or above
        for (int y = 1; y >= 0; y--)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                Vector2Int neighbourStagePos = GridToStage(exitPosition.x + x, exitPosition.y + y);
                Vector2Int neighbourRoomPos = GridToRoom(exitPosition.x + x, exitPosition.y + y);
                BlockType neighbourType = level[neighbourStagePos.x, neighbourStagePos.y].tiles[neighbourRoomPos.x, neighbourRoomPos.y].blockType;

                if (neighbourType == BlockType.Spike ||
                    neighbourType == BlockType.Coin ||
                    neighbourType == BlockType.Foliage ||
                    neighbourType == BlockType.Torch)
                {
                    // Delete neighbouring spikes, coins, foliage, torches to all sides
                    RemoveTile(neighbourStagePos.x, neighbourStagePos.y, neighbourRoomPos.x, neighbourRoomPos.y);
                }

                if (x == 0 && y == 1)
                {
                    // Directly above the door

                    if (neighbourType == BlockType.Ladder)
                    {
                        // Delete neighbouring ladders above
                        RemoveTile(neighbourStagePos.x, neighbourStagePos.y, neighbourRoomPos.x, neighbourRoomPos.y);

                        // Continuously delete ladders above until none remain
                        int offset = 1;
                        Vector2Int neighbourAboveStagePos = GridToStage(exitPosition.x + x, exitPosition.y + y + offset);
                        Vector2Int neighbourAboveRoomPos = GridToRoom(exitPosition.x + x, exitPosition.y + y + offset);
                        BlockType neighbourAboveType = level[neighbourAboveStagePos.x, neighbourAboveStagePos.y].tiles[neighbourAboveRoomPos.x, neighbourAboveRoomPos.y].blockType;

                        while (neighbourAboveType == BlockType.Ladder)
                        {
                            // Cap the ladder off correctly
                            RemoveTile(neighbourAboveStagePos.x, neighbourAboveStagePos.y, neighbourAboveRoomPos.x, neighbourAboveRoomPos.y);

                            offset += 1;
                            neighbourAboveStagePos = GridToStage(exitPosition.x + x, exitPosition.y + y + offset);
                            neighbourAboveRoomPos = GridToRoom(exitPosition.x + x, exitPosition.y + y + offset);
                            neighbourAboveType = level[neighbourAboveStagePos.x, neighbourAboveStagePos.y].tiles[neighbourAboveRoomPos.x, neighbourAboveRoomPos.y].blockType;
                        }
                    }
                    else if (neighbourType == BlockType.Vine)
                    {
                        // Correctly cap off vines above
                        RemoveTile(neighbourStagePos.x, neighbourStagePos.y, neighbourRoomPos.x, neighbourRoomPos.y);
                        PlaceTile(neighbourStagePos.x, neighbourStagePos.y, neighbourRoomPos.x, neighbourRoomPos.y, 57, BlockType.Vine);
                    }
                    else if (neighbourType == BlockType.Sign)
                    {
                        // Delete neighbouring signs above
                        RemoveTile(neighbourStagePos.x, neighbourStagePos.y, neighbourRoomPos.x, neighbourRoomPos.y);

                        // Keep deleting signs above that, until none remain
                        int offset = 1;
                        Vector2Int neighbourAboveStagePos = GridToStage(exitPosition.x + x, exitPosition.y + y + offset);
                        Vector2Int neighbourAboveRoomPos = GridToRoom(exitPosition.x + x, exitPosition.y + y + offset);
                        BlockType neighbourAboveType = level[neighbourAboveStagePos.x, neighbourAboveStagePos.y].tiles[neighbourAboveRoomPos.x, neighbourAboveRoomPos.y].blockType;

                        while (neighbourAboveType == BlockType.Sign)
                        {
                            RemoveTile(neighbourAboveStagePos.x, neighbourAboveStagePos.y, neighbourAboveRoomPos.x, neighbourAboveRoomPos.y);

                            offset += 1;
                            neighbourAboveStagePos = GridToStage(exitPosition.x + x, exitPosition.y + y + offset);
                            neighbourAboveRoomPos = GridToRoom(exitPosition.x + x, exitPosition.y + y + offset);
                            neighbourAboveType = level[neighbourAboveStagePos.x, neighbourAboveStagePos.y].tiles[neighbourAboveRoomPos.x, neighbourAboveRoomPos.y].blockType;
                        }
                    }
                }
            }
        }

        stepProgress = 1f;
        yield return null;
        CompleteStep();
    }

    // Step 24 of level generation
    // Iterates through all sign arrows in the level and ensures the direction they are pointing aligns with the critical path of the stage
    IEnumerator RedirectSigns()
    {
        int cleanupIterations = 0;

        // Ensure signs are pointing in the right direction (aligned with critical path)
        for (int y = 0; y < (stageSize.y * roomSize.y); y++)
        {
            for (int x = 0; x < (stageSize.x * roomSize.x); x++)
            {
                Vector2Int stagePos = GridToStage(x, y);
                Vector2Int roomPos = GridToRoom(x, y);
                int tileIndex = level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].tileIndex;

                // If this tile is an arrow sign
                if (tileIndex == 59 || tileIndex == 60)
                {
                    // The grid-space x value of the middle of the vertical access path on this floor
                    float verticalAccessMidpoint = -1f;

                    // Scan through row of rooms
                    for (int column = 0; column < stageSize.x; column++)
                    {
                        if (level[column, stagePos.y].roomType == LevelRoom.RoomType.Dropdown)
                        {
                            int gridMin = StageToGrid(column, stagePos.y, level[column, stagePos.y].verticalAccessMin, 0).x;
                            int gridMax = StageToGrid(column, stagePos.y, level[column, stagePos.y].verticalAccessMax, 0).x;

                            verticalAccessMidpoint = (gridMax - gridMin) / 2f;
                            verticalAccessMidpoint += gridMin;
                            break;
                        }

                        if (level[column, stagePos.y].roomType == LevelRoom.RoomType.Exit)
                        {
                            verticalAccessMidpoint = exitPosition.x;
                            break;
                        }
                    }

                    // Delete the old tile...
                    RemoveTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y);

                    // ...and replace it with one facing the correct direction
                    if (verticalAccessMidpoint - x > 0)
                        PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, 60, BlockType.Sign);
                    else
                        PlaceTile(stagePos.x, stagePos.y, roomPos.x, roomPos.y, 59, BlockType.Sign);
                }

                cleanupIterations++;
                stepProgress = (float)cleanupIterations / (stageSize.x * stageSize.y * roomSize.x * roomSize.y);
                if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                {
                    stopwatch.Restart();
                    yield return null;
                }
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 25 of level generation
    // Ensures all coins and the end goal are not unreachable due to spikes
    IEnumerator VerifyPaths()
    {
        int cleanupIterations = 0;

        // Weighted Dijkstra implementation

        // Start the pathfinding from the spawn point
        dijkstraPathfinding.SetTarget(spawnPosition.x, spawnPosition.y);

        // Set the tile weights for pathfinding
        for (int stageY = 0; stageY < stageSize.y; stageY++)
        {
            for (int stageX = 0; stageX < stageSize.x; stageX++)
            {
                for (int roomY = 0; roomY < roomSize.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.x; roomX++)
                    {
                        Vector2Int gridPos = StageToGrid(stageX, stageY, roomX, roomY);

                        // Set the weight of the tile according to what type it is
                        // Solid tiles are marked as non-traversable
                        // Spikes are very undesirable, but can be traversed
                        // Everything else can be traversed as standard
                        switch (level[stageX, stageY].tiles[roomX, roomY].blockType)
                        {
                            case BlockType.Solid:
                                dijkstraPathfinding.SetNodeWeight(gridPos.x, gridPos.y, -1);
                                break;
                            case BlockType.Spike:
                                dijkstraPathfinding.SetNodeWeight(gridPos.x, gridPos.y, 1000);
                                break;
                            default:
                                dijkstraPathfinding.SetNodeWeight(gridPos.x, gridPos.y, 1);
                                break;
                        }

                        if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                        {
                            stopwatch.Restart();
                            yield return null;
                        }
                    }
                }
            }
        }

        // Build a traversal map using the weights provided 
        dijkstraPathfinding.BakeFullGraph();

        // Check every coin and ensure it's possible to reach
        for (int stageY = 0; stageY < stageSize.y; stageY++)
        {
            for (int stageX = 0; stageX < stageSize.x; stageX++)
            {
                for (int roomY = 0; roomY < roomSize.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.x; roomX++)
                    {
                        if (level[stageX, stageY].tiles[roomX, roomY].blockType == BlockType.Coin)
                        {
                            // Get a path to this coin from the origin
                            Vector2Int gridPos = StageToGrid(stageX, stageY, roomX, roomY);
                            List<Vector2Int> pathToCoin = dijkstraPathfinding.ShortestPathTo(gridPos.x, gridPos.y);

                            // Iterate through the path
                            for (int pathStep = 0; pathStep < pathToCoin.Count; pathStep++)
                            {
                                Vector2Int pathStagePos = GridToStage(pathToCoin[pathStep].x, pathToCoin[pathStep].y);
                                Vector2Int pathRoomPos = GridToRoom(pathToCoin[pathStep].x, pathToCoin[pathStep].y);

                                // If there is a spike on the path, then it is most likely unavoidable, so it should be removed
                                if (level[pathStagePos.x, pathStagePos.y].tiles[pathRoomPos.x, pathRoomPos.y].blockType == BlockType.Spike)
                                {
                                    RemoveTile(pathStagePos.x, pathStagePos.y, pathRoomPos.x, pathRoomPos.y);
                                }
                            }
                        }

                        cleanupIterations++;
                        stepProgress = (float)cleanupIterations / (stageSize.x * stageSize.y * roomSize.x * roomSize.y);
                        if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                        {
                            stopwatch.Restart();
                            yield return null;
                        }
                    }
                }
            }
        }

        // Get a path to the exit from the origin
        List<Vector2Int> pathToExit = dijkstraPathfinding.ShortestPathTo(exitPosition.x, exitPosition.y);

        // Iterate through the path
        for (int pathStep = 0; pathStep < pathToExit.Count; pathStep++)
        {
            Vector2Int pathStagePos = GridToStage(pathToExit[pathStep].x, pathToExit[pathStep].y);
            Vector2Int pathRoomPos = GridToRoom(pathToExit[pathStep].x, pathToExit[pathStep].y);

            // If there is a spike on the path to the exit, then it is most likely unavoidable, so it should be removed
            if (level[pathStagePos.x, pathStagePos.y].tiles[pathRoomPos.x, pathRoomPos.y].blockType == BlockType.Spike)
            {
                RemoveTile(pathStagePos.x, pathStagePos.y, pathRoomPos.x, pathRoomPos.y);
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 26 of level generation
    // Fills the empty spaces of the map with air tiles, this might be useful at some point
    IEnumerator FillEmptyAreas()
    {
        int cleanupIterations = 0;

        for (int stageY = 0; stageY < stageSize.y; stageY++)
        {
            for (int stageX = 0; stageX < stageSize.x; stageX++)
            {
                for (int roomY = 0; roomY < roomSize.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.x; roomX++)
                    {
                        if (level[stageX, stageY].tiles[roomX, roomY].tileIndex == -1)
                        {
                            PlaceTile(stageX, stageY, roomX, roomY, 48, BlockType.None);
                        }

                        cleanupIterations++;
                        stepProgress = (float)cleanupIterations / (stageSize.x * stageSize.y * roomSize.x * roomSize.y);

                        if (stopwatch.Elapsed.TotalSeconds >= maxTimePerFrame)
                        {
                            stopwatch.Restart();
                            yield return null;
                        }
                    }
                }
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 27 of level generation
    // Counts the coins and other stats of the level for configuration
    IEnumerator ConfigureStage()
    {
        for (int y = 0; y < stageSize.y * roomSize.y; y++)
        {
            for (int x = 0; x < stageSize.x * roomSize.x; x++)
            {
                Vector2Int stagePos = GridToStage(x, y);
                Vector2Int roomPos = GridToRoom(x, y);

                if (level[stagePos.x, stagePos.y].tiles[roomPos.x, roomPos.y].blockType == BlockType.Coin) 
                    totalCoins += 1;
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 28 of level generation
    // Replaces placeholder tiles with prefabs where required, for things like coins
    IEnumerator SubstitutePrefabs()
    {
        yield return null;
        CompleteStep();
    }

    // Step 29 of level generation
    // Generates the colliders used by solids, platforms, ladders and spikes
    IEnumerator GenerateColliders()
    {
        levelTileManager.RecalculateAllComponents();

        yield return null;
        CompleteStep();
    }

    #endregion
}