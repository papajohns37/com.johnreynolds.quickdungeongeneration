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