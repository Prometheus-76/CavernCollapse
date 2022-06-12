using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Darcy Matheson 2022

// Stores settings related to the current run (determined on the "play" screen within main menu)
[CreateAssetMenu(fileName = "GameplayConfiguration")]
public class GameplayConfiguration : ScriptableObject
{
    public enum DifficultyOptions
    {
        beginner,
        standard,
        expert
    }

    public int dataset;
    public DifficultyOptions difficulty;
}
