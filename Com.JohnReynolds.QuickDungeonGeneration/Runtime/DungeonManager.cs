using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Serialization;

public class DungeonManager : MonoBehaviour
{
    private const string DllName = "NativeLevelGen";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct DungeonConfigInterop
    {
        public int mapWidth, mapHeight, roomWidth, roomHeight;
        public int roomSteps, roomBrushSize, wallHeightMinimum, wallWidthMinimum;
        public int roomTopPadding, roomBottomPadding, roomLeftPadding, roomRightPadding;
        public int roomTileBase, roomSeedIncrement, poissonMinimumDistance;
        public int poissonPoints, poissonMaximumAttempts, mstExtraEdges, corridorWidth;
        public byte mapWallValue, mapFloorValue;
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void GenerateDungeonNativeWithRooms(
        ref DungeonConfigInterop config,
        int seed,
        IntPtr outMapData,
        IntPtr outRoomData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int GeneratePoissonPointsNative(
        int width,
        int height,
        int minimumDistance,
        int pointCount,
        int maximumAttempts,
        int seed,
        IntPtr outPoints);

    [FormerlySerializedAs("tilemap")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private TileBase wallTile;
    [SerializeField] private TileBase floorTile;
    [SerializeField] private DungeonPropSpawner propSpawner;
    [SerializeField] private DungeonGenerationConfig config;

    public DungeonGenerationConfig Config
    {
        get { return config; }
        set { config = value; }
    }

    public Tilemap Tilemap
    {
        get { return floorTilemap; }
        set { floorTilemap = value; }
    }

    public Tilemap FloorTilemap
    {
        get { return floorTilemap; }
        set { floorTilemap = value; }
    }

    public Tilemap WallTilemap
    {
        get { return wallTilemap; }
        set { wallTilemap = value; }
    }

    public TileBase WallTile
    {
        get { return wallTile; }
        set { wallTile = value; }
    }

    public TileBase FloorTile
    {
        get { return floorTile; }
        set { floorTile = value; }
    }

    public DungeonPropSpawner PropSpawner
    {
        get { return propSpawner; }
        set { propSpawner = value; }
    }

    public void Generate(int seed)
    {
        Generate(GetConfigOrDefault(), seed);
    }

    // Kept for callers that supplied dimensions before the inspector configuration existed.
    public void Generate(int width, int height, int seed)
    {
        DungeonGenerationConfig sourceConfig = GetConfigOrDefault();
        DungeonGenerationConfig requestedConfig = CreateConfigInstance();
        requestedConfig.mapWidth = width;
        requestedConfig.mapHeight = height;
        requestedConfig.mapWallValue = sourceConfig.mapWallValue;
        requestedConfig.mapFloorValue = sourceConfig.mapFloorValue;
        requestedConfig.roomWidth = sourceConfig.roomWidth;
        requestedConfig.roomHeight = sourceConfig.roomHeight;
        requestedConfig.roomSteps = sourceConfig.roomSteps;
        requestedConfig.roomBrushSize = sourceConfig.roomBrushSize;
        requestedConfig.wallHeightMinimum = sourceConfig.wallHeightMinimum;
        requestedConfig.wallWidthMinimum = sourceConfig.wallWidthMinimum;
        requestedConfig.roomTopPadding = sourceConfig.roomTopPadding;
        requestedConfig.roomBottomPadding = sourceConfig.roomBottomPadding;
        requestedConfig.roomLeftPadding = sourceConfig.roomLeftPadding;
        requestedConfig.roomRightPadding = sourceConfig.roomRightPadding;
        requestedConfig.roomTileBase = sourceConfig.roomTileBase;
        requestedConfig.roomSeedIncrement = sourceConfig.roomSeedIncrement;
        requestedConfig.poissonMinimumDistance = sourceConfig.poissonMinimumDistance;
        requestedConfig.poissonPoints = sourceConfig.poissonPoints;
        requestedConfig.poissonMaximumAttempts = sourceConfig.poissonMaximumAttempts;
        requestedConfig.mstExtraEdges = sourceConfig.mstExtraEdges;
        requestedConfig.corridorWidth = sourceConfig.corridorWidth;
        Generate(requestedConfig, seed);
    }

    private DungeonGenerationConfig GetConfigOrDefault()
    {
        return config != null ? config : CreateConfigInstance();
    }

    private static DungeonGenerationConfig CreateConfigInstance()
    {
        return ScriptableObject.CreateInstance<DungeonGenerationConfig>();
    }

    private void Generate(DungeonGenerationConfig generationConfig, int seed)
    {
        ValidateConfig(generationConfig);
        int width = generationConfig.mapWidth;
        int height = generationConfig.mapHeight;
        int totalCells = width * height;
        byte[] mapBuffer = new byte[totalCells];
        int[] roomBuffer = new int[totalCells];

        GCHandle mapHandle = GCHandle.Alloc(mapBuffer, GCHandleType.Pinned);
        GCHandle roomHandle = GCHandle.Alloc(roomBuffer, GCHandleType.Pinned);
        try
        {
            var nativeConfig = new DungeonConfigInterop
            {
                mapWidth = width,
                mapHeight = height,
                roomWidth = generationConfig.roomWidth,
                roomHeight = generationConfig.roomHeight,
                roomSteps = generationConfig.roomSteps,
                roomBrushSize = generationConfig.roomBrushSize,
                wallHeightMinimum = generationConfig.wallHeightMinimum,
                wallWidthMinimum = generationConfig.wallWidthMinimum,
                roomTopPadding = generationConfig.roomTopPadding,
                roomBottomPadding = generationConfig.roomBottomPadding,
                roomLeftPadding = generationConfig.roomLeftPadding,
                roomRightPadding = generationConfig.roomRightPadding,
                roomTileBase = generationConfig.roomTileBase,
                roomSeedIncrement = generationConfig.roomSeedIncrement,
                poissonMinimumDistance = generationConfig.poissonMinimumDistance,
                poissonPoints = generationConfig.poissonPoints,
                poissonMaximumAttempts = generationConfig.poissonMaximumAttempts,
                mstExtraEdges = generationConfig.mstExtraEdges,
                corridorWidth = generationConfig.corridorWidth,
                mapWallValue = generationConfig.mapWallValue,
                mapFloorValue = generationConfig.mapFloorValue
            };

            GenerateDungeonNativeWithRooms(
                ref nativeConfig,
                seed,
                mapHandle.AddrOfPinnedObject(),
                roomHandle.AddrOfPinnedObject());
        }
        finally
        {
            roomHandle.Free();
            mapHandle.Free();
        }

        generatedMap = mapBuffer;
        generatedWidth = width;
        generatedHeight = height;
        generatedFloorValue = generationConfig.mapFloorValue;
        ApplyToTilemap(
            mapBuffer,
            width,
            height,
            generationConfig.mapWallValue,
            generationConfig.mapFloorValue);
        if (propSpawner != null)
        {
            propSpawner.Spawn(
                mapBuffer,
                roomBuffer,
                width,
                height,
                seed,
                floorTilemap,
                generationConfig.mapWallValue);
        }
    }

    public bool TryFindPathToTarget(
            Vector3 inputPosition,
            Vector3 targetPosition,
            List<Vector3> path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            path.Clear();
            if (generatedMap == null || floorTilemap == null)
            {
                return false;
            }

            Vector3Int inputCell = floorTilemap.WorldToCell(inputPosition);
            Vector3Int targetCell = floorTilemap.WorldToCell(targetPosition);
            Vector2Int start = FindNearestFloor(new Vector2Int(inputCell.x, inputCell.y));
            Vector2Int goal = FindNearestFloor(new Vector2Int(targetCell.x, targetCell.y));
            if (start.x < 0 || goal.x < 0)
            {
                return false;
            }

            int startIndex = start.y * generatedWidth + start.x;
            int goalIndex = goal.y * generatedWidth + goal.x;
            int[] previous = new int[generatedMap.Length];
            for (int i = 0; i < previous.Length; ++i)
            {
                previous[i] = -1;
            }

            Queue<int> frontier = new Queue<int>();
            frontier.Enqueue(startIndex);
            previous[startIndex] = startIndex;
            while (frontier.Count > 0 && previous[goalIndex] < 0)
            {
                int current = frontier.Dequeue();
                int x = current % generatedWidth;
                int y = current / generatedWidth;
                EnqueuePathNeighbor(x + 1, y, current, frontier, previous);
                EnqueuePathNeighbor(x - 1, y, current, frontier, previous);
                EnqueuePathNeighbor(x, y + 1, current, frontier, previous);
                EnqueuePathNeighbor(x, y - 1, current, frontier, previous);
            }

            if (previous[goalIndex] < 0)
            {
                return false;
            }

            List<int> indices = new List<int>();
            for (int current = goalIndex; ; current = previous[current])
            {
                indices.Add(current);
                if (current == startIndex)
                {
                    break;
                }
            }

            for (int i = indices.Count - 1; i >= 0; --i)
            {
                int index = indices[i];
                path.Add(floorTilemap.GetCellCenterWorld(new Vector3Int(
                    index % generatedWidth,
                    index / generatedWidth,
                    0)));
            }
            return true;
        }

        private byte[] generatedMap;
        private int generatedWidth;
        private int generatedHeight;
        private byte generatedFloorValue;

        private void EnqueuePathNeighbor(
            int x,
            int y,
            int previousIndex,
            Queue<int> frontier,
            int[] previous)
        {
            if (x < 0 || y < 0 || x >= generatedWidth || y >= generatedHeight)
            {
                return;
            }

            int index = y * generatedWidth + x;
            if (previous[index] >= 0 || generatedMap[index] != generatedFloorValue)
            {
                return;
            }

            previous[index] = previousIndex;
            frontier.Enqueue(index);
        }

        private Vector2Int FindNearestFloor(Vector2Int requested)
        {
            Vector2Int nearest = new Vector2Int(-1, -1);
            int nearestDistance = int.MaxValue;
            for (int y = 0; y < generatedHeight; ++y)
            {
                for (int x = 0; x < generatedWidth; ++x)
                {
                    if (generatedMap[y * generatedWidth + x] != generatedFloorValue)
                    {
                        continue;
                    }

                    int distance = Mathf.Abs(x - requested.x) + Mathf.Abs(y - requested.y);
                    if (distance < nearestDistance)
                    {
                        nearest = new Vector2Int(x, y);
                        nearestDistance = distance;
                    }
                }
            }
            return nearest;
        }

        private static void ValidateConfig(DungeonGenerationConfig generationConfig)
        {
            if (generationConfig == null ||
                generationConfig.mapWidth <= 0 ||
                generationConfig.mapHeight <= 0 ||
                generationConfig.roomWidth <= 0 ||
                generationConfig.roomHeight <= 0 ||
                generationConfig.mapWallValue == generationConfig.mapFloorValue)
            {
                throw new ArgumentException(
                    "Dungeon dimensions must be positive and wall/floor values must differ.",
                    nameof(generationConfig));
            }
        }

    internal static int GeneratePoissonCandidates(
        int width,
        int height,
        int minimumDistance,
        int pointCount,
        int maximumAttempts,
        int seed,
        int[] output) {
        if (output == null || output.Length < pointCount * 2)
        {
            throw new ArgumentException("The Poisson output buffer is too small.", nameof(output));
        }

        GCHandle handle = GCHandle.Alloc(output, GCHandleType.Pinned);
        try
        {
            return GeneratePoissonPointsNative(
                width,
                height,
                Mathf.Max(1, minimumDistance),
                pointCount,
                Mathf.Max(1, maximumAttempts),
                seed,
                handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    private void ApplyToTilemap(
        byte[] mapData,
        int width,
        int height,
        byte wallValue,
        byte floorValue)
    {
        if (floorTilemap == null || wallTilemap == null)
        {
            throw new InvalidOperationException(
                "DungeonManager requires both a floor Tilemap and a wall Tilemap.");
        }

        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        TileBase[] floorTileArray = new TileBase[mapData.Length];
        TileBase[] wallTileArray = new TileBase[mapData.Length];

        for (int i = 0; i < mapData.Length; i++)
        {
            if (mapData[i] == wallValue)
            {
                wallTileArray[i] = wallTile;
            }
            else if (mapData[i] == floorValue)
            {
                floorTileArray[i] = floorTile;
            }
        }

        BoundsInt bounds = new BoundsInt(0, 0, 0, width, height, 1);
        floorTilemap.SetTilesBlock(bounds, floorTileArray);
        wallTilemap.SetTilesBlock(bounds, wallTileArray);
    }
}