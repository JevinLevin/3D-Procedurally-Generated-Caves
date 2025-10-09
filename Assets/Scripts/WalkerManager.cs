using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkerManager
{
    public static Vector2 GetRandomDirection()
    {
        int choice = Mathf.FloorToInt(UnityEngine.Random.value * 3.99f);

        switch (choice)
        {
            case 0:
                return Vector2.down;
            case 1:
                return Vector2.left;
            case 2:
                return Vector2.up;
            case 3:
                return Vector2.right;
            default:
                return Vector2.zero;
        }
    }
    
    public static void ChanceToRemove(List<WalkerObject> walkers)
    {
        int updatedCount = walkers.Count;
        for (int i = 0; i < updatedCount; i++)
        {
            if (UnityEngine.Random.value < walkers[i].removeChance && walkers.Count > 1)
            {
                walkers.RemoveAt(i);
                break;
            }
        }
    }

    public static void ChanceToRedirect(List<WalkerObject> walkers)
    {
        for (int i = 0; i < walkers.Count; i++)
        {
            if (UnityEngine.Random.value < walkers[i].redirectChance)
            {
                WalkerObject curWalker = walkers[i];
                curWalker.Direction = GetRandomDirection();
                walkers[i] = curWalker;
            }
        }
    }

    public static void ChanceToCreate(List<WalkerObject> walkers, int maximumWalkers, float redirect, float remove, float create)
    {
        int updatedCount = walkers.Count;
        for (int i = 0; i < updatedCount; i++)
        {
            if (UnityEngine.Random.value < walkers[i].createChance && walkers.Count < maximumWalkers)
            {
                Vector2 newDirection = GetRandomDirection();
                Vector2 newPosition = walkers[i].Position;

                WalkerObject newWalker = new WalkerObject(newPosition, newDirection, redirect, remove, create);
                walkers.Add(newWalker);
            }
        }
    }

    public static void UpdatePosition(List<WalkerObject> walkers, int xLength, int yLength)
    {
        for (int i = 0; i < walkers.Count; i++)
        {
            WalkerObject FoundWalker = walkers[i];
            FoundWalker.Position += FoundWalker.Direction;
            FoundWalker.Position.x = Mathf.Clamp(FoundWalker.Position.x, 1, xLength - 2);
            FoundWalker.Position.y = Mathf.Clamp(FoundWalker.Position.y, 1, yLength - 2);
            walkers[i] = FoundWalker;
        }
    }
}

public class WalkerObject
{
    public Vector2 Position;
    public Vector2 Direction;
    public float redirectChance;
    public float removeChance;
    public float createChance;

    public WalkerObject(Vector2 pos, Vector2 dir, float redirectChance, float removeChance, float createChance){
        Position = pos;
        Direction = dir;
        this.redirectChance = redirectChance;
        this.removeChance = removeChance;
        this.createChance = createChance;
    }
}