using UnityEngine;

public class CaveTile : MonoBehaviour
{
    [SerializeField] private TileScriptableObject tile;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    
  
    public void Setup(TileScriptableObject tile)
    {
        this.tile = tile;

        meshFilter.mesh = tile.mesh;
        meshRenderer.material = tile.material;
    }
}
