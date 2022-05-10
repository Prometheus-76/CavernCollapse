using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileCollection")]
public class TileCollection : ScriptableObject
{
    public Tile[] tiles;
}
