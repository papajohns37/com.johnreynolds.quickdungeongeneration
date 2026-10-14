# Quick Dungeon Sample

Open `QuickDungeonSample.unity` to start with the manager, grid, tilemaps, and
prop spawner already connected. Assign project-specific floor and wall tiles,
then add reusable `PropSpawnConfig` assets to the assigned
`DungeonSpawnProfile` before generating a dungeon. Duplicate that container to
create alternate biome or progression spawn tables.

The sample intentionally does not include a native game-specific tile or prop
library. This keeps the package sample compatible with 2D projects using their
own art and prefabs.
