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
    private static extern void GenerateDungeonNative(ref DungeonConfigInterop config, int seed, IntPtr outMapData);

    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase wallTile;
    [SerializeField] private TileBase floorTile;

    public void Generate(int width, int height, int seed)
    {
        int totalCells = width * height;
        byte[] mapBuffer = new byte[totalCells];

        GCHandle handle = GCHandle.Alloc(mapBuffer, GCHandleType.Pinned);
        try
        {
            var config = new DungeonConfigInterop
            {
                mapWidth = width,
                mapHeight = height,
                roomWidth = 50,
                roomHeight = 50,
                roomSteps = 2000,
                roomBrushSize = 2,
                wallHeightMinimum = 3,
                wallWidthMinimum = 1,
                roomTopPadding = 3,
                roomBottomPadding = 3,
                roomLeftPadding = 2,
                roomRightPadding = 2,
                roomTileBase = 10,
                roomSeedIncrement = 1,
                poissonMinimumDistance = 50,
                poissonPoints = 1600,
                poissonMaximumAttempts = 30,
                mstExtraEdges = 1,
                corridorWidth = 2,
                mapWallValue = 1,
                mapFloorValue = 2
            };

            GenerateDungeonNative(ref config, seed, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }

        ApplyToTilemap(mapBuffer, width, height);
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