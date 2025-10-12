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
    
    [FormerlySerializedAs("mapWidth")] [Header("General")]
    public Vector3Int caveSize = new(50, 25, 50);
    public int floorHeight = 5;
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
    [Range(0.01f, 1.0f)] public float floorPerlinScale = 0.2f;
    [Range(0.01f, 5.0f)] public float floorPerlinAmplitude = 1.5f;
    [Range(0.01f, 1.0f)] public float ceilingPerlinScale = 0.5f;
    [Range(0.01f, 5.0f)] public float ceilingPerlinAmplitude = 2.5f;
    
    [Header("Environment")]
    [Range(0.01f, 1.0f)] public float environmentPerlinScale = 0.5f;
    [Range(0.01f, 1.0f)] public float environmentPerlin = 0.25f;
    
}
