using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class WalkerManager
{
    /// <summary>
    /// Generate a random direction for the walker
    /// </summary>
    /// <returns>
    /// A random cardinal direction (up, right, down, left)
    /// </returns>
    public static Vector2 GetRandomDirection()
    {
        int dir = Random.Range(0, 4);
        switch (dir)
        {
            case 0:
                return Vector2.up;
            case 1:
                return Vector2.right;
            case 2:
                return Vector2.down;
            case 3:
                return Vector2.left;
            default:
                return Vector2.up;
        }
    }
    
    /// <summary>
    /// Delete a walker based on its remove chance
    /// </summary>
    public static void ChanceToRemove(List<Walker> walkers)
    {
        int updatedCount = walkers.Count;
        for (int i = 0; i < updatedCount; i++)
        {
            if (!(Random.value < walkers[i].removeChance) || walkers.Count <= 1) continue;
            
            walkers.RemoveAt(i);
            break;
        }
    }

    /// <summary>
    /// Redirect walker based on its redirect chance
    /// </summary>
    public static void ChanceToRedirect(List<Walker> walkers)
    {
        for (int i = 0; i < walkers.Count; i++)
        {
            if (!(Random.value < walkers[i].redirectChance)) continue;
            
            // Change direction
            walkers[i].direction = GetRandomDirection();
        }
    }

    /// <summary>
    /// Create new walker based on its create chance
    /// </summary>
    public static void ChanceToCreate(List<Walker> walkers, int maximumWalkers, float redirect, float remove, float create)
    {
        int updatedCount = walkers.Count;
        for (int i = 0; i < updatedCount; i++)
        {
            if (!(Random.value < walkers[i].createChance) || walkers.Count >= maximumWalkers) continue;
            
            Walker newWalker = new Walker(
                walkers[i].position, 
                GetRandomDirection(), 
                redirect, remove, create);
            
            walkers.Add(newWalker);
        }
    }

    /// <summary>
    /// Update a walkers position in the grid
    /// </summary>
    /// <param name="walkers">List of all active walkers</param>
    /// <param name="xLength">Grid width</param>
    /// <param name="yLength">Grid height</param>
    public static void UpdatePosition(List<Walker> walkers, int xLength, int yLength)
    {
        foreach (Walker walker in walkers)
        {
            // Move
            walker.position += walker.direction;
            // Limit to grid
            walker.position.x = Mathf.Clamp(walker.position.x, 1, xLength - 2);
            walker.position.y = Mathf.Clamp(walker.position.y, 1, yLength - 2);
        }
    }
}

public class Walker
{
    public Vector2 position;
    public Vector2 direction;
    public readonly float redirectChance;
    public readonly float removeChance;
    public readonly float createChance;
    
    public Vector2Int IntPosition => new((int)position.x, (int)position.y);

    public Walker(Vector2 pos, Vector2 dir, float redirectChance, float removeChance, float createChance){
        position = pos;
        direction = dir;
        this.redirectChance = redirectChance;
        this.removeChance = removeChance;
        this.createChance = createChance;
    }
}