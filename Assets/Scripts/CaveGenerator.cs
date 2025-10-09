using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EditorAttributes;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Tween = PrimeTween.Tween;

public class CaveGenerator : MonoBehaviour
{
    public GenerationSettingsScriptableObject settings;

    [Header("Components")] [Header("Prefabs")] 
    [SerializeField] private GameObject floorCube;

    [Header("Seed")]
    [SerializeField] private int seed;
    [SerializeField] public bool randomSeed;



    public static Cell[,] levelGrid;
    public static int xLength;
    public static int yLength;
    public static int xLengthHalf;
    public static int yLengthHalf;

    private List<WalkerObject> walkers;
    private int tileCount;
    private List<Room> allRooms;
    
    private List<GameObject> floor;
    
    
    private void InitGrid(Cell[,] grid)
    {
        // Set seed
        if (randomSeed)
            seed = Random.Range(int.MinValue, int.MaxValue);
        
        Random.InitState(seed);
        
        // Set blank tiles
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                grid[i, j] = new Cell(i,j);
            }   
        }
        
        xLength = levelGrid.GetLength(0);
        yLength = levelGrid.GetLength(1);
        xLengthHalf = xLength / 2;
        yLengthHalf = yLength / 2;


    }

    private void CopyGrid(Cell[,] source, Cell[,] destination)
    {
        for (int column = 0; column < xLength; ++column)
        {
            for (int row = 0; row < yLength; ++row)
            {
                destination[column, row].Type = source[column, row].Type;
            }
        }
    }
    
    private bool IsInGrid(int x, int y)
    {
        return x >= 0 && x < xLength && y >= 0 && y < yLength;
    }
    
    private int TileDistance(Cell a, Cell b)
    {
        return (int)Mathf.Pow(a.x - b.x, 2) + (int)Mathf.Pow(a.y - b.y, 2);
    }
    

    [Button]
    public void FullGenerate()
    {
        
        // Clear anything left over
        ClearRoom();
        ResetRoom();
        // Initialise Room
        InitGrid(levelGrid);
        
        Vector3Int origin = new Vector3Int(xLengthHalf, yLengthHalf, 0);
        // Generate initial shape
        WalkerStart();
        AddCellularAutomaton(settings.cAIterations);
        // Repeat process twice to ensure no inaccessible walls are created when polishing
        CreateRooms();
        PolishRoom();
        CreateRooms();
        PolishRoom();
        
        if(allRooms.Count > 1)
            Debug.LogError("AHHHHHHHHHHHHHHHHHHH");
        
        // Spawn environment tiles
        GenerateEnvironment();
        
        // Set tiles into the tilemap
        SetTiles();
    }
    
    // Clear tilemaps and all game objects
    public void ClearRoom()
    {
        tileCount = 0;
        foreach (GameObject floorTile in floor)
        {
            if(Application.isPlaying)
                Destroy(floorTile);
            else
                DestroyImmediate(floorTile);
        }
        floor.Clear();
    }

    public void ResetRoom()
    {
        xLength = 0;
        yLength = 0;
        xLengthHalf = 0;
        yLengthHalf = 0;
        
        //environment = new();
        allRooms = new List<Room>();
        levelGrid = new Cell[settings.mapWidth, settings.mapHeight];
        
    }
    
    public void RegenerateRoom()
    {
        ClearRoom();
        ResetRoom();
        InitGrid(levelGrid);
        WalkerStart();
        SetTiles();
    }
    

    #region Walker Generation

    private void WalkerStart()
    {

        walkers = new List<WalkerObject>();
        
        Vector3Int TileCenter = new Vector3Int(xLengthHalf, yLengthHalf, 0);
        //Vector3Int TileCenter = Vector3Int.zero;
        
        WalkerObject curWalker = new WalkerObject(new Vector2(TileCenter.x, TileCenter.y), WalkerManager.GetRandomDirection() , settings.redirectChance, settings.removeChance, settings.createChance);
        levelGrid[TileCenter.x, TileCenter.y].Type = Cell.Types.Floor;
        walkers.Add(curWalker);

        tileCount++;

        WalkerGenerate();
        
    }
    
    private void WalkerGenerate()
    {
        while ((float)tileCount / (float)levelGrid.Length < settings.fillPercentage)
        {
            foreach (WalkerObject curWalker in walkers)
            {
                Vector3Int curPos = new Vector3Int((int)curWalker.Position.x, (int)curWalker.Position.y, 0);

                if (levelGrid[curPos.x, curPos.y].Type != Cell.Types.Floor)
                {
                    tileCount++;
                    levelGrid[curPos.x, curPos.y].Type = Cell.Types.Floor;
                }
            }

            //Walker Methods
            WalkerManager.ChanceToRemove(walkers);
            WalkerManager.ChanceToRedirect(walkers);
            WalkerManager.ChanceToCreate(walkers, settings.maximumWalkers, settings.redirectChance, settings.removeChance, settings.createChance);
            WalkerManager.UpdatePosition(walkers, xLength, yLength);
        }
    }
    
    #endregion
    
    #region Cellular Automaton
    private Cell[,] copyGrid;
public void AddCellularAutomaton(int iterations)
{
    // Pre-allocate the temporary grid if it doesn't exist or if grid size changed
    if (copyGrid == null || copyGrid.GetLength(0) != xLength || copyGrid.GetLength(1) != yLength)
    {
        copyGrid = new Cell[xLength, yLength];
        for (int x = 0; x < xLength; x++)
        {
            for (int y = 0; y < yLength; y++)
            {
                copyGrid[x, y] = new Cell(x, y);
            }
        }
    }

    for (int i = 0; i < iterations; i++)
    {
        // Copy current grid state into the copy grid
        CopyGrid(levelGrid, copyGrid);

        for (int j = 0; j < xLength; j++)
        {
            for (int k = 0; k < yLength; k++)
            {
                int wallCount = 0;

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        int neighborX = j + x;
                        int neighborY = k + y;

                        // If out of bounds, treat it as a wall
                        if (neighborX < 0 || neighborX >= xLength ||
                            neighborY < 0 || neighborY >= yLength)
                        {
                            wallCount++;
                        }
                        else
                        {
                            // Read from the snapshot (copyGrid) to avoid using modified data
                            // THIS PREVIOUSLY BROKE EVERYTHING
                            Cell.Types currentType = copyGrid[neighborX, neighborY].Type;
                            if (currentType == Cell.Types.Wall || currentType == Cell.Types.Null)
                            {
                                wallCount++;
                            }
                        }
                    }
                }

                // Apply automaton rules based on wall count
                levelGrid[j, k].Type = (wallCount > 4)
                    ? Cell.Types.Wall
                    : Cell.Types.Floor;
            }
        }
    }
}
    
    #endregion

    #region Room Creation
    
    public void CreateRooms()
    {
        List<Room> newRooms = new();
        int loop = 0;

        while (loop < 3)
        {
            loop++;
            
            newRooms = FindRooms();
            
            //print(allRooms.Count);

            // If there's only 1 room theres no need to connect
            if (newRooms.Count == 1) break;

            // Sort rooms based on size
            newRooms.Sort();

            // Set the biggest room as the main room
            newRooms[0].isMain = true;
            newRooms[0].isAcessible = true;

            // Connect all other rooms
            ConnectRooms(newRooms);
        }

        allRooms = newRooms;
    }

    private List<Room> FindRooms()
    {
        List<Room> currentRooms = new();
                // Grid that marks each floor tile that's been visited
        bool[,] tilesVisited = new bool[xLength, yLength];
        // Stores the currently checked tiles
        Queue<Cell> queue = new();
        
        // Loop through grid to find first clean tile
        for (int i = 0; i < xLength; i++)
        {
            for (int j = 0; j < yLength; j++)
            {
                // Ignore if not a floor
                if (levelGrid[i, j].Type != Cell.Types.Floor) continue;
                // Ignore if already in a room
                if (tilesVisited[i, j]) continue;

                // This must be a clean room
                queue.Enqueue(levelGrid[i,j]);

                List<Cell> room = new();

                while (queue.Count > 0)
                {
                    Cell current = queue.Dequeue();

                    // Loop all surrounding tiles
                    for (int x = current.x - 1; x <= current.x + 1; x++)
                    {
                        for (int y = current.y - 1; y <= current.y + 1; y++)
                        {
                            // Skip tile if
                            // If the tile is diagonal
                            if (x != current.x && y != current.y) continue;
                            // If the tile is not in the grid
                            if (!IsInGrid(x, y)) continue;
                            // If the tile has already been checked
                            if (tilesVisited[x, y]) continue;
                            // If the tile isn't a floor
                            if (levelGrid[x, y].Type != Cell.Types.Floor) continue;

                            tilesVisited[x, y] = true;
                            
                            room.Add(levelGrid[x,y]);
                            
                            queue.Enqueue(levelGrid[x,y]);
                    
                        
                        
                        }   
                    }
                }

                Room newRoom = new Room(room, levelGrid);

                // Only add room if it's big enough
                if (newRoom.roomSize > settings.roomThreshold)
                    currentRooms.Add(newRoom);
                else
                    newRoom.ClearRoom();

            }
        }

        return currentRooms;
    }

    private void ConnectRooms(List<Room> rooms, bool forceAccessibility = false)
    {
        // List of rooms NOT connected to main room
        List<Room> roomsA = new();
        // List of rooms that ARE connected to main room
        List<Room> roomsB = new();

        if (forceAccessibility)
        {
            foreach (Room room in rooms)
            {
                if(room.isAcessible)
                    roomsB.Add(room);
                else
                    roomsA.Add(room);
            }
        }
        else
        {
            roomsA = rooms;
            roomsB = rooms;
        }
        
        int lowest = 0;

        Cell bestTileA = new Cell();
        Cell bestTileB = new Cell();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();

        bool possibleConnection = false;
        
        
        // Check for all rooms
        foreach (Room a in roomsA)
        {
            // This ensures that during the second loop, it only connects the closest connection out of all possible rooms
            if (!forceAccessibility)
            {
                possibleConnection = false;      
                // Prevent checking an already connected room
                if (a.connectedRooms.Count > 0)
                    continue;
            }
            
            // Check all the rooms as every room
            foreach (Room b in roomsB)
            {
                // Prevent checking the same room or an already connected room
                if (a == b || a.IsConnected(b))
                    continue;

                // Loop through all the edge tiles in both rooms
                for (int indexA = 0; indexA < a.edgeTiles.Count; indexA++)
                {
                    for (int indexB = 0; indexB < b.edgeTiles.Count; indexB++)
                    {
                        // Check the distance between the edges
                        Cell tileA = a.edgeTiles[indexA];
                        Cell tileB = b.edgeTiles[indexB];
                        int distance = TileDistance(tileA, tileB);

                        // If the distance is the shortest so far, store tiles and rooms
                        if (distance < lowest || !possibleConnection)
                        {
                            lowest = distance;
                            possibleConnection = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = a;
                            bestRoomB = b;
                        }
                    }   
                }
            }

            // During the first loop connect all rooms to their closest
            if (possibleConnection && !forceAccessibility)
            {
                CreateConnection(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
            
        }
        
        // During the second loop, only connect the closest connection out of all possible rooms
        if (possibleConnection && forceAccessibility)
        {
            CreateConnection(bestRoomA, bestRoomB, bestTileA, bestTileB);
            // Repeat it until all rooms are connected to main
            ConnectRooms(rooms, true);
        }

        if (!forceAccessibility)
        {
            ConnectRooms(rooms, true);
        }
        
        // After all rooms are connected, apply one round of cellular automata to smoothen
        AddCellularAutomaton(1);
    }

    private void CreateConnection(Room a, Room b, Cell origin, Cell destination)
    {
        Room.ConnectRooms(a,b);
        
        //Debug.DrawLine(TileToPositionZ(origin)+Vector3.up*1.5f, TileToPositionZ(destination)+Vector3.up*1.5f, Color.green, 100);

        List<Cell> line = GetLine(origin, destination);

        foreach (Cell tile in line)
        {
            DrawCircle(tile, 1);
        }

    }

    void DrawCircle(Cell tile, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                 int drawX = tile.x + x;
                 int drawY = tile.y + y;
                 if (IsInGrid(drawX, drawY))
                     levelGrid[drawX, drawY].Type = Cell.Types.Floor;
            }   
        }
    }

    private List<Cell> GetLine(Cell from, Cell to)
    {
        List<Cell> line = new();

        // Bresenhams Line Generation Algorithm to calculate cells
        int x = from.x;
        int y = from.y;

        int dx = to.x - from.x;
        int dy = to.y - from.y;

        bool inverted = false;
        
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);
        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        // Swap values depending on if the line is going horizontal or vertical
        if (longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);
            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; i++)
        {
            line.Add(levelGrid[x, y]);

            if (inverted)
                y += step;
            else
                x += step;

            gradientAccumulation += shortest;

            if (gradientAccumulation >= longest)
            {
                if (inverted)
                    x += gradientStep;
                else
                    y += gradientStep;

                gradientAccumulation -= longest;
            }
        }

        return line;
    }
    #endregion

    // Add finishing touches to generated cave shape
public void PolishRoom()
{
    bool madeEdit = true;
    int failSafe = 0;

    while (madeEdit && failSafe < 10)
    {
        madeEdit = false;
        failSafe++;

        for (int i = 0; i < xLength; i++)
        {
            for (int j = 0; j < yLength; j++)
            {
                Cell current = levelGrid[i, j];
                Cell.Types currentType = current.Type;

                bool isOutside = true;
                int floorCount = 0;
                int wallCount = 0;

                // Loop surrounding 8 neighbors
                for (int x = i - 1; x <= i + 1; x++)
                {
                    for (int y = j - 1; y <= j + 1; y++)
                    {
                        // Skip current cell
                        if (x == i && y == j) continue;

                        // Inline IsInGrid check
                        bool inBounds = x >= 0 && x < xLength &&
                                        y >= 0 && y < yLength;

                        if (inBounds)
                        {
                            Cell.Types neighborType = levelGrid[x, y].Type;

                            if (currentType == Cell.Types.Floor && neighborType == Cell.Types.Null)
                            {
                                current.Type = Cell.Types.Wall;
                            }

                            if (currentType == Cell.Types.Null && neighborType == Cell.Types.Floor)
                            {
                                current.Type = Cell.Types.Wall;
                            }

                            if (neighborType == Cell.Types.Floor)
                            {
                                isOutside = false;
                                floorCount++;
                            }

                            if (neighborType == Cell.Types.Wall)
                            {
                                wallCount++;
                            }
                        }
                        else
                        {
                            if (currentType == Cell.Types.Floor)
                            {
                                current.Type = Cell.Types.Wall;
                            }
                        }
                    }
                }

                // Floor surrounded by walls
                if (wallCount >= 6 && current.Type == Cell.Types.Floor)
                {
                    current.Type = Cell.Types.Wall;
                    madeEdit = true;
                }

                // Wall surrounded by floor
                if (floorCount >= 6 && current.Type == Cell.Types.Wall)
                {
                    current.Type = Cell.Types.Floor;
                    madeEdit = true;
                }

                // Remove walls that are outside
                if (isOutside && current.Type != Cell.Types.Null)
                {
                    current.Type = Cell.Types.Null;
                    madeEdit = true;
                }

                // Check thin horizontal corridors (guarded bounds check)
                if (current.Type == Cell.Types.Floor && i > 0 && i < xLength - 1)
                {
                    var left = levelGrid[i - 1, j];
                    var right = levelGrid[i + 1, j];

                    if (left.Type == Cell.Types.Wall && right.Type == Cell.Types.Wall)
                    {
                        left.Type = Cell.Types.Floor;
                        right.Type = Cell.Types.Floor;
                        madeEdit = true;
                    }
                }

                // Check thin vertical corridors (guarded bounds check)
                if (current.Type == Cell.Types.Floor && j > 0 && j < yLength - 1)
                {
                    var top = levelGrid[i, j - 1];
                    var bottom = levelGrid[i, j + 1];

                    if (top.Type == Cell.Types.Wall && bottom.Type == Cell.Types.Wall)
                    {
                        top.Type = Cell.Types.Floor;
                        bottom.Type = Cell.Types.Floor;
                        madeEdit = true;
                    }
                }
            }
        }
    }
}

    #region Environment

    // Fill the floor tiles with random environment tiles
    private void GenerateEnvironment()
    {
        // Randomly place rocks etc
        
        // Create noise map based on size and scale
        float[,] noiseMap = new float[xLength, yLength];
        float noiseValue = 0.0f;
        // Generate random offsets using a seed
        float offsetX = Random.Range(0f, 10000f);
        float offsetY = Random.Range(0f, 10000f);

        for (int x = 0; x < xLength; x++)
        {
            for (int y = 0; y < yLength; y++)
            {
                float sampleX = (x * settings.environmentPerlinScale) + offsetX;
                float sampleY = (y * settings.environmentPerlinScale) + offsetY;

                noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
                noiseMap[x, y] = noiseValue;
            }
        }
        
        
        // Set all empty floor tiles based on noise map
        for (int i = 0; i < xLength; i++)
        {
            for (int j = 0; j < yLength; j++)
            {
                Cell current = levelGrid[i,j];
                
                if(!current.IsEmpty())
                    continue;
                

                noiseValue = noiseMap[i, j];
                if (noiseValue < settings.environmentPerlin)
                    current.Tile = Cell.Tiles.Environment;
            }   
        }
    }
    #endregion
    
    private void SetTiles()
    {
        for(int x = 0; x < xLength; x++)
        {
            for(int y = 0; y < yLength; y++)
            {
                Cell current = levelGrid[x,y];
                
                if (current.Type == Cell.Types.Floor)
                {
                    floor.Add(Instantiate(floorCube, current.WorldPosition, Quaternion.identity, transform));
                }
            }
        }
    }
    
        
}
[Serializable]
public class Cell
{
    public enum Types
    {
        Floor,
        Wall,
        Null
    }

    public enum Tiles
    {
        Empty,
        Environment,
    }

    public int x, y;
    

    public Types Type { get; set; }
    public Tiles Tile { get; set; }
    
    public Vector2Int AsVector2 => new Vector2Int(x, y);
    public Vector3 WorldPosition => new Vector3(x - CaveGenerator.xLengthHalf, 0, y - CaveGenerator.yLengthHalf);

    public Cell()
    {
        x = 0;
        y = 0;
    }

    public Cell(int x, int y)
    {
        this.x = x;
        this.y = y;
        Type = Types.Null;
        Tile = Tiles.Empty;
    }
    
    public Cell(Cell copy)
    {
        x = copy.x;
        y = copy.y;
        Type = copy.Type;
        Tile = copy.Tile;
    }

    //public bool isConnected;

    public bool IsEmpty()
    {
        return Type == Types.Floor && Tile == Tiles.Empty;
    }
    
    public void ClearTile()
    {
        Tile = Tiles.Empty;
    }

}
public class Room : IComparable<Room>
{
    public List<Cell> tiles;
    public List<Cell> edgeTiles;
    public List<Room> connectedRooms;
    public int roomSize;

    public bool isAcessible;
    public bool isMain;

    public Room(){}
    public Room(List<Cell> roomTiles, Cell[,] grid)
    {
        tiles = roomTiles;
        roomSize = tiles.Count;
        connectedRooms = new();

        edgeTiles = new();
        foreach (Cell tile in tiles)
        {
            bool skip = false;
            // Loop all surrounding tiles
            for (int x = tile.x - 1; x <= tile.x + 1; x++)
            {
                if (skip) break;
                for (int y = tile.y - 1; y <= tile.y + 1; y++)
                {
                    // Skip tile if
                    // If the tile is not in the grid
                    if (!(x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))) continue;
                    // If the tile is diagonal
                    if (!(x == tile.x || y == tile.y)) continue;
                    // If the tile isnt a wall
                    if (grid[x, y].Type != Cell.Types.Wall) continue;

                    edgeTiles.Add(tile);
                    skip = true;
                    break;

                }   
            }
        }
        
        //Debug.Log("Edge Count = " + edgeTiles.Count);

    }

    public void ClearRoom()
    {
        foreach (Cell tile in tiles)
        {
            tile.Type = Cell.Types.Null;
        }
    }

    public void SetAccessible()
    {
        if (!isAcessible)
        {
            isAcessible = true;
            foreach (Room connectedRoom in connectedRooms)
            {
                connectedRoom.SetAccessible();
            }
        }

    }

    public static void ConnectRooms(Room a, Room b)
    {
        if (a.isAcessible)
            b.SetAccessible();
        else if(b.isAcessible)
            a.SetAccessible();
        
        a.connectedRooms.Add(b);
        b.connectedRooms.Add(a);
    }

    public bool IsConnected(Room other)
    {
        return connectedRooms.Contains(other);
    }

    public int CompareTo(Room other)
    {
        return other.roomSize.CompareTo(this.roomSize);
    }

}

