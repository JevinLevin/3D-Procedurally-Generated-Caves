using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable] 
[CreateAssetMenu(fileName = "GenerationSettings", menuName = "ScriptableObjects/Generation/GenerationSettings")]
public class GenerationSettingsScriptableObject : ScriptableObject
{
    
    [Header("General")]
    public int mapWidth = 50;
    public int mapHeight = 50;
    public int roomThreshold = 6;
    
    [Header("Random Walker")]
    public int maximumWalkers = 10;

    [Range(0.05f, 1)] public float redirectChance = 0.5f;
    [Range(0.05f, 1)] public float removeChance = 0.5f;
    [Range(0.05f, 1)] public float createChance = 0.5f;
    [Range(0,0.9f)] public float fillPercentage = 0.4f;

    [Header("Cellular Automaton")] 
    public int cAIterations = 3;

    [Header("Perlin Noise")] 
    public float environmentPerlinScale = 0.5f;
    public float environmentPerlin = 0.25f;
    
}
