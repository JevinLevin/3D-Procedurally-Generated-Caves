using UnityEngine;
using Random = UnityEngine.Random;

public static class CaveUtilities
{

    /// <summary>
    ///  Copy grid from source to destination
    /// </summary>
    /// <param name="source">Original grid</param>
    /// <param name="destination">Target grid </param>
    /// <param name="xLength">Width</param>
    /// <param name="yLength">Height</param>

    public static void CopyGrid(CaveMask[,] source, CaveMask[,] destination, int xLength, int yLength)
    {
        for (int column = 0; column < xLength; ++column)
        {
            for (int row = 0; row < yLength; ++row)
            {
                destination[column, row].active = source[column, row].active;
            }
        }
    }
    
    public static void CopyMask(CaveMask[,] source, CaveCell[,,] destination, int height, int xLength, int yLength)
    {
        for (int column = 0; column < xLength; ++column)
        {
            for (int row = 0; row < yLength; ++row)
            {
                destination[column, height, row].Tile = source[column, row].active ? CaveCell.Tiles.Tile : CaveCell.Tiles.Empty;
            }
        }
    }
    
    /// <summary>
    ///  Check if coordinates are within grid bounds
    /// </summary>
    /// <param name="x">Horizontal Position</param>
    /// <param name="y">Vertical Position</param>
    /// <param name="xLength">Width</param>
    /// <param name="yLength">Height</param>
    public static bool IsInGrid(int x, int y, int xLength, int yLength)
    {
        return x >= 0 && x < xLength && y >= 0 && y < yLength;
    }
    /// <summary>
    ///  Check if coordinates are within grid bounds
    /// </summary>
    /// <param name="x">Horizontal Position</param>
    /// <param name="y">Vertical Position</param>
    /// <param name="z">Z Position</param>
    /// <param name="xLength">Width</param>
    /// <param name="yLength">Height</param>
    /// <param name="zLength">Height</param>
    public static bool IsInGrid(int x, int y, int z, int xLength, int yLength, int zLength)
    {
        return x >= 0 && x < xLength && y >= 0 && y < yLength && z >= 0 && z < zLength;
    }
    
    /// <summary>
    ///  Get vector distance between two tiles
    /// </summary>
    /// <param name="a">Tile 1</param>
    /// <param name="b">Tile 2</param>
    public static int TileDistance(CaveMask a, CaveMask b)
    {
        return (int)Mathf.Pow(a.x - b.x, 2) + (int)Mathf.Pow(a.z - b.z, 2);
    }
    
    /// <summary>
    ///  Returns a random active cave mask in the grid
    /// </summary>
    public static CaveMask? GetRandomMask()
    {
        int failSafe = 0;
        while (failSafe < 1000)
        {
            failSafe++;
            CaveMask check = CaveGenerator.levelMask[Random.Range(0, CaveGenerator.xWidth), Random.Range(0, CaveGenerator.zWidth)];
            if (check.active)
                return check;
        }
        return null;
    }
    
    /// <summary>
    ///  Returns a random empty cave cell in the grid
    /// </summary>
    public static CaveCell GetRandomEmptyCell()
    {
        int failSafe = 0;
        while (failSafe < 1000)
        {
            failSafe++;
            CaveMask? mask = GetRandomMask();
            if (!mask.HasValue) continue;
            int x = mask.Value.x;
            int z = mask.Value.z;
            if (CaveGenerator.levelGrid[x, 1, z].Tile == CaveCell.Tiles.Empty)
            {
                return CaveGenerator.levelGrid[x, 1, z];
            }
        }
        return null;
    }
}
