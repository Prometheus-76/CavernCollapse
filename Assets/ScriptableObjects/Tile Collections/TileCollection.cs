using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Stores a collection of tiles used by the editor and game

[CreateAssetMenu(fileName = "TileCollection")]
public class TileCollection : ScriptableObject
{
    public Tile[] tiles;
}
