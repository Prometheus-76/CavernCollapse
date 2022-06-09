using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
