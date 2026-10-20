using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DungeonSpawnProfile",
    menuName = "Quick Dungeon/Dungeon Spawn Profile",
    order = 2)]
public sealed class DungeonSpawnProfile : ScriptableObject
{
    [Header("Biome / Level Spawn Rules")]
    public List<PropSpawnConfig> spawnRules = new List<PropSpawnConfig>();
}
