#include "DungeonGenerator.h"

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

    EXPORT_API void GenerateDungeonNative(const DungeonConfigInterop* config, int seed, uint8_t* outMapData) {
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
        }
        catch (const std::exception& e) {
            fprintf(stderr, "C++ EXCEPTION CAUGHT: %s\n", e.what());
            fflush(stderr);
        }
    }
}