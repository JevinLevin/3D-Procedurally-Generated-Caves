using UnityEngine;

[CreateAssetMenu(fileName = "Tile", menuName = "ScriptableObjects/Generation/Tile")]
public class TileScriptableObject : ScriptableObject
{
    [Header("Components")]
    public Mesh mesh;
    public Material material;
    
    [Header("Attributes")]
    public int weight = 1;
    public int maxHealth;
    public int value;
    public Color particleColors;
}
