using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    #region Structs

    // Represents one tile in the level
    struct LevelTile
    {
        public LevelRoom room;

        // Variable between stages
        public bool isReservedSpace;
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

    [SerializeField, Tooltip("The dimensions of the stage (in rooms)")] private Vector2Int stageSize;
    [SerializeField, Tooltip("The dimensions of each room (in tiles)")] private Vector2Int roomSize;

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

        GenerateLevel();
    }

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
        ResetLevelData();
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

                level[roomX, roomY].tiles[x, y].room = level[roomX, roomY];
            }
        }
    }

    void ResetLevelData()
    {
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
                        level[stageX, stageY].tiles[roomX, roomY].isReservedSpace = false;
                    }
                }
            }
        }
        
        criticalPath.Clear();
    }

    void GenerateLevel()
    {
        // Ensure we start from fresh
        ResetLevelData();

        // Set spawn and winding snake order of rooms from top to bottom
        CreateRoomOrder();

        // Build a critical path layout with some organic variation in every room to ensure a path exists
        ReserveRoomPaths();
    }

    void CreateRoomOrder()
    {
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
                    moveDirection = Random.Range(0, Mathf.CeilToInt(stageSize.x / 2f)) == 0 ? 0 : moveDirection;
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
    }

    // This is kind of shit but we can do better later!
    // TODO 
    void ReserveRoomPaths()
    {
        // For every room index in the critical path list...
        for (int i = 0; i < criticalPath.Count; i++)
        {
            // Get the room we will be working on
            LevelRoom room = level[criticalPath[i].x, criticalPath[i].y];

            // Choose a starting point on the left of the room above the ground (which cannot be destroyed... yet)
            int startHeight = Random.Range(1, roomSize.y);
            Vector2Int currentTile = new Vector2Int(0, startHeight);
            int moveDirection = 0;

            // Until the algorithm hits the end of the room (on the right)
            while (currentTile.x <= roomSize.x - 1)
            {
                if (currentTile.y <= 1)
                {
                    // Must move up or right
                    if (moveDirection == 0)
                    {
                        // Go up
                        moveDirection = 1;
                    }
                    else
                    {
                        // We hit a wall so go right
                        moveDirection = 0;
                    }
                }
                else if (currentTile.y >= roomSize.y - 1)
                {
                    // Must move down or right
                    if (moveDirection == 0)
                    {
                        // Go down
                        moveDirection = -1;
                    }
                    else
                    {
                        // We hit a wall so go right
                        moveDirection = 0;
                    }
                }
                else
                {
                    // Chance to move right, otherwise continue in same direction
                    moveDirection = Mathf.RoundToInt(Mathf.Clamp(Random.Range(-2, 3), -1f, 1f));
                }

                // Mark tile as being reserved
                room.tiles[currentTile.x, currentTile.y].isReservedSpace = true;

                // Move in the direction and connect a room
                if (moveDirection == 0)
                {
                    // Go right
                    currentTile.x += 1;
                }
                else
                {
                    // Move up or down
                    currentTile.y += moveDirection;
                }
            }
        }
    }
}
