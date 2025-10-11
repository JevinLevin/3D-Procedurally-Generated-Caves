using UnityEngine;

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
    ///  Get vector distance between two tiles
    /// </summary>
    /// <param name="a">Tile 1</param>
    /// <param name="b">Tile 2</param>
    public static int TileDistance(CaveMask a, CaveMask b)
    {
        return (int)Mathf.Pow(a.x - b.x, 2) + (int)Mathf.Pow(a.z - b.z, 2);
    }
}
