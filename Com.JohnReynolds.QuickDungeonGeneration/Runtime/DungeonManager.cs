using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    public void Generate(int width, int height, int seed)
    {
        int totalCells = width * height;
        byte[] mapBuffer = new byte[totalCells];
        int[] roomBuffer = new int[totalCells];

        GCHandle mapHandle = GCHandle.Alloc(mapBuffer, GCHandleType.Pinned);
        GCHandle roomHandle = GCHandle.Alloc(roomBuffer, GCHandleType.Pinned);
        try
        {
            var config = new DungeonConfigInterop
            {
                mapWidth = width,
                mapHeight = height,
                roomWidth = 25,
                roomHeight = 25,
                roomSteps = 500,
                roomBrushSize = 2,
                wallHeightMinimum = 3,
                wallWidthMinimum = 1,
                roomTopPadding = 3,
                roomBottomPadding = 3,
                roomLeftPadding = 2,
                roomRightPadding = 2,
                roomTileBase = 10,
                roomSeedIncrement = 1,
                poissonMinimumDistance = 27,
                poissonPoints = 100,
                poissonMaximumAttempts = 30,
                mstExtraEdges = 1,
                corridorWidth = 2,
                mapWallValue = 1,
                mapFloorValue = 2
            };

            GenerateDungeonNativeWithRooms(
                ref config,
                seed,
                mapHandle.AddrOfPinnedObject(),
                roomHandle.AddrOfPinnedObject());
        }
        finally
        {
            roomHandle.Free();
            mapHandle.Free();
        }

        ApplyToTilemap(mapBuffer, width, height);
        if (propSpawner != null)
        {
            propSpawner.Spawn(mapBuffer, roomBuffer, width, height, seed, tilemap);
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

    private void ApplyToTilemap(byte[] mapData, int width, int height)
    {
        tilemap.ClearAllTiles();
        TileBase[] tileArray = new TileBase[mapData.Length];

        for (int i = 0; i < mapData.Length; i++)
        {
            tileArray[i] = mapData[i] switch
            {
                1 => wallTile,
                2 => floorTile,
                _ => null
            };
        }

        BoundsInt bounds = new BoundsInt(0, 0, 0, width, height, 1);
        tilemap.SetTilesBlock(bounds, tileArray);
    }
}