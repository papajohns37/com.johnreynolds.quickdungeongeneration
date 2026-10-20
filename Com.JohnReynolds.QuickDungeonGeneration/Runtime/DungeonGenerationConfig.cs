using UnityEngine;

[CreateAssetMenu(
    fileName = "DungeonGenerationProfile",
    menuName = "Quick Dungeon/Dungeon Generation Profile",
    order = 1)]
public sealed class DungeonGenerationConfig : ScriptableObject
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
