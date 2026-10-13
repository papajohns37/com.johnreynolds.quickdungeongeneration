#include "AStarPathfinder.h"
#include "DisjointSet.h"
#include "DungeonGenerator.h"
#include "PoissonDiskSampler.h"
#include "RandomWalkRoomGeneratorConfig.h"
#include "WallThicknessEnforcer.h"

#include <cstdint>
#include <iostream>
#include <stdexcept>
#include <unordered_set>
#include <vector>

namespace {
    struct DungeonConfigInterop {
        int mapWidth, mapHeight, roomWidth, roomHeight;
        int roomSteps, roomBrushSize, wallHeightMinimum, wallWidthMinimum;
        int roomTopPadding, roomBottomPadding, roomLeftPadding, roomRightPadding;
        int roomTileBase, roomSeedIncrement, poissonMinimumDistance;
        int poissonPoints, poissonMaximumAttempts, mstExtraEdges, corridorWidth;
        uint8_t mapWallValue, mapFloorValue;
    };

    extern "C" void GenerateDungeonNative(
        const DungeonConfigInterop* config, int seed, uint8_t* outMapData);

    void require(bool condition, const char* message) {
        if (!condition) {
            throw std::runtime_error(message);
        }
    }

    bool contains(const std::vector<Vector2Int>& points, Vector2Int value) {
        for (const auto& point : points) {
            if (point.x == value.x && point.y == value.y) {
                return true;
            }
        }
        return false;
    }

    DungeonGeneratorConfig dungeonConfig() {
        return {
            48, 48, 1, 2,
            8, 8, 1, 1, 1, 1,
            1, 12, 1, 1, 10, 7,
            2, 1, 12.0f, 4, 20
        };
    }

    void testDisjointSetUnitesComponents() {
        DisjointSet sets(4);
        require(sets.find(0) != sets.find(1), "New disjoint-set items should not share a root.");
        require(sets.unite(0, 1), "Unite should report when two components are joined.");
        require(sets.find(0) == sets.find(1), "Joined items should share a root.");
        require(!sets.unite(0, 1), "Unite should reject an already joined pair.");
        require(sets.unite(2, 3), "A second component should be joinable.");
        require(sets.unite(1, 2), "Two distinct components should be joinable.");
        require(sets.find(0) == sets.find(3), "Transitive unions should share a root.");
    }

    void testAStarFindsPathsAtMapEdges() {
        Grid<uint8_t> map(5, 5, 0);
        AStarPathfinder pathfinder;
        const auto path = pathfinder.FindPath(map, {0, 0}, {4, 4}, 1, 1, {1});

        require(!path.empty(), "A* should find a path on an open map.");
        require(contains(path, {0, 0}) && contains(path, {4, 4}),
                "A* paths should include both endpoints.");
    }

    void testAStarAvoidsBlockedTilesAndReportsNoRoute() {
        Grid<uint8_t> map(5, 5, 0);
        for (int y = 0; y < map.height; ++y) {
            map(2, y) = 1;
        }
        AStarPathfinder pathfinder;
        const auto blocked = pathfinder.FindPath(map, {0, 2}, {4, 2}, 1, 1, {1});
        require(blocked.empty(), "A* should report no path through an impassable wall.");

        map(2, 2) = 0;
        const auto route = pathfinder.FindPath(map, {0, 2}, {4, 2}, 1, 1, {1});
        require(!route.empty() && contains(route, {2, 2}),
                "A* should use the opening in a blocking wall.");
    }

    void testAStarHandlesTrivialAndInvalidRequests() {
        Grid<uint8_t> map(3, 3, 0);
        AStarPathfinder pathfinder;
        require(pathfinder.FindPath(map, {1, 1}, {1, 1}, 1, 1, {1}).size() == 1,
                "A* should return the start tile for a trivial path.");
        require(pathfinder.FindPath(map, {-1, 0}, {1, 1}, 1, 1, {1}).empty(),
                "A* should reject an out-of-bounds start.");
        map(1, 1) = 1;
        require(pathfinder.FindPath(map, {1, 1}, {2, 2}, 1, 1, {1}).empty(),
                "A* should reject a blocked start.");
    }

    void testIntegerPoissonPointsAreInBounds() {
        PoissonDiskSampler sampler;
        const auto points = sampler.generateIntegerPoints(20.0f, 15.0f, 3.0f, 10, 20, 42);
        for (const auto point : points) {
            require(point.x >= 0 && point.x < 20 && point.y >= 0 && point.y < 15,
                    "Integer Poisson points should remain inside the sampling bounds.");
        }
    }

    void testWallThicknessEnforcerFillsShortRuns() {
        Grid<uint8_t> map(7, 7, 0);
        map(2, 3) = 1;
        map(3, 3) = 1;
        map(3, 2) = 1;
        RandomWalkRoomGeneratorConfig config{
            7, 7, 0, 1, 3, 3, 0, 0, 0, 0, 0, {3, 3}
        };

        WallThicknessEnforcer{}.enforceWallThickness(map, config);

        for (int x = 0; x < map.width; ++x) {
            require(map(x, 3) == 0,
                    "Short horizontal wall runs should be converted to floor.");
        }
        for (int y = 0; y < map.height; ++y) {
            require(map(3, y) == 0,
                    "Short vertical wall runs should be converted to floor.");
        }
    }

    void testDungeonGenerationIsDeterministicAndProducesMap() {
        const auto config = dungeonConfig();
        DungeonGenerator generator;
        const auto first = generator.generate(config, 1234);
        const auto second = generator.generate(config, 1234);

        require(first.map.width == config.mapWidth && first.map.height == config.mapHeight,
                "Generated map dimensions should match the configuration.");
        require(first.map.data == second.map.data,
                "Dungeon generation should be deterministic for a fixed seed.");
        require(first.astarCalls >= 0, "A* fallback count should never be negative.");

        bool hasFloor = false;
        bool hasWall = false;
        for (const auto tile : first.map.data) {
            require(tile == config.mapWallValue || tile == config.mapFloorValue,
                    "Generated maps should contain only wall and floor values.");
            hasFloor |= tile == config.mapFloorValue;
            hasWall |= tile == config.mapWallValue;
        }
        require(hasFloor && hasWall, "A generated dungeon should contain floors and walls.");
    }

    void testNativeInteropCopiesGeneratedMap() {
        const auto config = dungeonConfig();
        const DungeonConfigInterop interop{
            config.mapWidth, config.mapHeight, config.roomWidth, config.roomHeight,
            config.roomSteps, config.roomBrushSize, config.wallHeightMinimum,
            config.wallWidthMinimum, config.roomTopPadding, config.roomBottomPadding,
            config.roomLeftPadding, config.roomRightPadding, config.roomTileBase,
            config.roomSeedIncrement, static_cast<int>(config.poissonMinimumDistance),
            config.poissonPoints, config.poissonMaximumAttempts, config.mstExtraEdges,
            config.corridorWidth, static_cast<uint8_t>(config.mapWallValue),
            static_cast<uint8_t>(config.mapFloorValue)
        };
        std::vector<uint8_t> output(
            static_cast<std::size_t>(config.mapWidth * config.mapHeight), 0);

        GenerateDungeonNative(&interop, 1234, output.data());

        require(output != std::vector<uint8_t>(output.size(), 0),
                "Native interop should copy generated data into the output buffer.");
        GenerateDungeonNative(nullptr, 1234, output.data());
        GenerateDungeonNative(&interop, 1234, nullptr);
    }
}

int main() {
    try {
        testDisjointSetUnitesComponents();
        testAStarFindsPathsAtMapEdges();
        testAStarAvoidsBlockedTilesAndReportsNoRoute();
        testAStarHandlesTrivialAndInvalidRequests();
        testIntegerPoissonPointsAreInBounds();
        testWallThicknessEnforcerFillsShortRuns();
        testDungeonGenerationIsDeterministicAndProducesMap();
        testNativeInteropCopiesGeneratedMap();
    } catch (const std::exception& error) {
        std::cerr << "CoreGenerationTests failed: " << error.what() << '\n';
        return 1;
    }
    return 0;
}
