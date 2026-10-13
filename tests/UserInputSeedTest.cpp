#include "DungeonGenerator.h"

#include <iostream>
#include <windows.h>

int main() {
    SetConsoleOutputCP(CP_UTF8);
    SetConsoleCP(CP_UTF8);

    int seed;
    std::cout << "Enter a seed: ";
    std::cin >> seed;

    const DungeonGeneratorConfig config{
        2000,    // mapWidth
        2000,    // mapHeight
        1,      // mapWallValue
        2,      // mapFloorValue
        25,    // roomWidth
        25,    // roomHeight
        2,      // roomTopPadding
        2,      // roomBottomPadding
        2,      // roomLeftPadding
        2,      // roomRightPadding
        1,      // roomBrushSize
        500,   // roomSteps
        1,      // wallHeightMinimum
        1,      // wallWidthMinimum
        10,     // roomTileBase
        1,      // roomSeedIncrement
        2,      // corridorWidth
        1,      // mstExtraEdges
        25.0f, // poissonMinimumDistance
        100,     // poissonPoints
        30      // poissonMaximumAttempts
    };

    const DungeonGenerationResult result = DungeonGenerator{}.generate(config, seed);
    
    for (int y = 0; y < config.mapHeight; ++y) {
        for (int x = 0; x < config.mapWidth; ++x) {
            std::cout << (result.map(x, y) == config.mapWallValue ? "██" : "  ");
        }
        std::cout << '\n';
    }
}
