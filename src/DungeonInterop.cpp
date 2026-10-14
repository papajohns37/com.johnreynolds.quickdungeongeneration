#include "DungeonGenerator.h"
#include "PoissonDiskSampler.h"

#include <cstdint>
#include <cstring>

#if defined(_WIN32)
    #define EXPORT_API __declspec(dllexport)
#else
    #define EXPORT_API __attribute__((visibility("default")))
#endif

extern "C" {
    struct DungeonConfigInterop {
        int mapWidth, mapHeight, roomWidth, roomHeight;
        int roomSteps, roomBrushSize, wallHeightMinimum, wallWidthMinimum;
        int roomTopPadding, roomBottomPadding, roomLeftPadding, roomRightPadding;
        int roomTileBase, roomSeedIncrement, poissonMinimumDistance;
        int poissonPoints, poissonMaximumAttempts, mstExtraEdges, corridorWidth;
        uint8_t mapWallValue, mapFloorValue;
    };

    void generateDungeonNative(
        const DungeonConfigInterop* config,
        int seed,
        uint8_t* outMapData,
        int32_t* outRoomData) {
        if (!config || !outMapData) return;

        try {
            // Explicit named assignment prevents parameter shifting bugs entirely
            DungeonGeneratorConfig internalConfig;
            internalConfig.mapWidth = config->mapWidth;
            internalConfig.mapHeight = config->mapHeight;
            internalConfig.mapWallValue = config->mapWallValue;
            internalConfig.mapFloorValue = config->mapFloorValue;
            internalConfig.roomWidth = config->roomWidth;
            internalConfig.roomHeight = config->roomHeight;
            internalConfig.roomTopPadding = config->roomTopPadding;
            internalConfig.roomBottomPadding = config->roomBottomPadding;
            internalConfig.roomLeftPadding = config->roomLeftPadding;
            internalConfig.roomRightPadding = config->roomRightPadding;
            internalConfig.roomBrushSize = config->roomBrushSize;
            internalConfig.roomSteps = config->roomSteps;
            internalConfig.wallHeightMinimum = config->wallHeightMinimum;
            internalConfig.wallWidthMinimum = config->wallWidthMinimum;
            internalConfig.roomTileBase = config->roomTileBase;
            internalConfig.roomSeedIncrement = config->roomSeedIncrement;
            internalConfig.corridorWidth = config->corridorWidth;
            internalConfig.mstExtraEdges = config->mstExtraEdges;
            internalConfig.poissonMinimumDistance = static_cast<float>(config->poissonMinimumDistance);
            internalConfig.poissonPoints = config->poissonPoints;
            internalConfig.poissonMaximumAttempts = config->poissonMaximumAttempts;

            DungeonGenerator generator;
            DungeonGenerationResult result = generator.generate(internalConfig, seed);

            int totalSize = config->mapWidth * config->mapHeight;
            std::memcpy(outMapData, result.map.data.data(), totalSize);
            if (outRoomData) {
                std::memcpy(
                    outRoomData,
                    result.roomIds.data.data(),
                    totalSize * sizeof(int32_t));
            }
        }
        catch (const std::exception& e) {
            fprintf(stderr, "C++ EXCEPTION CAUGHT: %s\n", e.what());
            fflush(stderr);
        }
    }

    EXPORT_API void GenerateDungeonNative(
        const DungeonConfigInterop* config,
        int seed,
        uint8_t* outMapData) {
        generateDungeonNative(config, seed, outMapData, nullptr);
    }

    EXPORT_API void GenerateDungeonNativeWithRooms(
        const DungeonConfigInterop* config,
        int seed,
        uint8_t* outMapData,
        int32_t* outRoomData) {
        generateDungeonNative(config, seed, outMapData, outRoomData);
    }

    EXPORT_API int GeneratePoissonPointsNative(
        int width,
        int height,
        int minimumDistance,
        int pointCount,
        int maximumAttempts,
        int seed,
        int32_t* outPoints) {
        if (!outPoints || width <= 0 || height <= 0 ||
            minimumDistance <= 0 || pointCount <= 0 || maximumAttempts <= 0) {
            return 0;
        }

        try {
            PoissonDiskSampler sampler;
            const auto points = sampler.generateIntegerPoints(
                static_cast<float>(width),
                static_cast<float>(height),
                static_cast<float>(minimumDistance),
                pointCount,
                maximumAttempts,
                seed);
            const int count = static_cast<int>(points.size());
            for (int i = 0; i < count; ++i) {
                outPoints[i * 2] = points[i].x;
                outPoints[i * 2 + 1] = points[i].y;
            }
            return count;
        }
        catch (const std::exception& e) {
            fprintf(stderr, "C++ POISSON EXCEPTION CAUGHT: %s\n", e.what());
            fflush(stderr);
            return 0;
        }
    }
}