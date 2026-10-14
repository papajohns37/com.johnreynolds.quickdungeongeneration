using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public sealed class DungeonGenerationConfig
{
    [Header("Map")]
    public int mapWidth = 48;
    public int mapHeight = 48;
    public byte mapWallValue = 1;
    public byte mapFloorValue = 2;

    [Header("Rooms")]
    public int roomWidth = 25;
    public int roomHeight = 25;
    public int roomSteps = 500;
    public int roomBrushSize = 2;
    public int wallHeightMinimum = 3;
    public int wallWidthMinimum = 1;
    public int roomTopPadding = 3;
    public int roomBottomPadding = 3;
    public int roomLeftPadding = 2;
    public int roomRightPadding = 2;
    public int roomTileBase = 10;
    public int roomSeedIncrement = 1;

    [Header("Connections and Sampling")]
    public int poissonMinimumDistance = 27;
    public int poissonPoints = 100;
    public int poissonMaximumAttempts = 30;
    public int mstExtraEdges = 1;
    public int corridorWidth = 2;
}

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

    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase wallTile;
    [SerializeField] private TileBase floorTile;
    [SerializeField] private DungeonPropSpawner propSpawner;
    [SerializeField] private DungeonGenerationConfig config = new DungeonGenerationConfig();

    public void Generate(int seed)
    {
        Generate(config, seed);
    }

    // Kept for callers that supplied dimensions before the inspector configuration existed.
    public void Generate(int width, int height, int seed)
    {
        DungeonGenerationConfig requestedConfig = new DungeonGenerationConfig
        {
            mapWidth = width,
            mapHeight = height,
            mapWallValue = config.mapWallValue,
            mapFloorValue = config.mapFloorValue,
            roomWidth = config.roomWidth,
            roomHeight = config.roomHeight,
            roomSteps = config.roomSteps,
            roomBrushSize = config.roomBrushSize,
            wallHeightMinimum = config.wallHeightMinimum,
            wallWidthMinimum = config.wallWidthMinimum,
            roomTopPadding = config.roomTopPadding,
            roomBottomPadding = config.roomBottomPadding,
            roomLeftPadding = config.roomLeftPadding,
            roomRightPadding = config.roomRightPadding,
            roomTileBase = config.roomTileBase,
            roomSeedIncrement = config.roomSeedIncrement,
            poissonMinimumDistance = config.poissonMinimumDistance,
            poissonPoints = config.poissonPoints,
            poissonMaximumAttempts = config.poissonMaximumAttempts,
            mstExtraEdges = config.mstExtraEdges,
            corridorWidth = config.corridorWidth
        };
        Generate(requestedConfig, seed);
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
                tilemap,
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
            if (generatedMap == null || tilemap == null)
            {
                return false;
            }

            Vector3Int inputCell = tilemap.WorldToCell(inputPosition);
            Vector3Int targetCell = tilemap.WorldToCell(targetPosition);
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
                path.Add(tilemap.GetCellCenterWorld(new Vector3Int(
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
        tilemap.ClearAllTiles();
        TileBase[] tileArray = new TileBase[mapData.Length];

        for (int i = 0; i < mapData.Length; i++)
        {
            tileArray[i] = mapData[i] == wallValue
                ? wallTile
                : mapData[i] == floorValue
                    ? floorTile
                    : null;
        }

        BoundsInt bounds = new BoundsInt(0, 0, 0, width, height, 1);
        tilemap.SetTilesBlock(bounds, tileArray);
    }
}