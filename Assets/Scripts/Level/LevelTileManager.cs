using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Darcy Matheson 2022

// Responsible for placing tiles on different tilemaps and objects
public class LevelTileManager : MonoBehaviour
{
    public enum SpecialTile
    {
        Player,
        Coin,
        EntryDoor,
        ExitDoor
    }

    [Header("Hierarchy")]
    public Transform objectParent;
    public Transform coinParent;
    public Transform doorParent;

    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject coinPrefab;
    public GameObject entryDoor;
    public GameObject exitDoor;

    [Header("Tiles")]
    public TileCollection tileCollection;
    public CompositeCollider2D solidCollider;
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

    // Removes a tile at this position in all tilemaps
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

    // Places a special tile (it's actually just a prefab)
    public void PlaceSpecialTile(float x, float y, SpecialTile tileType)
    {
        GameObject instance;

        switch (tileType)
        {
            case SpecialTile.Player:
                instance = Instantiate<GameObject>(playerPrefab, objectParent);
                instance.transform.position = new Vector3(x, y, 0f);
                break;
            case SpecialTile.Coin:
                instance = Instantiate<GameObject>(coinPrefab, coinParent);
                instance.transform.position = new Vector3(x, y, 0f);
                break;
            case SpecialTile.EntryDoor:
                instance = Instantiate<GameObject>(entryDoor, doorParent);
                instance.transform.position = new Vector3(x, y, 0f);
                break;
            case SpecialTile.ExitDoor:
                instance = Instantiate<GameObject>(exitDoor, doorParent);
                instance.transform.position = new Vector3(x, y, 0f);
                break;
        }
    }

    // Recalculates all the tilemap colliders and other things
    public void RecalculateAllComponents()
    {
        solidCollider.GenerateGeometry();
    }
}
