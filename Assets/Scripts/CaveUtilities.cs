using UnityEngine;

public static class CaveUtilities
{

    public static void CopyGrid(Cell[,] source, Cell[,] destination, int xLength, int yLength)
    {
        for (int column = 0; column < xLength; ++column)
        {
            for (int row = 0; row < yLength; ++row)
            {
                destination[column, row].Type = source[column, row].Type;
            }
        }
    }
    
    public static bool IsInGrid(int x, int y, int xLength, int yLength)
    {
        return x >= 0 && x < xLength && y >= 0 && y < yLength;
    }
    
    public static int TileDistance(Cell a, Cell b)
    {
        return (int)Mathf.Pow(a.x - b.x, 2) + (int)Mathf.Pow(a.y - b.y, 2);
    }
}
