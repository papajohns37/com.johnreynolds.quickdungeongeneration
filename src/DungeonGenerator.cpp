#include "DungeonGenerator.h"

#include "AStarPathfinder.h"
#include "MSTSolver.h"
#include "OrthogonalRoutingPathfinder.h"
#include "PoissonDiskSampler.h"
#include "RandomWalkRoomGenerator.h"
#include "RandomWalkRoomGeneratorConfig.h"
#include "WallThicknessEnforcer.h"

#include <unordered_set>
#include <utility>
#include <vector>

namespace {
    void copyRoomToMap(
        const Grid<uint8_t>& room,
        int roomValue,
        Vector2Int center,
        const DungeonGeneratorConfig& config,
        Grid<uint8_t>& map) {
        const int left = center.x - config.roomWidth / 2;
        const int top = center.y - config.roomHeight / 2;
        for (int y = 0; y < config.roomHeight; ++y) {
            for (int x = 0; x < config.roomWidth; ++x) {
                if (room(x, y) == roomValue) {
                    map(left + x, top + y) = static_cast<uint8_t>(roomValue);
                }
            }
        }
    }

    std::unordered_set<int> corridorBlockedTiles(
        int roomCount,
        int roomTileBase,
        int sourceRoom,
        int destinationRoom) {
        std::unordered_set<int> blockedTiles;
        for (int room = 0; room < roomCount; ++room) {
            const int roomValue = roomTileBase + room;
            if (roomValue != sourceRoom && roomValue != destinationRoom) {
                blockedTiles.insert(roomValue);
            }
        }
        return blockedTiles;
    }
}

DungeonGenerationResult DungeonGenerator::generate(
    const DungeonGeneratorConfig& config,
    int seed) const {
    Grid<uint8_t> map(config.mapWidth, config.mapHeight, config.mapWallValue);
    Grid<int32_t> roomIds(config.mapWidth, config.mapHeight, 0);
    PoissonDiskSampler sampler;
    const auto localSamples = sampler.generateIntegerPoints(
        config.mapWidth - config.roomWidth,
        config.mapHeight - config.roomHeight,
        config.poissonMinimumDistance,
        config.poissonPoints,
        config.poissonMaximumAttempts,
        seed);

    std::vector<Vector2Int> rooms;
    rooms.reserve(localSamples.size());
    for (const auto& sample : localSamples) {
        rooms.push_back({
            sample.x + config.roomWidth / 2,
            sample.y + config.roomHeight / 2
        });
    }

    RandomWalkRoomGenerator roomGenerator;
    for (int roomIndex = 0; roomIndex < static_cast<int>(rooms.size()); ++roomIndex) {
        const int roomValue = config.roomTileBase + roomIndex;
        Grid<uint8_t> room(config.roomWidth, config.roomHeight, config.mapWallValue);
        const RandomWalkRoomGeneratorConfig roomConfig{
            config.roomWidth,
            config.roomHeight,
            config.roomSteps,
            config.roomBrushSize,
            config.wallHeightMinimum,
            config.wallWidthMinimum,
            config.roomTopPadding,
            config.roomBottomPadding,
            config.roomLeftPadding,
            config.roomRightPadding,
            roomValue,
            {config.roomWidth / 2, config.roomHeight / 2}
        };
        roomGenerator.generate(
            roomConfig,
            room,
            seed + roomIndex * config.roomSeedIncrement);
        copyRoomToMap(room, roomValue, rooms[roomIndex], config, map);
        const int left = rooms[roomIndex].x - config.roomWidth / 2;
        const int top = rooms[roomIndex].y - config.roomHeight / 2;
        for (int y = 0; y < config.roomHeight; ++y) {
            for (int x = 0; x < config.roomWidth; ++x) {
                if (room(x, y) == roomValue) {
                    roomIds(left + x, top + y) = roomIndex + 1;
                }
            }
        }
    }

    MSTSolver solver;
    const auto edges = solver.findMST(
        static_cast<int>(rooms.size()),
        solver.getEdgesForPoints(rooms),
        config.mstExtraEdges);

    OrthogonalRoutingPathfinder pathfinder;
    AStarPathfinder aStarPathfinder;
    int astarCalls = 0;
    for (const auto& edge : edges) {
        const int sourceRoom = config.roomTileBase + edge.v1;
        const int destinationRoom = config.roomTileBase + edge.v2;
        auto path = pathfinder.FindPath(
            map,
            rooms[edge.v1],
            rooms[edge.v2],
            config.corridorWidth + 2 * config.wallHeightMinimum > config.wallWidthMinimum ? config.wallHeightMinimum : config.wallWidthMinimum,
            config.corridorWidth,
            corridorBlockedTiles(
                static_cast<int>(rooms.size()),
                config.roomTileBase,
                sourceRoom,
                destinationRoom));

        // If this edge is part of the MST and could not be found, use A* to ensure the dungeon is fully accessible
        if (path.empty() && edges.size() < rooms.size()) {
            astarCalls++;
            path = aStarPathfinder.FindPath(
                map,
                rooms[edge.v1],
                rooms[edge.v2],
                config.corridorWidth + 2 * config.wallHeightMinimum > config.wallWidthMinimum ? config.wallHeightMinimum : config.wallWidthMinimum,
                config.corridorWidth,
                corridorBlockedTiles(
                    static_cast<int>(rooms.size()),
                    config.roomTileBase,
                    sourceRoom,
                    destinationRoom));

            if (path.empty()){
                //Final resort, we need to generate a different dungeon to ensure no disconnected dungeons will be built
                return generate(config, seed - 1);
            }
        }

        for (const auto& point : path) {
            if (point.x >= 0 && point.x < config.mapWidth &&
                point.y >= 0 && point.y < config.mapHeight &&
                map(point.x, point.y) == config.mapWallValue) {
                map(point.x, point.y) = config.mapFloorValue;
            }
        }
    }

    for (int y = 0; y < config.mapHeight; ++y) {
        for (int x = 0; x < config.mapWidth; ++x) {
            if (map(x, y) != config.mapWallValue) {
                map(x, y) = config.mapFloorValue;
            }
        }
    }

    const RandomWalkRoomGeneratorConfig fullMapConfig{
        config.mapWidth,
        config.mapHeight,
        0,
        0,
        config.wallHeightMinimum,
        config.wallWidthMinimum,
        0,
        0,
        0,
        0,
        config.mapFloorValue,
        {0, 0}
    };
    WallThicknessEnforcer wallThicknessEnforcer;
    wallThicknessEnforcer.enforceWallThickness(map, fullMapConfig);

    return {std::move(map), std::move(roomIds), astarCalls};
}
