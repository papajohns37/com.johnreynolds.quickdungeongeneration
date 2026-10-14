This project will be a Unity package written in C++ to provide developers with quickly generated dungeon levels for top down games. Rooms will be generated with random walk. Rooms will be placed on the map randomly with Poisson Disk Sampling (which can also be used later to spawn enemies/items/points of interest).

## Prop and entity spawning

Add `DungeonPropSpawner` to the same scene as `DungeonManager`, assign it in the
manager, and configure its `spawnRules` list with prefabs and unique
`objectKey` values. The native generator now exports room ownership and a
bounded Poisson candidate set. The Unity spawner consumes those results after
tilemap generation and applies, in order:

1. player anchors and BFS distance fields;
2. farthest anchors;
3. level-wide Poisson placements;
4. per-room placements;
5. footprint, clearance, surface, separation, grouping, and orientation rules.

Generated instances are parented below `spawnedObjectsRoot` when assigned and
are destroyed and rebuilt on the next generation. The original
`GenerateDungeonNative` three-argument export remains available for native
callers; Unity uses `GenerateDungeonNativeWithRooms` for room-aware spawning.

## Inspector configuration and enemy paths

`DungeonManager` exposes its `DungeonGenerationConfig` in the Inspector. All
native generator settings are editable there; the seed remains an argument to
`Generate(int seed)` so each generation can choose its seed at runtime.

After generation, call `TryFindPathToTarget(inputPosition, targetPosition,
path)` with an enemy position, a goal position, and a reusable
`List<Vector3>`. The manager returns a four-direction path through generated
floor cells in world coordinates. Positions on walls or outside the map are
resolved to the nearest floor cell. The method returns `false` when no dungeon
has been generated or the two cells are disconnected.

## Unity setup

The package supports both a manual component workflow and a one-click editor
workflow:

1. Open `Tools > Quick Dungeon > Create Dungeon Generator`.
2. Assign floor and wall `TileBase` assets to the generated manager.
3. Create reusable dungeon settings with `Assets > Create > Quick Dungeon >
   Dungeon Generation Profile`. Assign the profile to `DungeonManager.Config`.
   Duplicate profiles to create larger or harder progression tiers.
4. Create a biome or level spawn table with `Assets > Create > Quick Dungeon >
   Dungeon Spawn Profile`, then assign it to `DungeonPropSpawner.Active Spawn
   Profile`.
5. Create reusable individual rules with `Assets > Create > Quick Dungeon >
   Prop Spawn Profile`, assign a prefab and an `objectKey`, and add those
   assets to the active dungeon spawn profile.
6. Call `DungeonManager.Generate(seed)` from your game flow.

The wizard creates a root object with a `Grid`, floor and wall tilemaps, a
`DungeonManager`, a `DungeonPropSpawner`, and a parent for generated props. It
also registers the objects with Unity's Undo system. A ready-to-configure
version can be created with `Tools > Quick Dungeon > Create Sample Scene`.

`DungeonSpawnProfile` is the container asset for a complete biome or level
spawn table. Swap the single `Active Spawn Profile` reference to change from a
cave to a dungeon or volcano configuration. Its `PropSpawnConfig` entries are
reusable individual rule assets, so shared enemy, loot, and decoration rules
do not need to be duplicated between profiles.

`DungeonGenerationConfig` assets work the same way for generation settings.
Create profiles such as `EarlyGame`, `DeepDungeon`, and `Endgame`, then assign
the desired profile before calling `Generate`. The manager's
`Generate(width, height, seed)` overload remains available for callers that
need to override dimensions at runtime.