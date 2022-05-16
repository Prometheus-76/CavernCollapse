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
        public LevelRoom room;

        // Whether a tile has been locked in or not 
        public bool tileAssigned;
        public bool onCriticalPath;

        public int[] possibleTiles;
        public BlockType blockType;
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
        public bool onCriticalPath;
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
        GenerationComplete
    }

    private GenerationStep currentStep;
    private float mainProgress;
    [HideInInspector] public float stepProgress;
    private bool awaitingNewStep;

    [Header("Configuration")]
    [SerializeField, Tooltip("The amount of resetting iterations to do per frame")] private int resetIterationsPerFrame;
    [SerializeField, Tooltip("The amount of floors of rooms to create per frame")] private int roomSequenceFloorsPerFrame;
    [SerializeField, Tooltip("The amount of rooms to reserve a path through per frame")] private int pathReservationRoomsPerFrame;
    [SerializeField, Tooltip("The dimensions of the stage (in rooms)")] private Vector2Int stageSize;
    [SerializeField, Tooltip("The dimensions of each room (in tiles)")] private Vector2Int roomSize;

    [Header("Components")]
    public TileCollection tileCollection;
    public GameplayConfiguration gameplayConfiguration;
    public LoadingScreen loadingScreen;
    public DatasetAnalyser datasetAnalyser;

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
            case GenerationStep.GenerationComplete:
                loadingScreen.SetStepText("Stage generation complete");
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
        Debug.Log("Generation complete!");
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

                level[roomX, roomY].tiles[x, y].room = level[roomX, roomY];
            }
        }
    }

    #endregion

    // Coroutines are dangerous, so I have to be super careful about when things are called in here!
    IEnumerator GenerateLevel()
    {
        currentStep = (GenerationStep)0;
        awaitingNewStep = true;

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
                level[stageX, stageY].onCriticalPath = false;
                level[stageX, stageY].roomType = LevelRoom.RoomType.Unassigned;
                level[stageX, stageY].verticalAccessMin = -1;
                level[stageX, stageY].verticalAccessMax = -1;

                // ...and every tile in that room
                for (int roomY = 0; roomY < roomSize.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomSize.x; roomX++)
                    {
                        level[stageX, stageY].tiles[roomX, roomY].tileAssigned = false;
                        level[stageX, stageY].tiles[roomX, roomY].onCriticalPath = false;
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
            level[currentRoom.x, currentRoom.y].onCriticalPath = true;
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
    // Builds a path through the room using a biased drunk walking algorithm
    IEnumerator ReserveRoomPaths()
    {
        int roomPathsCreated = 0;
        Vector2Int lastTile = Vector2Int.zero;

        // For every room in the level
        for (int r = 0; r < criticalPath.Count; r++)
        {
            bool pathCompleted = false;
            Vector2Int nextRoomDirection = Vector2Int.zero; // The direction to the next room in the sequence
            Vector2Int lastMoveDirection = Vector2Int.zero;

            if (r + 1 < criticalPath.Count)
            {
                // For each room after the first one
                nextRoomDirection = criticalPath[r + 1] - criticalPath[r];
            }

            // If the current room requires a fresh start point for the algorithm
            if (level[criticalPath[r].x, criticalPath[r].y].roomType == LevelRoom.RoomType.Landing ||
                level[criticalPath[r].x, criticalPath[r].y].roomType == LevelRoom.RoomType.Spawn)
            {
                if (r + 1 < criticalPath.Count)
                {
                    // Set the start tile to the back of the room, furthest from the next one
                    if (nextRoomDirection.x == -1)
                        lastTile.x = roomSize.x - 1;
                    else
                        lastTile.x = 0;
                    
                    lastTile.y = Random.Range(1, roomSize.y - 1);
                }
            }

            // Drunk walking algorithm until the path trace inside the room is completed
            while (pathCompleted == false)
            {
                if (Random.Range(0, 2) == 0)
                {
                    // Move in a new direction

                }
                else
                {
                    // Continue in same direction

                }

                break;
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

    #endregion

    // Draw the stage arrangement of rooms with gizmos
    private void OnDrawGizmos()
    {
        if (currentStep == GenerationStep.GenerationComplete && criticalPath != null)
        {
            for (int r = 0; r < criticalPath.Count; r++)
            {
                Vector3 position = Vector3.zero;
                position.x = criticalPath[r].x;
                position.y = criticalPath[r].y;
                Gizmos.DrawSphere(position, 0.5f);
            }
        }
    }
}
