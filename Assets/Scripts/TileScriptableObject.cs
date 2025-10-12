using UnityEngine;

[CreateAssetMenu(fileName = "Tile", menuName = "ScriptableObjects/Generation/Tile")]
public class TileScriptableObject : ScriptableObject
{
    public Mesh mesh;
    public Material material;
    public int weight = 1;
}
