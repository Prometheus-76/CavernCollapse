using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Stores the user settings for the game

[CreateAssetMenu(fileName = "GameSettings")]
public class GameSettings : ScriptableObject
{
    public int musicVolume;
    public int soundVolume;
    public int targetFPS;
}
