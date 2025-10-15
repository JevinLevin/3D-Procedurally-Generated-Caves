using System;
using System.Collections;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

public class CaveGenerator : MonoBehaviour
{
    public GenerationSettingsScriptableObject settings;

    [Header("Components")] [Header("Prefabs")] 
    [SerializeField] private GameObject floorCube;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private List<TileScriptableObject> tiles;

    [Header("Seed")]
    [SerializeField] private int seed;
    [SerializeField] public bool randomSeed;

    public float stepTime = 0.0f;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.G))
            FullGenerate();
    }

    public CaveCell[,,] levelGrid;
    public CaveMask[,] levelMask;
    public static int xWidth;
    public static int yWidth;
    public static int zWidth;
    public static int xWidthHalf => xWidth / 2;
    public static int yWidthHalf => yWidth / 2;
    public static int zWidthHalf => zWidth / 2;

    private List<Walker> walkers;
    private int tileCount;
    private Room mainRoom;
    private List<Room> allRooms;
    
    [SerializeField] [HideInInspector] private List<GameObject> floor = new();
    
    /// <summary>
    /// Initialises the grid used for generation
    /// </summary>
    /// <param name="grid">Holds cave data</param>
    private void InitGrid(CaveCell[,,] grid)
    {
        // Set blank tiles
        for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
                for(int z = 0; z < grid.GetLength(2); z++)
                    grid[x, y, z] = new CaveCell(x,y,z);
        
        xWidth = levelGrid.GetLength(0);
        yWidth = levelGrid.GetLength(1);
        zWidth = levelGrid.GetLength(2);
    }
    /// <summary>
    /// Initialises the mask used for generation
    /// </summary>
    /// <param name="grid">Holds cave data</param>
    private void InitGrid(CaveMask[,] grid)
    {
        // Set blank tiles
        for (int i = 0; i < grid.GetLength(0); i++)
            for (int j = 0; j < grid.GetLength(1); j++)
                grid[i, j] = new CaveMask(i,j);
    }




    /// <summary>
    /// Completes the full process of generating the cave
    /// </summary>

    private Coroutine currentProcess;
    public IEnumerator FullGenerate()
    {
        // Set seed
        if (randomSeed)
            seed = Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(seed);
        
        // Clear anything left over
        ClearRoom();
        ResetRoom();
        // Initialise Room
        InitGrid(levelGrid);
        InitGrid(levelMask);
        
        // Generate initial shape
        WalkerStart();
        while(currentProcess != null)
            yield return null;
        
        AddCellularAutomaton(settings.cAIterations);
        
        // Repeat process twice to ensure no inaccessible walls are created when polishing
        // Testing found this combination produced the best results
        CreateRooms();
        PolishRoom();
        CreateRooms();
        PolishRoom();
        
        // Create the floor and ceiling of the cave
        CreateFloor();
        
        // Erect walls around the main room
        CreateWalls();
        
        // Spawn environment tiles
        GenerateEnvironment();
        
        // Instantiate all gameobjects
        SetTiles();

        yield return null;
    }
    
    /// <summary>
    /// Remove all game objects from the room
    /// </summary>
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

    /// <summary>
    /// CLear all data used in generation
    /// </summary>
    public void ResetRoom()
    {
        xWidth = 0;
        yWidth = 0;
        zWidth = 0;
        
        allRooms = new List<Room>();
        levelGrid = new CaveCell[settings.caveSize.x, settings.caveSize.y, settings.caveSize.z];
        levelMask = new CaveMask[settings.caveSize.x, settings.caveSize.z];
        
    }
    

    #region Walker Generation
    /// <summary>
    /// Start the random walker algorithm
    /// </summary>
    private void WalkerStart()
    {

        walkers = new List<Walker>();
        
        Vector3Int tileCenter = new Vector3Int(xWidthHalf, zWidthHalf, 0);
        
        Walker walker = new Walker(new Vector2(tileCenter.x, tileCenter.y), WalkerManager.GetRandomDirection() , settings.redirectChance, settings.removeChance, settings.createChance);
        levelMask[tileCenter.x, tileCenter.y].active = true;
        walkers.Add(walker);

        tileCount++;

        currentProcess = StartCoroutine(WalkerGenerate());
    }
    
    /// <summary>
    /// Runs the main walker generation loop
    /// </summary>
    private IEnumerator WalkerGenerate()
    {
        while ((float)tileCount / levelMask.Length < settings.fillPercentage)
        {
            foreach (Walker walker in walkers)
            {
                Vector2Int gridPos = walker.IntPosition;

                // Ignore already set floors
                if (levelMask[gridPos.x, gridPos.y].active) continue;
                
                tileCount++;
                levelMask[gridPos.x, gridPos.y].active = true;
            }

            //Walker Methods
            WalkerManager.ChanceToRemove(walkers);
            WalkerManager.ChanceToRedirect(walkers);
            WalkerManager.ChanceToCreate(walkers, settings.maximumWalkers, settings.redirectChance, settings.removeChance, settings.createChance);
            WalkerManager.UpdatePosition(walkers, xWidth, zWidth);
            
            if (stepTime > 0.0f)
            {
                CaveUtilities.CopyMask(levelMask, levelGrid, 0, xWidth, zWidth);
                ClearRoom();
                SetTiles();
                yield return new WaitForSeconds(stepTime);
            }
        }
    }
    
    #endregion
    
    #region Cellular Automaton
    private CaveMask[,] copyGrid;
    
    /// <summary>
    /// Applies the specified number of cellular automaton iterations to the grid
    /// </summary>
    /// <param name="iterations">Number of rounds of cellular automaton to run</param>
    private void AddCellularAutomaton(int iterations)
    {
        // Pre-allocate the temporary grid if it doesn't exist or if grid size changed
        if (copyGrid == null || copyGrid.GetLength(0) != xWidth || copyGrid.GetLength(1) != zWidth)
        {
            copyGrid = new CaveMask[xWidth, zWidth];
            for (int x = 0; x < xWidth; x++)
            {
                for (int y = 0; y < zWidth; y++)
                {
                    copyGrid[x, y] = new CaveMask(x, y);
                }
            }
        }

        for (int i = 0; i < iterations; i++)
        {
            // Copy current grid state into the copy grid
            // Uses copy grid as a snapshot to avoid using modified data during iteration
            CaveUtilities.CopyGrid(levelMask, copyGrid, xWidth, zWidth);

            for (int x = 0; x < xWidth; x++)
            {
                for (int y = 0; y < zWidth; y++)
                {
                    int offCount = 0;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            // Ignore the current cell
                            if (dx == 0 && dy == 0)
                                continue;

                            int neighborX = x + dx;
                            int neighborY = y + dy;

                            // If out of bounds, treat it as off
                            if (neighborX < 0 || neighborX >= xWidth ||
                                neighborY < 0 || neighborY >= zWidth)
                            {
                                offCount++;
                            }
                            else
                            {
                                // Read from the snapshot (copyGrid) to avoid using modified data
                                if (!copyGrid[neighborX, neighborY].active)
                                {
                                    offCount++;
                                }
                            }
                        }
                    }

                    // Apply automaton rules based on off count
                    levelMask[x, y].active = offCount <= 4;
                }
            }
        }
    }
    
    #endregion

    #region Room Creation
    /// <summary>
    /// Finds all rooms in the grid and connects them if necessary
    /// https://www.youtube.com/watch?v=eVb9kQXvEZM&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9&index=6
    /// Thanks to Sebastian Lague for the tutorial
    /// </summary>
    public void CreateRooms()
    {
        List<Room> newRooms = new();
        int loop = 0;

        while (loop < 3)
        {
            loop++;
            
            newRooms = FindRooms();
            
            // If there's only 1 room there's no need to connect
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
        mainRoom = allRooms[0];
    }

    /// <summary>
    /// Creates list of all grounds of connected floor tiles
    /// </summary>
    private List<Room> FindRooms()
    {
        List<Room> currentRooms = new();
        // Grid that marks each floor tile that's been visited
        bool[,] tilesVisited = new bool[xWidth, zWidth];
        // Stores the currently checked tiles
        Queue<CaveMask> queue = new();
        
        // Loop through grid to find first clean tile
        for (int i = 0; i < xWidth; i++)
        {
            for (int j = 0; j < zWidth; j++)
            {
                // Ignore if off
                if (!levelMask[i, j].active) continue;
                // Ignore if already in a room
                if (tilesVisited[i, j]) continue;

                // This must be a clean room
                queue.Enqueue(levelMask[i,j]);

                List<CaveMask> room = new();

                while (queue.Count > 0)
                {
                    CaveMask current = queue.Dequeue();

                    // Loop all surrounding tiles
                    for (int x = current.x - 1; x <= current.x + 1; x++)
                    {
                        for (int y = current.z - 1; y <= current.z + 1; y++)
                        {
                            // Skip tile if
                            // If the tile is diagonal
                            if (x != current.x && y != current.z) continue;
                            // If the tile is not in the grid
                            if (!CaveUtilities.IsInGrid(x, y, xWidth, zWidth)) continue;
                            // If the tile has already been checked
                            if (tilesVisited[x, y]) continue;
                            // If the tile isn't a floor
                            if (!levelMask[x, y].active) continue;

                            tilesVisited[x, y] = true;
                            
                            room.Add(levelMask[x,y]);
                            
                            queue.Enqueue(levelMask[x,y]);
                        }   
                    }
                }

                Room newRoom = new Room(room, levelMask);

                // Only add room if it's big enough
                if (newRoom.roomSize > settings.roomThreshold)
                    currentRooms.Add(newRoom);
                else
                    newRoom.ClearRoom();
            }
        }
        return currentRooms;
    }

    
    /// <summary>
    /// Connect all rooms together, first to the biggest room, then to the closest room
    /// </summary>
    /// <param name="rooms">List of all available rooms</param>
    /// <param name="forceAccessibility">Used during repeated loops to force connection to the closest room </param>
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

        CaveMask bestTileA = new CaveMask();
        CaveMask bestTileB = new CaveMask();
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
                foreach (var tileA in a.edgeTiles)
                {
                    foreach (var tileB in b.edgeTiles)
                    {
                        int distance = CaveUtilities.TileDistance(tileA, tileB);

                        // If the distance is the shortest so far, store tiles and rooms
                        if (distance >= lowest && possibleConnection) continue;
                        
                        lowest = distance;
                        possibleConnection = true;
                        bestTileA = tileA;
                        bestTileB = tileB;
                        bestRoomA = a;
                        bestRoomB = b;
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
        
        // After all rooms are connected, apply one round of cellular automata to smoothen connections
        AddCellularAutomaton(1);
    }

    /// <summary>
    /// Draws a tunnel between two rooms
    /// </summary>
    /// <param name="a">Origin room</param>
    /// <param name="b">Destination room</param>
    /// <param name="origin">Where to start drawing the tunnel</param>>
    /// <param name="destination">Where to finish drawing the tunnel</param>>

    private void CreateConnection(Room a, Room b, CaveMask origin, CaveMask destination)
    {
        Room.ConnectRooms(a,b);
        
        //Debug.DrawLine(TileToPositionZ(origin)+Vector3.up*1.5f, TileToPositionZ(destination)+Vector3.up*1.5f, Color.green, 100);

        List<CaveMask> line = GetLine(origin, destination);

        foreach (CaveMask tile in line)
        {
            DrawCircle(tile, 1);
        }

    }

    /// <summary>
    /// Draw a circle of tiles of a given radius around a given tile 
    /// </summary>
    /// <param name="tile">Circle origin</param>
    /// <param name="radius">Circle size </param>
    void DrawCircle(CaveMask tile, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int drawX = tile.x + x;
                int drawY = tile.z + y;
                if (CaveUtilities.IsInGrid(drawX, drawY, xWidth, zWidth))
                    levelMask[drawX, drawY].active = true;
            }   
        }
    }

    /// <summary>
    /// Generate list of cells between two points using Bresenhams Line Algorithm
    /// </summary>
    /// <param name="from">Origin cell</param>
    /// <param name="to">Destination cell</param>
    private List<CaveMask> GetLine(CaveMask from, CaveMask to)
    {
        List<CaveMask> line = new();

        // Bresenhams Line Generation Algorithm to calculate cells
        int x = from.x;
        int y = from.z;

        int dx = to.x - from.x;
        int dy = to.z - from.z;

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
            line.Add(levelMask[x, y]);

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

    #region Height

    /// <summary>
    /// Creates the floor and ceiling of the cave using a distance field and perlin noise
    /// </summary>
    private void CreateFloor()
    {
        // Copy mask to bottom and top of grid
        CaveUtilities.CopyMask(levelMask, levelGrid, 0, xWidth, zWidth);
        CaveUtilities.CopyMask(levelMask, levelGrid, settings.caveSize.y-1, xWidth, zWidth);
        
        // Use distance field to determine height of each tile
        int[,] invertedDistanceField = CalculateInvertedDistanceField(settings.floorHeight);
        
        AddHeight(0, 1, invertedDistanceField, settings.floorPerlinScale, settings.floorPerlinAmplitude);
        AddHeight(settings.caveSize.y-1, -1, invertedDistanceField, settings.ceilingPerlinScale, settings.ceilingPerlinAmplitude);
    }

    /// <summary>
    /// Adjusts the height of a layer in the grid based on a distance field and perlin noise
    /// </summary>
    /// <param name="height">Layer height</param>
    /// <param name="direction">Normalised direction</param>
    /// <param name="invertedDistanceField">Grid of each tile's inverted distance from wall</param>
    /// <param name="perlinScale">Scale of perlin noise calculations</param>
    /// <param name="perlinAmplitude">Amplitude of perlin noise calculations</param>


    private void AddHeight(int height, int direction, int[,] invertedDistanceField, float perlinScale, float perlinAmplitude)
    {
        
        for (int x = 0; x < xWidth; x++)
        {
            for (int z = 0; z < zWidth; z++)
            {
                CaveMask current = levelMask[x,z];
                
                if(!current.active)
                    continue;

                int distance = invertedDistanceField[x, z];
                
                int noiseOffset = GenerateNoiseOffset(x, z, perlinScale, perlinAmplitude);
                
                int newHeight =  height + distance * direction + noiseOffset;
                newHeight = Mathf.Clamp(newHeight, 0, settings.caveSize.y - 1);

                int start = height;
                int end = newHeight;
                
                // From start to end in the correct direction
                for (int h = start; h != end; h += direction)
                {
                    levelGrid[x, h, z].Tile = CaveCell.Tiles.Tile;
                }
            }   
        }
    }
    
    /// <summary>
    /// Return vertical offset based on perlin noise
    /// </summary>
    /// <param name="x">Horizontal position</param>
    /// <param name="z">Vertical position</param>
    /// <param name="perlinScale">Smoothness of offsets</param>
    /// <param name="perlinAmplitude">Maximum height of offset</param>

    private int GenerateNoiseOffset(int x, int z, float perlinScale, float perlinAmplitude)
    {
        // Offset perlin to get different results each time
        float offsetX = (seed % 10000) * 0.1f;
        float offsetZ = (seed % 5000) * 0.1f;
        
        // Generate random value with scale
        // Large scale = smooth changes
        // Smaller scale = jagged changes
        float noiseValue = Mathf.PerlinNoise(
            x * perlinScale + offsetX,
            z * perlinScale + offsetZ * 0.5f
        );
        
        
        return Mathf.RoundToInt(
                                // Shift range from [0,1] to [-0.5,0.5]
                                (noiseValue - 0.5f) 
                                // Scale range to [-1,1]
                                * 2f 
                                // Scale by additional amplitude
                                * perlinAmplitude);
    }
    
    /// <summary>
    /// Calculate distance of each tile from edge
    /// </summary>
    /// <param name="clamp">Limit distance to amount</param>
    private int[,] CalculateDistanceField(int clamp)
    {
        int[,] distance = new int[xWidth, zWidth];
        Queue<Vector2Int> queue = new();
        
        // Edge tiles get the minimum distance of 1
        foreach(CaveMask mask in mainRoom.edgeTiles)
        {
            distance[mask.x, mask.z] = 1;
            queue.Enqueue(new Vector2Int(mask.x, mask.z));
        }
        
        // Flood fill to set distance from edge for each tile
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int currentDistance = distance[current.x, current.y];
            Vector2Int[] searchOrder = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

            // Loop through all cardinal neighbours
            foreach (Vector2Int dir in searchOrder)
            {
                Vector2Int neighbor = current + dir;

                // Make sure it's in the mask
                if (!CaveUtilities.IsInGrid(neighbor.x, neighbor.y, xWidth, zWidth)) continue;
                if (!levelMask[neighbor.x, neighbor.y].active) continue;

                // If the neighbour is unset or distance is shorter, update it
                if (distance[neighbor.x, neighbor.y] == 0 || distance[neighbor.x, neighbor.y] > currentDistance + 1)
                {
                    distance[neighbor.x, neighbor.y] = Mathf.Clamp(currentDistance + 1,0,clamp);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return distance;

    }
    /// <summary>
    /// Calculate distance of each tile from edge, but invert the values so the furthest tile has the highest value
    /// </summary>
    /// <param name="clamp">Limit distance to amount</param>
    private int[,] CalculateInvertedDistanceField(int clamp)
    {
        int[,] distanceField = CalculateDistanceField(clamp);
        int maxDistance = 0;

        for (int x = 0; x < xWidth; x++)
        {
            for (int z = 0; z < zWidth; z++)
            {
                if (distanceField[x, z] > maxDistance)
                    maxDistance = distanceField[x, z];
            }
        }

        for (int x = 0; x < xWidth; x++)
        {
            for (int z = 0; z < zWidth; z++)
            {
                if (distanceField[x, z] > 0)
                    distanceField[x, z] = (maxDistance + 1) - distanceField[x, z];
            }
        }
        return distanceField;
    }

    #endregion

    #region Walls

    /// <summary>
    /// Extrude tiles around edge tiles up to ceiling
    /// </summary>
    private void CreateWalls()
    {
        HashSet<Vector2Int> wallMask = new();
        // Loop through edge tiles and add outer tiles to hashset
        foreach (CaveMask mask in mainRoom.edgeTiles)
        {
            Vector2Int[] searchOrder = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

            // Loop through all cardinal neighbours
            foreach (Vector2Int dir in searchOrder)
            {
                CaveMask neighbour = levelMask[mask.x + dir.x, mask.z + dir.y];
                if(neighbour.active)
                    continue;
                
                wallMask.Add(neighbour.IntPosition);
            }
        }
        
        // Set wall tiles in grid
        foreach (Vector2Int wall in wallMask)
        {
            for (int y = 0; y < settings.caveSize.y; y++)
            {
                levelGrid[wall.x,y,wall.y].Tile = CaveCell.Tiles.Tile;
            }
        }
    }

    #endregion

    # region Polish
    /// <summary>
    /// Custom algorithm that applies finishing touches to the room
    /// </summary>
    public void PolishRoom()
    {
        bool madeEdit = true;
        int failSafe = 0;

        while (madeEdit && failSafe < 10)
        {
            madeEdit = false;
            failSafe++;

            for (int i = 0; i < xWidth; i++)
            {
                for (int j = 0; j < zWidth; j++)
                {
                    CaveMask current = levelMask[i, j];
                    bool currentActive = current.active;

                    int onCount = 0;
                    int offCount = 0;

                    // Loop surrounding 8 neighbors
                    for (int x = i - 1; x <= i + 1; x++)
                    {
                        for (int y = j - 1; y <= j + 1; y++)
                        {
                            // Skip current cell
                            if (x == i && y == j) continue;

                            // Inline IsInGrid check
                            bool inBounds = x >= 0 && x < xWidth &&
                                            y >= 0 && y < zWidth;

                            if (inBounds)
                            {
                                if (levelMask[x, y].active)
                                {
                                    onCount++;
                                }
                                
                                if (!levelMask[x, y].active)
                                {
                                    offCount++;
                                }
                            }
                            else
                            {
                                if (currentActive)
                                {
                                    current.active = false;
                                }
                            }
                        }
                    }

                    // Floor surrounded by walls
                    if (offCount >= 6 && current.active)
                    {
                        current.active = false;
                        madeEdit = true;
                    }

                    // Wall surrounded by floor
                    if (onCount >= 6 && !current.active)
                    {
                        current.active = true;
                        madeEdit = true;
                    }

                    // Check thin horizontal corridors (guarded bounds check)
                    if (current.active && i > 0 && i < xWidth - 1)
                    {
                        var left = levelMask[i - 1, j];
                        var right = levelMask[i + 1, j];

                        if (!left.active && !right.active)
                        {
                            left.active = true;
                            right.active = true;
                            madeEdit = true;
                        }
                    }

                    // Check thin vertical corridors (guarded bounds check)
                    if (current.active && j > 0 && j < zWidth - 1)
                    {
                        var top = levelMask[i, j - 1];
                        var bottom = levelMask[i, j + 1];

                        if (!top.active && !bottom.active)
                        {
                            top.active = true;
                            bottom.active = true;
                            madeEdit = true;
                        }
                    }
                }
            }
        }
    }
    #endregion

    #region Environment
    
    /// <summary>
    /// Generates environment tiles such as rocks based on a perlin noise map
    /// </summary>
    private void GenerateEnvironment()
    {
        // Create noise map based on size and scale
        float[,] noiseMap = new float[xWidth, zWidth];
        float noiseValue = 0.0f;
        // Generate random offsets using a seed
        float offsetX = Random.Range(0f, 10000f);
        float offsetY = Random.Range(0f, 10000f);

        for (int x = 0; x < xWidth; x++)
        {
            for (int y = 0; y < zWidth; y++)
            {
                float sampleX = (x * settings.environmentPerlinScale) + offsetX;
                float sampleY = (y * settings.environmentPerlinScale) + offsetY;

                noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
                noiseMap[x, y] = noiseValue;
            }
        }
        
        
        // Set all empty floor tiles based on noise map
        for (int i = 0; i < xWidth; i++)
        {
            for (int j = 0; j < zWidth; j++)
            {
                CaveMask current = levelMask[i,j];
                
                if(!current.active)
                    continue;
                

                noiseValue = noiseMap[i, j];
                if (noiseValue < settings.environmentPerlin)
                {
                    // Get lowest tile on x and z axis
                    for (int h = 0; h < settings.caveSize.y; h++)
                    {
                        if (levelGrid[i, h, j].Tile == CaveCell.Tiles.Tile)
                            continue;
                        
                        levelGrid[i, h, j].Tile = CaveCell.Tiles.Environment;
                        break;
                    }
                }
            }   
        }
    }
    #endregion
    
    /// <summary>
    /// Instantiate all game objects based on the grid data
    /// </summary>
    private void SetTiles()
    {
        for(int x = 0; x < xWidth; x++)
        {
            for (int y = 0; y < yWidth; y++)
            {
                for(int z = 0; z < zWidth; z++)
                {
                    CaveCell current = levelGrid[x,y,z];
                
                    if (current.Tile == CaveCell.Tiles.Tile)
                    {
                        floor.Add(Instantiate(floorCube, current.WorldPosition, Quaternion.identity, transform));
                    }
                    else if (current.Tile == CaveCell.Tiles.Environment)
                    {
                        TileScriptableObject tile = RandomWeightedTile();
                        GameObject newTile = Instantiate(tilePrefab, current.WorldPosition, Quaternion.identity, transform);
                        newTile.GetComponent<CaveTile>().Setup(tile);
                        floor.Add(newTile);
                        
                    }
                }
            }
        }
    }

    private TileScriptableObject RandomWeightedTile()
    {
        int totalWeight = 0;
        foreach (TileScriptableObject tile in tiles)
        {
            totalWeight += tile.weight;
        }
        int randomValue = Random.Range(0, totalWeight);
        foreach (TileScriptableObject tile in tiles)
        {
            if (randomValue < tile.weight)
            {
                return tile;
            }
            randomValue -= tile.weight;
        }
        // Failsafe
        return tiles[0];
    }
    
        
}
[Serializable]
public class CaveCell
{
    public enum Tiles
    {
        Empty,
        Tile,
        Environment,
    }

    public int x, y, z;
    

    public Tiles Tile { get; set; }
    
    public Vector3 WorldPosition => new Vector3(x - CaveGenerator.xWidthHalf, y, z - CaveGenerator.zWidthHalf);

    public CaveCell()
    {
        x = 0;
        y = 0;
        z = 0;
    }

    public CaveCell(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        Tile = Tiles.Empty;
    }
    
    public CaveCell(CaveCell copy)
    {
        x = copy.x;
        y = copy.y;
        z = copy.z;
        Tile = copy.Tile;
    }

    /// <summary>
    /// Clear any environment tile on this cell
    /// </summary>
    public void ClearTile()
    {
        Tile = Tiles.Empty;
    }

}
[Serializable]
public struct CaveMask
{
    public CaveMask(int x, int z)
    {
        this.x = x;
        this.z = z;
        active = false;
    }
    
    public CaveMask(CaveMask copy)
    {
        x = copy.x;
        z = copy.z;
        active = copy.active;
    }
    
    public bool active;

    public int x,z;
    public Vector2Int IntPosition => new Vector2Int(x, z);

    public void Toggle() => active = !active;
    public void Disable() => active = false;
    
}
public class Room : IComparable<Room>
{
    public List<CaveMask> tiles;
    public List<CaveMask> edgeTiles;
    public List<Room> connectedRooms;
    public int roomSize;

    public bool isAcessible;
    public bool isMain;

    public Room(){}
    public Room(List<CaveMask> roomTiles, CaveMask[,] grid)
    {
        tiles = roomTiles;
        roomSize = tiles.Count;
        connectedRooms = new();

        // Store edge tiles of room when created
        edgeTiles = new();
        foreach (CaveMask tile in tiles)
        {
            bool skip = false;
            // Loop all surrounding tiles
            for (int x = tile.x - 1; x <= tile.x + 1; x++)
            {
                if (skip) break;
                for (int y = tile.z - 1; y <= tile.z + 1; y++)
                {
                    // Skip tile if
                    // If the tile is not in the grid
                    if (!(x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))) continue;
                    // If the tile is diagonal
                    if (!(x == tile.x || y == tile.z)) continue;
                    // If the tile is acticve
                    if (grid[x, y].active) continue;

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
        foreach (CaveMask tile in tiles)
        {
            tile.Disable();
        }
    }

    private void SetAccessible()
    {
        if (isAcessible) return;
        
        isAcessible = true;
        foreach (Room connectedRoom in connectedRooms)
        {
            connectedRoom.SetAccessible();
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
        return other.roomSize.CompareTo(roomSize);
    }

}

[Serializable]
public class EditorFunctions
{
    public CaveGenerator caveGenerator;
    public float stepTime;
    [Button]
    private void GenerateCave()
    {
        caveGenerator.stepTime = stepTime;
        caveGenerator.StartCoroutine(nameof(caveGenerator.FullGenerate));
    }
}

