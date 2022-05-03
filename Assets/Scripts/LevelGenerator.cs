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
    }

    #endregion

    #region Variables

    [SerializeField, Tooltip("The dimensions of the stage (in rooms)")] private Vector2Int stageDimensions;
    [SerializeField, Tooltip("The dimensions of each room (in tiles)")] private Vector2Int roomDimensions;

    #region Private

    private LevelRoom[,] level;
    private List<Vector2Int> criticalPath;

    #endregion

    #endregion

    // Awake is called when the script instance is loaded
    void Awake()
    {
        // Allocate and initialise the level, all rooms and all default tiles within those rooms
        InitialiseLevel();
        criticalPath = new List<Vector2Int>();

        GenerateLevel();
    }

    void InitialiseLevel()
    {
        // Create the level (an array of rooms)
        level = new LevelRoom[stageDimensions.x, stageDimensions.y];

        // ...and all rooms within it
        for (int y = 0; y < stageDimensions.y; y++)
        {
            for (int x = 0; x < stageDimensions.x; x++)
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
        level[roomX, roomY].tiles = new LevelTile[roomDimensions.x, roomDimensions.y];

        // ...and all tiles within that
        for (int y = 0; y < roomDimensions.y; y++)
        {
            for (int x = 0; x < roomDimensions.x; x++)
            {
                level[roomX, roomY].tiles[x, y] = new LevelTile();

                level[roomX, roomY].tiles[x, y].room = level[roomX, roomY];
            }
        }
    }

    void ResetLevelData()
    {
        // For every room...
        for (int stageY = 0; stageY < stageDimensions.y; stageY++)
        {
            for (int stageX = 0; stageX < stageDimensions.x; stageX++)
            {
                level[stageX, stageY].onCriticalPath = false;
                level[stageX, stageY].roomType = LevelRoom.RoomType.Unassigned;

                // ...and every tile in that room
                for (int roomY = 0; roomY < roomDimensions.y; roomY++)
                {
                    for (int roomX = 0; roomX < roomDimensions.x; roomX++)
                    {

                    }
                }
            }
        }
    }

    void GenerateLevel()
    {
        // Set spawn and winding snake order of rooms from top to bottom
        CreateRoomOrder();
    }

    void CreateRoomOrder()
    {
        criticalPath.Clear();

        // The x value of the room which will be the starting point of the level
        int startRoomIndex = Random.Range(0, stageDimensions.x);

        // Starting from the spawn room, develop the room order moving downwards
        Vector2Int currentRoom = new Vector2Int(startRoomIndex, stageDimensions.y - 1);
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
            else if (currentRoom.x >= stageDimensions.x - 1)
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
                    moveDirection = Random.Range(0, Mathf.CeilToInt(stageDimensions.x / 2f)) == 0 ? 0 : moveDirection;
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

    private void OnDrawGizmos()
    {
        // Only draw during play
        if (level != null)
        {
            
        }
    }
}
