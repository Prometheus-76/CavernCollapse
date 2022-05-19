using System.Collections;
using System.Collections.Generic;
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

        // Whether a tile has been locked in or not 
        public bool tileAssigned;

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
        LoadDataset,
        ConstructRuleset,
        AssembleRoomSequence,
        ReserveRoomPaths,
        CreateMapBorder,
        ConnectVerticalRooms,
        CreateRoomBorders,
        WaveFunctionCollapseWalls,
        WaveFunctionCollapsePlatforming,
        WaveFunctionCollapseGameplay,
        WaveFunctionCollapseDeco,
        PlaceSpawnAndExit,
        GenerateColliders,
        GenerationComplete
    }

    private GenerationStep currentStep;
    private float mainProgress;
    [HideInInspector] public float stepProgress;
    private bool awaitingNewStep;
    private int seed;

    [Header("Configuration")]
    [SerializeField, Tooltip("The amount of resetting iterations to do per frame")] private int resetIterationsPerFrame;
    [SerializeField, Tooltip("The amount of floors of rooms to create per frame")] private int roomSequenceFloorsPerFrame;
    [SerializeField, Tooltip("The amount of rooms to reserve a path through per frame")] private int pathReservationRoomsPerFrame;
    [SerializeField, Tooltip("The amount of rooms to set up the map borders within per frame")] private int roomMapBordersPerFrame;
    [SerializeField, Tooltip("The amount of rooms to connect vertically per frame")] private int verticalRoomConnectionsPerFrame;
    [SerializeField, Tooltip("The amount of room borders to set up per frame")] private int roomBordersPerFrame;
    [SerializeField, Tooltip("The amount of tiles to collapse per frame")] private int tilesCollapsedPerFrame;
    [SerializeField, Tooltip("The dimensions of the stage (in rooms)")] private Vector2Int stageSize;
    [SerializeField, Tooltip("The dimensions of each room (in tiles)")] private Vector2Int roomSize;

    [Header("Components")]
    public TileCollection tileCollection;
    public GameplayConfiguration gameplayConfiguration;
    public LoadingScreen loadingScreen;
    public DatasetAnalyser datasetAnalyser;
    public LevelTileManager levelTileManager;
    public WaveFunctionCollapse waveFunctionCollapse;
    public CameraController cameraController;

    #region Private

    private LevelRoom[,] level;
    private List<Vector2Int> criticalPath;

    #endregion

    #endregion

    // Awake is called when the script instance is loaded
    void Awake()
    {
        criticalPath = new List<Vector2Int>();

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
        mainProgress = (float)currentStep / (float)GenerationStep.GenerationComplete;
        mainProgress += (1f / (float)GenerationStep.GenerationComplete) * stepProgress;

        loadingScreen.SetMainProgress(mainProgress);
        loadingScreen.SetStepProgress(stepProgress);

        switch (currentStep)
        {
            case GenerationStep.ResetStageData:
                loadingScreen.SetStepText("Resetting stage data");
                break;
            case GenerationStep.LoadDataset:
                loadingScreen.SetStepText("Loading dataset samples");
                break;
            case GenerationStep.ConstructRuleset:
                loadingScreen.SetStepText("Constructing wave function");
                break;
            case GenerationStep.AssembleRoomSequence:
                loadingScreen.SetStepText("Assembling room sequence");
                break;
            case GenerationStep.ReserveRoomPaths:
                loadingScreen.SetStepText("Reserving room paths");
                break;
            case GenerationStep.CreateMapBorder:
                loadingScreen.SetStepText("Building map border");
                break;
            case GenerationStep.ConnectVerticalRooms:
                loadingScreen.SetStepText("Connecting vertical rooms");
                break;
            case GenerationStep.CreateRoomBorders:
                loadingScreen.SetStepText("Building room borders");
                break;
            case GenerationStep.WaveFunctionCollapseWalls:
                loadingScreen.SetStepText("Collapsing cavern walls");
                break;
            case GenerationStep.WaveFunctionCollapsePlatforming:
                loadingScreen.SetStepText("Placing platforms and ladders");
                break;
            case GenerationStep.WaveFunctionCollapseGameplay:
                loadingScreen.SetStepText("Setting up risk and reward");
                break;
            case GenerationStep.WaveFunctionCollapseDeco:
                loadingScreen.SetStepText("Growing vines and foliage");
                break;
            case GenerationStep.PlaceSpawnAndExit:
                loadingScreen.SetStepText("Placing doors");
                break;
            case GenerationStep.GenerateColliders:
                loadingScreen.SetStepText("Generating colliders");
                break;
            case GenerationStep.GenerationComplete:
                loadingScreen.SetStepText("Entering caverns...");
                break;
        }
    }

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
    }

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

    void PlaceTile(int stageX, int stageY, int roomX, int roomY, int tileIndex, BlockType blockType)
    {        
        level[stageX, stageY].tiles[roomX, roomY].blockType = blockType;
        level[stageX, stageY].tiles[roomX, roomY].tileIndex = tileIndex;

        Vector2Int gridPosition = StageToGrid(stageX, stageY, roomX, roomY);
        levelTileManager.PlaceTileOfType(gridPosition.x, gridPosition.y, tileIndex, blockType);
        waveFunctionCollapse.SetTile(gridPosition.x, gridPosition.y, blockType, tileIndex);
    }

    void RemoveTile(int stageX, int stageY, int roomX, int roomY)
    {
        level[stageX, stageY].tiles[roomX, roomY].blockType = BlockType.None;
        level[stageX, stageY].tiles[roomX, roomY].tileIndex = -1;
        level[stageX, stageY].tiles[roomX, roomY].tileAssigned = false;

        Vector2Int gridPosition = StageToGrid(stageX, stageY, roomX, roomY);
        levelTileManager.RemoveTile(gridPosition.x, gridPosition.y);
        waveFunctionCollapse.ResetTile(gridPosition.x, gridPosition.y);
    }

    // Coroutines can be dangerous, so I have to be super careful about when things are called in here!
    IEnumerator GenerateLevel()
    {
        // Randomise the seed
        seed = Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(seed);

        currentStep = (GenerationStep)0;
        awaitingNewStep = true;

        waveFunctionCollapse.InitialiseWaveFunction(stageSize.x * roomSize.x, stageSize.y * roomSize.y);

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
                    case GenerationStep.WaveFunctionCollapsePlatforming:
                        StartCoroutine(WaveFunctionCollapsePlatforming());
                        break;
                    case GenerationStep.WaveFunctionCollapseGameplay:
                        StartCoroutine(WaveFunctionCollapseGameplay());
                        break;
                    case GenerationStep.WaveFunctionCollapseDeco:
                        StartCoroutine(WaveFunctionCollapseDeco());
                        break;
                    case GenerationStep.PlaceSpawnAndExit:
                        StartCoroutine(PlaceSpawnAndExit());
                        break;
                    case GenerationStep.GenerateColliders:
                        StartCoroutine(GenerateColliders());
                        break;
                    case GenerationStep.GenerationComplete: 
                        // This should be unreachable
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
                        level[stageX, stageY].tiles[roomX, roomY].tileAssigned = false;
                        level[stageX, stageY].tiles[roomX, roomY].reservedTile = false;
                        level[stageX, stageY].tiles[roomX, roomY].blockType = BlockType.None;

                        iterationsCompleted++;
                        stepProgress = (float)iterationsCompleted / (stageSize.y * stageSize.x * roomSize.y * roomSize.x);
                        if (iterationsCompleted % resetIterationsPerFrame == 0)
                            yield return null;
                    }
                }

            }
        }
        
        criticalPath.Clear();
        yield return null;
        CompleteStep();
    }

    // Step 2 is done in DatasetAnalyser class
    // Step 3 is done in DatasetAnalyser class

    // Step 4 of level generation
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
                if (levelsCreated % roomSequenceFloorsPerFrame == 0)
                    yield return null;
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

    // Step 5 of level generation
    // Builds a path through the room using a biased drunk walking algorithm (padded 2 off the ceiling and 2 off the ground)
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
            if (roomPathsCreated % pathReservationRoomsPerFrame == 0)
                yield return null;
        }

        yield return null;
        CompleteStep();
    }

    // Step 6 of level generation
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
                if (roomMapBordersCreated % roomMapBordersPerFrame == 0)
                    yield return null;
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 7 of level generation
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
                    for (int x = 1; x < roomSize.x - 1; x++)
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
            if (roomCutoutsCompleted % verticalRoomConnectionsPerFrame == 0)
                yield return null;
        }

        yield return null;
        CompleteStep();
    }

    // Step 8 of level generation
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
                if (roomBordersCreated % roomBordersPerFrame == 0)
                    yield return null;
            }
        }

        yield return null;
        CompleteStep();
    }

    // Step 9 of level generation
    // Wave function collapse pass for walls and air
    IEnumerator WaveFunctionCollapseWalls()
    {
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
            }
        }

        int uncollapsedTiles = waveFunctionCollapse.GetUncollapsedCount();

        int positionsCollapsed = 0;
        Vector2Int positionToCollapse;
        while (true)
        {
            positionToCollapse = waveFunctionCollapse.GetLowestEntropyTile();

            if (positionToCollapse.x != -1)
            {
                // Collapse this position
                if (waveFunctionCollapse.CollapseTile(positionToCollapse.x, positionToCollapse.y))
                {
                    BlockType collapsedType = waveFunctionCollapse.GetCollapsedType(positionToCollapse.x, positionToCollapse.y);
                    int collapsedTileIndex = waveFunctionCollapse.GetCollapsedTileIndex(positionToCollapse.x, positionToCollapse.y);
                    Vector2Int stagePos = GridToStage(positionToCollapse.x, positionToCollapse.y);
                    Vector2Int roomPos = GridToRoom(positionToCollapse.x, positionToCollapse.y);
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
                }

                positionsCollapsed++;
                stepProgress = (float)positionsCollapsed / uncollapsedTiles;
                if (positionsCollapsed % tilesCollapsedPerFrame == 0)
                    yield return null;
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

    // Step 10 of level generation
    // Wave function collapse pass for ladders and platforms
    IEnumerator WaveFunctionCollapsePlatforming()
    {
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
        waveFunctionCollapse.AddToBlockPalette(BlockType.OneWay);
        waveFunctionCollapse.AddToBlockPalette(BlockType.Ladder);

        // Calculate entropy of entire wave function collapse grid
        for (int y = 0; y < (stageSize.y * roomSize.y); y++)
        {
            for (int x = 0; x < (stageSize.x * roomSize.x); x++)
            {
                waveFunctionCollapse.RecalculateEntropy(x, y);
            }
        }

        int uncollapsedTiles = waveFunctionCollapse.GetUncollapsedCount();

        int positionsCollapsed = 0;
        Vector2Int positionToCollapse;
        while (true)
        {
            positionToCollapse = waveFunctionCollapse.GetLowestEntropyTile();

            if (positionToCollapse.x != -1)
            {
                // Collapse this position
                if (waveFunctionCollapse.CollapseTile(positionToCollapse.x, positionToCollapse.y))
                {
                    BlockType collapsedType = waveFunctionCollapse.GetCollapsedType(positionToCollapse.x, positionToCollapse.y);
                    int collapsedTileIndex = waveFunctionCollapse.GetCollapsedTileIndex(positionToCollapse.x, positionToCollapse.y);
                    Vector2Int stagePos = GridToStage(positionToCollapse.x, positionToCollapse.y);
                    Vector2Int roomPos = GridToRoom(positionToCollapse.x, positionToCollapse.y);
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
                }

                positionsCollapsed++;
                stepProgress = (float)positionsCollapsed / uncollapsedTiles;
                if (positionsCollapsed % tilesCollapsedPerFrame == 0)
                    yield return null;
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

    // Step 11 of level generation
    // Wave function collapse pass for spikes and coins
    IEnumerator WaveFunctionCollapseGameplay()
    {
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
            }
        }

        int uncollapsedTiles = waveFunctionCollapse.GetUncollapsedCount();

        int positionsCollapsed = 0;
        Vector2Int positionToCollapse;
        while (true)
        {
            positionToCollapse = waveFunctionCollapse.GetLowestEntropyTile();

            if (positionToCollapse.x != -1)
            {
                // Collapse this position
                if (waveFunctionCollapse.CollapseTile(positionToCollapse.x, positionToCollapse.y))
                {
                    BlockType collapsedType = waveFunctionCollapse.GetCollapsedType(positionToCollapse.x, positionToCollapse.y);
                    int collapsedTileIndex = waveFunctionCollapse.GetCollapsedTileIndex(positionToCollapse.x, positionToCollapse.y);
                    Vector2Int stagePos = GridToStage(positionToCollapse.x, positionToCollapse.y);
                    Vector2Int roomPos = GridToRoom(positionToCollapse.x, positionToCollapse.y);
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
                }

                positionsCollapsed++;
                stepProgress = (float)positionsCollapsed / uncollapsedTiles;
                if (positionsCollapsed % tilesCollapsedPerFrame == 0)
                    yield return null;
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
    // Wave function collapse pass for foliage, vines, signs and torches
    IEnumerator WaveFunctionCollapseDeco()
    {
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
            }
        }

        int uncollapsedTiles = waveFunctionCollapse.GetUncollapsedCount();

        int positionsCollapsed = 0;
        Vector2Int positionToCollapse;
        while (true)
        {
            positionToCollapse = waveFunctionCollapse.GetLowestEntropyTile();

            if (positionToCollapse.x != -1)
            {
                // Collapse this position
                if (waveFunctionCollapse.CollapseTile(positionToCollapse.x, positionToCollapse.y))
                {
                    BlockType collapsedType = waveFunctionCollapse.GetCollapsedType(positionToCollapse.x, positionToCollapse.y);
                    int collapsedTileIndex = waveFunctionCollapse.GetCollapsedTileIndex(positionToCollapse.x, positionToCollapse.y);
                    Vector2Int stagePos = GridToStage(positionToCollapse.x, positionToCollapse.y);
                    Vector2Int roomPos = GridToRoom(positionToCollapse.x, positionToCollapse.y);
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
                }

                positionsCollapsed++;
                stepProgress = (float)positionsCollapsed / uncollapsedTiles;
                if (positionsCollapsed % tilesCollapsedPerFrame == 0)
                    yield return null;
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

    // Step 13 of level generation
    // Sets the player's spawn and end goal points in the level
    IEnumerator PlaceSpawnAndExit()
    {
        // Set spawn

        // Find highest point in critical path
        bool spawnPlaced = false;
        Vector2Int spawnPoint = Vector2Int.zero;
        for (int y = roomSize.y - 1; y >= 0; y--)
        {
            for (int x = 1; x < roomSize.x - 1; x++)
            {
                // Found the highest point in the critical path of the first room
                if (level[criticalPath[0].x, criticalPath[0].y].tiles[x, y].reservedTile)
                {
                    spawnPoint.x = x;
                    spawnPoint.y = y;

                    // Trace down until we hit ground
                    Vector2Int belowPoint = spawnPoint;
                    belowPoint.y -= 1;
                    BlockType typeBelow = level[criticalPath[0].x, criticalPath[0].y].tiles[belowPoint.x, belowPoint.y].blockType;
                    while (typeBelow != BlockType.Solid && typeBelow != BlockType.OneWay)
                    {
                        belowPoint.y -= 1;
                        typeBelow = level[criticalPath[0].x, criticalPath[0].y].tiles[belowPoint.x, belowPoint.y].blockType;
                    }

                    spawnPoint = belowPoint;
                    spawnPoint.y += 1;

                    // Remove anything that might have been in this space resting on the ground
                    RemoveTile(criticalPath[0].x, criticalPath[0].y, spawnPoint.x, spawnPoint.y);

                    // Place spawn door
                    Vector2Int doorPosition = StageToGrid(criticalPath[0].x, criticalPath[0].y, spawnPoint.x, spawnPoint.y);
                    levelTileManager.PlaceSpecialTile(doorPosition.x, doorPosition.y, LevelTileManager.SpecialTile.EntryDoor);

                    // Ensure both sides of the door are free of spikes
                    if (level[criticalPath[0].x, criticalPath[0].y].tiles[spawnPoint.x - 1, spawnPoint.y].blockType == BlockType.Spike)
                        RemoveTile(criticalPath[0].x, criticalPath[0].y, spawnPoint.x - 1, spawnPoint.y);

                    if (level[criticalPath[0].x, criticalPath[0].y].tiles[spawnPoint.x + 1, spawnPoint.y].blockType == BlockType.Spike)
                        RemoveTile(criticalPath[0].x, criticalPath[0].y, spawnPoint.x + 1, spawnPoint.y);

                    // Set camera starting position
                    Vector3 spawnPointAsVec3 = new Vector3(doorPosition.x, doorPosition.y, 0f);
                    cameraController.SetStartPosition(spawnPointAsVec3);

                    spawnPlaced = true;
                    break;
                }
            }

            if (spawnPlaced) break;
        }

        stepProgress = 0.5f;
        yield return null;

        // Set exit

        // Find highest point in critical path
        bool exitPlaced = false;
        Vector2Int exitPoint = Vector2Int.zero;
        for (int y = roomSize.y - 1; y >= 0; y--)
        {
            for (int x = 1; x < roomSize.x - 1; x++)
            {
                // Found the highest point in the critical path of the last room
                if (level[criticalPath[criticalPath.Count - 1].x, criticalPath[criticalPath.Count - 1].y].tiles[x, y].reservedTile)
                {
                    exitPoint.x = x;
                    exitPoint.y = y;

                    // Trace down until we hit ground
                    Vector2Int belowPoint = exitPoint;
                    belowPoint.y -= 1;
                    BlockType typeBelow = level[criticalPath[criticalPath.Count - 1].x, criticalPath[criticalPath.Count - 1].y].tiles[belowPoint.x, belowPoint.y].blockType;
                    while (typeBelow != BlockType.Solid && typeBelow != BlockType.OneWay)
                    {
                        belowPoint.y -= 1;
                        typeBelow = level[criticalPath[criticalPath.Count - 1].x, criticalPath[criticalPath.Count - 1].y].tiles[belowPoint.x, belowPoint.y].blockType;
                    }

                    exitPoint = belowPoint;
                    exitPoint.y += 1;

                    // Remove anything that might have been in this space resting on the ground
                    RemoveTile(criticalPath[criticalPath.Count - 1].x, criticalPath[criticalPath.Count - 1].y, exitPoint.x, exitPoint.y);

                    // Place exit door
                    Vector2Int doorPosition = StageToGrid(criticalPath[criticalPath.Count - 1].x, criticalPath[criticalPath.Count - 1].y, exitPoint.x, exitPoint.y);
                    levelTileManager.PlaceSpecialTile(doorPosition.x, doorPosition.y, LevelTileManager.SpecialTile.ExitDoor);

                    // Ensure both sides of the door are free of spikes
                    if (level[criticalPath[criticalPath.Count - 1].x, criticalPath[criticalPath.Count - 1].y].tiles[exitPoint.x - 1, exitPoint.y].blockType == BlockType.Spike)
                        RemoveTile(criticalPath[criticalPath.Count - 1].x, criticalPath[criticalPath.Count - 1].y, exitPoint.x - 1, exitPoint.y);

                    if (level[criticalPath[criticalPath.Count - 1].x, criticalPath[criticalPath.Count - 1].y].tiles[exitPoint.x + 1, exitPoint.y].blockType == BlockType.Spike)
                        RemoveTile(criticalPath[criticalPath.Count - 1].x, criticalPath[criticalPath.Count - 1].y, exitPoint.x + 1, exitPoint.y);

                    exitPlaced = true;
                    break;
                }
            }

            if (exitPlaced) break;
        }

        yield return null;
        CompleteStep();
    }

    // Step 14 of level generation
    // Generates the colliders used by solids, platforms, ladders and spikes
    IEnumerator GenerateColliders()
    {
        levelTileManager.RecalculateAllComponents();

        yield return null;
        CompleteStep();
    }

    #endregion

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
}
