using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public enum SurfaceType { Floor, Wall, Corner, Ceiling }

[Serializable]
public enum PlacementMethod { RandomScatter, PerimeterWallHugger, ClusterAroundAnchor, DeadEndOnly }

[Serializable]
public enum RotationMode { None, Random4Way, FaceRoomCenter, AlignToWall }

public sealed class DungeonPropSpawner : MonoBehaviour
{
    [SerializeField] private DungeonSpawnProfile activeSpawnProfile;
    [SerializeField] private Transform spawnedObjectsRoot;
    [SerializeField] private int poissonMaximumAttempts = 30;

    private readonly List<SpawnedPlacement> spawned = new List<SpawnedPlacement>();
    private System.Random random;
    private byte[] map;
    private int[] roomIds;
    private int[] playerDistances;
    private string[] occupancy;
    private int width;
    private int height;
    private Tilemap tilemap;
    private Vector2Int playerTile;
    private bool hasPlayer;
    private int maxPlayerDistance;
    private byte wallValue = 1;

    public DungeonSpawnProfile ActiveSpawnProfile
    {
        get { return activeSpawnProfile; }
        set { activeSpawnProfile = value; }
    }

    public void Configure(
        DungeonSpawnProfile profile,
        Transform objectsRoot = null)
    {
        activeSpawnProfile = profile;
        spawnedObjectsRoot = objectsRoot;
    }

    private List<PropSpawnConfig> SpawnRules
    {
        get
        {
            return activeSpawnProfile == null || activeSpawnProfile.spawnRules == null
                ? EmptyRules
                : activeSpawnProfile.spawnRules;
        }
    }

    private static readonly List<PropSpawnConfig> EmptyRules =
        new List<PropSpawnConfig>();

    private sealed class SpawnedPlacement
    {
        public PropSpawnConfig rule;
        public Vector2Int tile;
        public GameObject instance;
    }

    public void Spawn(
        byte[] mapData,
        int[] roomData,
        int mapWidth,
        int mapHeight,
        int seed,
        Tilemap targetTilemap,
        byte wallTileValue = 1)
    {
        if (mapData == null || roomData == null ||
            mapData.Length != mapWidth * mapHeight ||
            roomData.Length != mapData.Length || targetTilemap == null)
        {
            throw new ArgumentException("Dungeon spawn buffers are invalid.");
        }

        ClearSpawnedObjects();
        map = mapData;
        roomIds = roomData;
        width = mapWidth;
        height = mapHeight;
        tilemap = targetTilemap;
        wallValue = wallTileValue;
        occupancy = new string[map.Length];
        random = new System.Random(seed);
        playerDistances = new int[map.Length];

        PlacePlayerAnchors();
        CalculatePlayerDistances();
        PlaceFarthestAnchors();
        PlaceGlobalRules();
        PlaceRoomRules();
    }

    private void PlacePlayerAnchors()
    {
        for (int i = 0; i < SpawnRules.Count; ++i)
        {
            PropSpawnConfig rule = SpawnRules[i];
            if (!IsUsableRule(rule) || !rule.isPlayer)
            {
                continue;
            }

            Vector2Int tile;
            if (!FindCandidate(rule, -1, false, out tile))
            {
                Debug.LogWarning("No valid player anchor location for " + rule.objectKey, this);
                continue;
            }

            if (TrySpawn(rule, tile))
            {
                if (!hasPlayer)
                {
                    playerTile = tile;
                    hasPlayer = true;
                }
            }
        }
    }

    private void PlaceFarthestAnchors()
    {
        for (int i = 0; i < SpawnRules.Count; ++i)
        {
            PropSpawnConfig rule = SpawnRules[i];
            if (!IsUsableRule(rule) || !rule.isFarthestFromPlayer)
            {
                continue;
            }

            Vector2Int best = new Vector2Int();
            int bestDistance = -1;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    int index = y * width + x;
                    if (playerDistances[index] > bestDistance &&
                        IsValidCandidate(rule, new Vector2Int(x, y), -1, false))
                    {
                        best = new Vector2Int(x, y);
                        bestDistance = playerDistances[index];
                    }
                }
            }

            if (bestDistance >= 0)
            {
                TrySpawn(rule, best);
            }
        }
    }

    private void PlaceGlobalRules()
    {
        for (int i = 0; i < SpawnRules.Count; ++i)
        {
            PropSpawnConfig rule = SpawnRules[i];
            if (!IsUsableRule(rule) || rule.isPlayer || rule.isFarthestFromPlayer)
            {
                continue;
            }

            int count = RandomCount(rule.countPerLevel, rule.levelVariance);
            PlaceCount(rule, count, -1, false, i);
        }
    }

    private void PlaceRoomRules()
    {
        int maxRoomId = 0;
        for (int i = 0; i < roomIds.Length; ++i)
        {
            if (roomIds[i] > maxRoomId)
            {
                maxRoomId = roomIds[i];
            }
        }

        for (int ruleIndex = 0; ruleIndex < SpawnRules.Count; ++ruleIndex)
        {
            PropSpawnConfig rule = SpawnRules[ruleIndex];
            if (!IsUsableRule(rule) || rule.isPlayer || rule.isFarthestFromPlayer ||
                rule.countPerRoom <= 0)
            {
                continue;
            }

            for (int roomId = 1; roomId <= maxRoomId; ++roomId)
            {
                if (random.NextDouble() > Mathf.Clamp01(rule.roomIncludeChance))
                {
                    continue;
                }

                PlaceCount(
                    rule,
                    RandomCount(rule.countPerRoom, rule.roomVariance),
                    roomId,
                    true,
                    ruleIndex);
            }
        }
    }

    private void PlaceCount(
        PropSpawnConfig rule,
        int count,
        int roomId,
        bool restrictToRoom,
        int ruleIndex)
    {
        if (count <= 0)
        {
            return;
        }

        int pointCapacity = Mathf.Max(8, count * 12);
        int[] points = new int[pointCapacity * 2];
        int pointCount = DungeonManager.GeneratePoissonCandidates(
            width,
            height,
            Mathf.Max(1, rule.minInstanceSeparation),
            pointCapacity,
            Mathf.Max(1, poissonMaximumAttempts),
            random.Next(),
            points);

        int placed = 0;
        for (int i = 0; i < pointCount && placed < count; ++i)
        {
            Vector2Int tile = new Vector2Int(points[i * 2], points[i * 2 + 1]);
            if (restrictToRoom && roomIds[tile.y * width + tile.x] != roomId)
            {
                continue;
            }
            if (IsValidCandidate(rule, tile, roomId, restrictToRoom) &&
                TrySpawn(rule, tile))
            {
                ++placed;
            }
        }

        List<int> fallbackIndices = new List<int>();
        for (int index = 0; index < map.Length; ++index)
        {
            if (map[index] == wallValue) continue;
            if (restrictToRoom && roomIds[index] != roomId) continue;

            fallbackIndices.Add(index);
        }

        for (int i = fallbackIndices.Count - 1; i > 0; --i)
        {
            int rnd = random.Next(i + 1);
            int temp = fallbackIndices[i];
            fallbackIndices[i] = fallbackIndices[rnd];
            fallbackIndices[rnd] = temp;
        }

        for (int i = 0; i < fallbackIndices.Count && placed < count; ++i)
        {
            int index = fallbackIndices[i];
            Vector2Int tile = new Vector2Int(index % width, index / width);
            if (IsValidCandidate(rule, tile, roomId, restrictToRoom) &&
                TrySpawn(rule, tile))
            {
                ++placed;
            }
        }
    }

    private bool FindCandidate(PropSpawnConfig rule, int roomId, bool restrictToRoom, out Vector2Int result)
    {
        for (int i = 0; i < map.Length; ++i)
        {
            int index = (i * 7919 + random.Next(map.Length)) % map.Length;
            if (map[index] == wallValue)
            {
                continue;
            }
            Vector2Int tile = new Vector2Int(index % width, index / width);
            if ((!restrictToRoom || roomIds[index] == roomId) &&
                IsValidCandidate(rule, tile, roomId, restrictToRoom))
            {
                result = tile;
                return true;
            }
        }

        result = new Vector2Int();
        return false;
    }

    private bool IsValidCandidate(PropSpawnConfig rule, Vector2Int tile, int roomId, bool restrictToRoom)
    {
        if (tile.x < 0 || tile.y < 0 || tile.x >= width || tile.y >= height ||
            (!restrictToRoom && map[tile.y * width + tile.x] == wallValue))
        {
            return false;
        }

        float minDistance = Mathf.Clamp01(Mathf.Min(rule.minDistanceFromPlayerRatio, rule.maxDistanceFromPlayerRatio));
        float maxDistance = Mathf.Clamp01(Mathf.Max(rule.minDistanceFromPlayerRatio, rule.maxDistanceFromPlayerRatio));
        float ratio = maxPlayerDistance == 0 ? 0f : (float)playerDistances[tile.y * width + tile.x] / maxPlayerDistance;
        if (hasPlayer && (ratio < minDistance || ratio > maxDistance))
        {
            return false;
        }

        if (restrictToRoom && roomIds[tile.y * width + tile.x] != roomId)
        {
            return false;
        }
        if (!FootprintIsValid(rule, tile) || !MatchesSurface(rule, tile))
        {
            return false;
        }
        if (rule.isSolid && !HasClearance(rule, tile))
        {
            return false;
        }
        if (IsTooCloseToSameObject(rule, tile))
        {
            return false;
        }

        if (rule.placementMethod == PlacementMethod.DeadEndOnly && FloorNeighbours(tile) != 1)
        {
            return false;
        }
        if (rule.placementMethod == PlacementMethod.ClusterAroundAnchor &&
            !HasNearbyObject(rule, tile))
        {
            return false;
        }
        return true;
    }

    private bool TrySpawn(PropSpawnConfig rule, Vector2Int tile)
    {
        int index = tile.y * width + tile.x;
        if (occupancy[index] != null)
        {
            SpawnedPlacement existing = FindPlacement(tile);
            if (existing == null || existing.rule.mutuallyExclusiveGroupId < 0 ||
                existing.rule.mutuallyExclusiveGroupId != rule.mutuallyExclusiveGroupId ||
                random.Next(2) != 0)
            {
                return false;
            }
            RemovePlacement(existing);
        }

        Vector3 position = tilemap.GetCellCenterWorld(new Vector3Int(tile.x, tile.y, 0));
        Quaternion rotation = CalculateRotation(rule, tile);
        Transform parent = spawnedObjectsRoot != null ? spawnedObjectsRoot : transform;
        GameObject instance = Instantiate(rule.prefab, position, rotation, parent);
        SpawnedPlacement placement = new SpawnedPlacement { rule = rule, tile = tile, instance = instance };
        spawned.Add(placement);

        for (int y = 0; y < Mathf.Max(1, rule.height); ++y)
        {
            for (int x = 0; x < Mathf.Max(1, rule.width); ++x)
            {
                occupancy[(tile.y + y) * width + tile.x + x] = rule.objectKey;
            }
        }
        return true;
    }

    private Quaternion CalculateRotation(PropSpawnConfig rule, Vector2Int tile)
    {
        switch (rule.rotationMode)
        {
            case RotationMode.Random4Way:
                return Quaternion.Euler(0f, 0f, random.Next(4) * 90f);
            case RotationMode.FaceRoomCenter:
                Vector2 center = FindRoomCenter(tile);
                return Quaternion.Euler(0f, 0f, Mathf.Atan2(center.y - tile.y, center.x - tile.x) * Mathf.Rad2Deg);
            case RotationMode.AlignToWall:
                return Quaternion.Euler(0f, 0f, WallNormalAngle(tile) + 90f);
            default:
                return Quaternion.identity;
        }
    }

    private Vector2 FindRoomCenter(Vector2Int tile)
    {
        int roomId = roomIds[tile.y * width + tile.x];
        Vector2 sum = Vector2.zero;
        int count = 0;
        for (int i = 0; i < roomIds.Length; ++i)
        {
            if (roomIds[i] == roomId)
            {
                sum += new Vector2(i % width, i / width);
                ++count;
            }
        }
        return count == 0 ? tile : sum / count;
    }

    private float WallNormalAngle(Vector2Int tile)
    {
        if (IsWall(tile + Vector2Int.up)) return 90f;
        if (IsWall(tile + Vector2Int.right)) return 0f;
        if (IsWall(tile + Vector2Int.down)) return -90f;
        return 180f;
    }

    private bool MatchesSurface(PropSpawnConfig rule, Vector2Int tile)
    {
        int walls = 0;
        if (IsWall(tile + Vector2Int.up)) ++walls;
        if (IsWall(tile + Vector2Int.right)) ++walls;
        if (IsWall(tile + Vector2Int.down)) ++walls;
        if (IsWall(tile + Vector2Int.left)) ++walls;
        if (rule.surfaceType == SurfaceType.Wall) return walls > 0;
        if (rule.surfaceType == SurfaceType.Corner) return walls >= 2;
        return map[tile.y * width + tile.x] != wallValue;
    }

    private bool FootprintIsValid(PropSpawnConfig rule, Vector2Int tile)
    {
        int objectWidth = Mathf.Max(1, rule.width);
        int objectHeight = Mathf.Max(1, rule.height);
        if (tile.x + objectWidth > width || tile.y + objectHeight > height) return false;
        for (int y = 0; y < objectHeight; ++y)
        {
            for (int x = 0; x < objectWidth; ++x)
            {
                int index = (tile.y + y) * width + tile.x + x;
                if (map[index] == wallValue || occupancy[index] != null) return false;
            }
        }
        return true;
    }

    private bool HasClearance(PropSpawnConfig rule, Vector2Int tile)
    {
        int left = tile.x - 1;
        int right = tile.x + Mathf.Max(1, rule.width);
        int bottom = tile.y - 1;
        int top = tile.y + Mathf.Max(1, rule.height);
        for (int y = bottom; y <= top; ++y)
        {
            for (int x = left; x <= right; ++x)
            {
                if (x >= 0 && y >= 0 && x < width && y < height &&
                    map[y * width + x] == wallValue) return false;
            }
        }
        return true;
    }

    private bool IsTooCloseToSameObject(PropSpawnConfig rule, Vector2Int tile)
    {
        int distance = Mathf.Max(0, rule.minInstanceSeparation);
        int distanceSquared = distance * distance;
        for (int i = 0; i < spawned.Count; ++i)
        {
            if (spawned[i].rule.objectKey == rule.objectKey)
            {
                Vector2Int delta = spawned[i].tile - tile;
                if (delta.sqrMagnitude < distanceSquared) return true;
            }
        }
        return false;
    }

    private bool HasNearbyObject(PropSpawnConfig rule, Vector2Int tile)
    {
        for (int i = 0; i < spawned.Count; ++i)
        {
            if (spawned[i].rule.objectKey == rule.objectKey &&
                (spawned[i].tile - tile).sqrMagnitude <= 25)
            {
                return true;
            }
        }
        return false;
    }

    private int FloorNeighbours(Vector2Int tile)
    {
        int count = 0;
        if (!IsWall(tile + Vector2Int.up)) ++count;
        if (!IsWall(tile + Vector2Int.right)) ++count;
        if (!IsWall(tile + Vector2Int.down)) ++count;
        if (!IsWall(tile + Vector2Int.left)) ++count;
        return count;
    }

    private bool IsWall(Vector2Int tile)
    {
        return tile.x < 0 || tile.y < 0 || tile.x >= width || tile.y >= height ||
               map[tile.y * width + tile.x] == wallValue;
    }

    private void CalculatePlayerDistances()
    {
        for (int i = 0; i < playerDistances.Length; ++i) playerDistances[i] = -1;
        if (!hasPlayer) return;
        int[] queue = new int[map.Length];
        int head = 0;
        int tail = 0;
        int start = playerTile.y * width + playerTile.x;
        playerDistances[start] = 0;
        queue[tail++] = start;
        while (head < tail)
        {
            int current = queue[head++];
            int x = current % width;
            int y = current / width;
            int nextDistance = playerDistances[current] + 1;
            TryVisit(x - 1, y, nextDistance, queue, ref tail);
            TryVisit(x + 1, y, nextDistance, queue, ref tail);
            TryVisit(x, y - 1, nextDistance, queue, ref tail);
            TryVisit(x, y + 1, nextDistance, queue, ref tail);
        }
        maxPlayerDistance = 0;
        for (int i = 0; i < playerDistances.Length; ++i)
        {
            if (playerDistances[i] > maxPlayerDistance) maxPlayerDistance = playerDistances[i];
        }
    }

    private void TryVisit(int x, int y, int distance, int[] queue, ref int tail)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        int index = y * width + x;
        if (map[index] == wallValue || playerDistances[index] >= 0) return;
        playerDistances[index] = distance;
        queue[tail++] = index;
    }

    private int RandomCount(int value, int variance)
    {
        if (variance <= 0) return Mathf.Max(0, value);
        return Mathf.Max(0, value + random.Next(-variance, variance + 1));
    }

    private bool IsUsableRule(PropSpawnConfig rule)
    {
        return rule != null && rule.prefab != null && !string.IsNullOrEmpty(rule.objectKey);
    }

    private SpawnedPlacement FindPlacement(Vector2Int tile)
    {
        for (int i = 0; i < spawned.Count; ++i)
            if (spawned[i].tile == tile) return spawned[i];
        return null;
    }

    private void RemovePlacement(SpawnedPlacement placement)
    {
        if (placement.instance != null) Destroy(placement.instance);
        for (int y = 0; y < Mathf.Max(1, placement.rule.height); ++y)
            for (int x = 0; x < Mathf.Max(1, placement.rule.width); ++x)
                occupancy[(placement.tile.y + y) * width + placement.tile.x + x] = null;
        spawned.Remove(placement);
    }

    private void ClearSpawnedObjects()
    {
        for (int i = 0; i < spawned.Count; ++i)
            if (spawned[i].instance != null) Destroy(spawned[i].instance);
        spawned.Clear();
    }
}
