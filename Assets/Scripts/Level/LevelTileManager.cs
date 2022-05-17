using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Responsible for placing tiles on different tilemaps and objects
public class LevelTileManager : MonoBehaviour
{
    public TileCollection tileCollection;
    public Tilemap solidTilemap;

    // Puts a tile in the correct tilemap, given an index and a type
    public void PlaceTileOfType(int x, int y, int tileIndex, BlockType tileType)
    {
        Vector3Int tilemapPos = new Vector3Int(x, y, 0);

        switch (tileType)
        {
            default:
                solidTilemap.SetTile(tilemapPos, tileCollection.tiles[tileIndex]);
                break;
        }
    }

    public void RemoveTile(int x, int y)
    {
        Vector3Int position = Vector3Int.zero;

        position.x = x;
        position.y = y;

        solidTilemap.SetTile(position, null);
    }

    // Recalculates all the tilemap colliders and other things
    public void RecalculateAllComponents()
    {

    }
}
