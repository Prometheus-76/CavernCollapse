using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Responsible for placing tiles on different tilemaps and objects
public class LevelTileManager : MonoBehaviour
{
    public enum SpecialTile
    {
        EntryDoor,
        ExitDoor
    }

    public TileCollection tileCollection;
    public GameObject entryDoor;
    public GameObject exitDoor;

    public Tilemap solidTilemap;
    public Tilemap platformTilemap;
    public Tilemap ladderTilemap;
    public Tilemap spikeTilemap;
    public Tilemap decoTilemap;

    // Puts a tile in the correct tilemap, given an index and a type
    public void PlaceTileOfType(int x, int y, int tileIndex, BlockType tileType)
    {
        Vector3Int tilemapPos = new Vector3Int(x, y, 0);

        switch (tileType)
        {
            case BlockType.Solid:
                solidTilemap.SetTile(tilemapPos, tileCollection.tiles[tileIndex]);
                break;
            case BlockType.OneWay:
                platformTilemap.SetTile(tilemapPos, tileCollection.tiles[tileIndex]);
                break;
            case BlockType.Ladder:
                ladderTilemap.SetTile(tilemapPos, tileCollection.tiles[tileIndex]);
                break;
            case BlockType.Spike:
                spikeTilemap.SetTile(tilemapPos, tileCollection.tiles[tileIndex]);
                break;
            default:
                decoTilemap.SetTile(tilemapPos, tileCollection.tiles[tileIndex]);
                break;
        }
    }

    public void RemoveTile(int x, int y)
    {
        Vector3Int position = Vector3Int.zero;

        position.x = x;
        position.y = y;

        solidTilemap.SetTile(position, null);
        platformTilemap.SetTile(position, null);
        ladderTilemap.SetTile(position, null);
        spikeTilemap.SetTile(position, null);
        decoTilemap.SetTile(position, null);
    }

    public void PlaceSpecialTile(int x, int y, SpecialTile tileType)
    {
        GameObject instance;

        switch (tileType)
        {
            case SpecialTile.EntryDoor:
                instance = Instantiate<GameObject>(entryDoor, transform);
                instance.transform.position = new Vector3(x, y, 0f);
                break;
            case SpecialTile.ExitDoor:
                instance = Instantiate<GameObject>(exitDoor, transform);
                instance.transform.position = new Vector3(x, y, 0f);
                break;
        }
    }

    // Recalculates all the tilemap colliders and other things
    public void RecalculateAllComponents()
    {

    }
}
