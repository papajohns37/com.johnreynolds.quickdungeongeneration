#include "AStarPathfinder.h"

#include "AStarTile.h"
#include <functional>
#include <limits>
#include <queue>

namespace {
    bool tileIsValid(const Grid<uint8_t>& map, int width, std::unordered_set<int> validPathTiles, Vector2Int tile);
    float distance(Vector2Int v1, Vector2Int v2);
}

std::vector<Vector2Int> AStarPathfinder::FindPath(const Grid<uint8_t>& map, Vector2Int start, Vector2Int end, int checkWidth, int drawWidth, std::unordered_set<int> validPathTiles){
    if (start.x < 0 || start.y < 0 || end.x < 0 || end.y < 0 ||
        start.x >= map.width || start.y >= map.height ||
        end.x >= map.width || end.y >= map.height) {
        return {};
    }

    Grid<AStarTile> astarMap(map.width, map.height);
    for (int x = 0; x < map.width; x++)
    {
        for (int y = 0; y < map.height; y++)
        {
            astarMap(x, y) = AStarTile{
                Vector2Int{x, y},
                nullptr,
                tileIsValid(map, checkWidth, validPathTiles, Vector2Int{x, y}),
                std::numeric_limits<float>::infinity(),
                distance(Vector2Int{x, y}, end),
                std::numeric_limits<float>::infinity()
            };
        }
    }
    if (!astarMap(start.x, start.y).valid || !astarMap(end.x, end.y).valid) {
        return {};
    }
    astarMap(start.x, start.y).distanceFromStart = 0.0f;
    astarMap(start.x, start.y).totalHeuristicCost = distance(start, end);

    if (start.x == end.x && start.y == end.y) {
        return {start};
    }

    std::priority_queue<AStarTile, std::vector<AStarTile>, std::greater<AStarTile>> queue;
    queue.push(astarMap(start.x, start.y));
    while (!queue.empty()){
        AStarTile current = queue.top();
        queue.pop();

        const Vector2Int directions[] = {
            {-1, 0}, {1, 0}, {0, -1}, {0, 1}
        };
        for (const auto& direction : directions) {
            const int x = current.position.x + direction.x;
            const int y = current.position.y + direction.y;
            if (x < 0 || x >= map.width || y < 0 || y >= map.height) {
                continue;
            }
            AStarTile& adjacent = astarMap(x, y);
            if (!adjacent.valid) {
                continue;
            }

            float distanceFromStart = current.distanceFromStart + 1;
            if (distanceFromStart < adjacent.distanceFromStart){
                adjacent.distanceFromStart = distanceFromStart;
                adjacent.totalHeuristicCost = distanceFromStart + adjacent.distanceToEnd;
                adjacent.prev = &astarMap(current.position.x, current.position.y);
                queue.push(adjacent);
            }
        }
    }

    if (!astarMap(end.x, end.y).prev){
        return std::vector<Vector2Int>{};
    }

    std::unordered_set<int> visitedTiles;
    std::vector<Vector2Int> result;
    AStarTile* current = &astarMap(end.x, end.y);

    while (current != nullptr)
    {
        Vector2Int currentPosition = current->position;
        for (int dx = -(drawWidth / 2); dx < (drawWidth + 1) / 2; ++dx) {
            for (int dy = -(drawWidth / 2); dy < (drawWidth + 1) / 2; ++dy) {
                int nx = currentPosition.x + dx;
                int ny = currentPosition.y + dy;

                int tileIndex = ny * map.width + nx;
                    
                // insert().second is true only if the element was newly inserted
                if (visitedTiles.insert(tileIndex).second) {
                    result.push_back(Vector2Int{nx, ny});
                }
            }
        }
        current = current->prev;
    }

    return result;
}

namespace {
    bool tileIsValid(const Grid<uint8_t>& map, int width, std::unordered_set<int> validPathTiles, Vector2Int tile){
        for (int dx = -(width / 2); dx < (width + 1) / 2; ++dx) {
            for (int dy = -(width / 2); dy < (width + 1) / 2; ++dy) {
                Vector2Int current{tile.x + dx, tile.y + dy};
                if (current.x < 0 || current.x >= map.width || current.y < 0 || current.y >= map.height){
                    return false;
                }
                if (validPathTiles.find(map(current.x, current.y)) != validPathTiles.end()){
                    return false;
                }
            }
        }

        return true;
    }
    float distance(Vector2Int v1, Vector2Int v2) {
        float dx = v1.x - v2.x;
        float dy = v1.y - v2.y;
        return std::sqrt(dx * dx + dy * dy);
    }
}