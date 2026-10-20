using UnityEngine;

[CreateAssetMenu(
    fileName = "PropSpawnProfile",
    menuName = "Quick Dungeon/Prop Spawn Profile",
    order = 0)]
public sealed class PropSpawnConfig : ScriptableObject
{
    [Header("Identification")]
    public string objectKey;
    public GameObject prefab;

    [Header("Quantity & Frequency")]
    public int countPerLevel;
    public int levelVariance;
    public int countPerRoom;
    public int roomVariance;
    [Range(0f, 1f)] public float roomIncludeChance = 1f;

    [Header("Footprint & Clearance")]
    public int width = 1;
    public int height = 1;
    public bool isSolid;
    public int minInstanceSeparation;

    [Header("Placement Rules & Anchors")]
    public SurfaceType surfaceType = SurfaceType.Floor;
    public PlacementMethod placementMethod = PlacementMethod.RandomScatter;

    [Header("Player & Distance Relations")]
    public bool isPlayer;
    public bool isFarthestFromPlayer;
    [Range(0f, 1f)] public float minDistanceFromPlayerRatio;
    [Range(0f, 1f)] public float maxDistanceFromPlayerRatio = 1f;

    [Header("Orientation & Grouping")]
    public RotationMode rotationMode = RotationMode.None;
    public int mutuallyExclusiveGroupId = -1;
}
