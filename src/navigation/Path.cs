using System;
using System.Collections.Generic;
using Priority_Queue;

namespace Space.Navigation
{
    public class Path
    {
        private static readonly (int, int)[] ADJACENT_DIRECTIONS = new[] {
            (-1, 0), (1, 0), (0, -1), (0, 1)
        };

        private Queue<(int, int)> localPath = new();

        private readonly Queue<uint> highLevelPath;

        private readonly int endTileX, endTileY;

        private readonly ChunkGrid chunkGrid;

        public Path(ChunkGrid chunkGrid, Queue<uint> highLevelPath, int endTileX, int endTileY)
        {
            this.chunkGrid = chunkGrid;
            this.highLevelPath = highLevelPath;
            this.endTileX = endTileX;
            this.endTileY = endTileY;
        }

        public (int, int) GetNextTilePosition(int currentTileX, int currentTileY)
        {
            if (localPath.Count > 0 && localPath.Peek() == (currentTileX, currentTileY))
            {
                localPath.Dequeue();
            }

            if (localPath.Count == 0)
            {
                (int, int) targetTile;
                if (highLevelPath.Count > 0)
                {
                    var link = highLevelPath.Dequeue();
                    targetTile = GetTileFromLink(link);
                }
                else
                {
                    targetTile = (endTileX, endTileY);
                }

                LocalAStarSearch((currentTileX, currentTileY), targetTile);
            }

            return localPath.Peek();
        }

        private void LocalAStarSearch((int, int) startTile, (int, int) endTile)
        {
            int chunkX = endTile.Item1 / chunkGrid.chunkSizeX;
            int chunkY = endTile.Item2 / chunkGrid.chunkSizeY;

            var maxNodes = chunkGrid.xChunks * chunkGrid.yChunks;
            var openList = new FastPriorityQueue<TileNode>(maxNodes);
            openList.Enqueue(new(startTile), 0f);

            var cameFrom = new Dictionary<(int, int), (int, int)>();

            var costs = new Dictionary<(int, int), float>();
            costs[startTile] = 0f;

            while (openList.Count > 0)
            {
                var current = openList.Dequeue();
                var currentTile = current.coords;
                if (currentTile == endTile)
                {
                    ReconstructLocalPath(cameFrom, endTile);
                    return;
                }

                foreach (var (directionX, directionY) in ADJACENT_DIRECTIONS)
                {
                    // TODO: skip tiles not surrounded by enough tiles to fit the agent size
                    var otherTile = (currentTile.Item1 + directionX, currentTile.Item2 + directionY);

                    if (!WithinChunk(chunkX, chunkY, otherTile)) continue;

                    var cost = costs[currentTile] + 1;
                    if (!costs.ContainsKey(otherTile) || cost < costs[otherTile])
                    {
                        costs[otherTile] = cost;
                        cameFrom[otherTile] = currentTile;
                        float priority = cost + LocalHeuristic(otherTile, endTile);
                        openList.Enqueue(new(otherTile), priority);
                    }
                }
            }

            throw new Exception($"Could not find path from {startTile} to {endTile}.");
        }

        private bool WithinChunk(int chunkX, int chunkY, (int, int) coordinates)
        {
            int otherChunkX = coordinates.Item1 / chunkGrid.chunkSizeX;
            int otherChunkY = coordinates.Item2 / chunkGrid.chunkSizeY;
            return chunkX == otherChunkX && chunkY == otherChunkY;
        }

        private float LocalHeuristic((int, int) fromTile, (int, int) endTile)
        {
            float dx = endTile.Item1 - fromTile.Item1;
            float dy = endTile.Item2 - fromTile.Item2;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        private void ReconstructLocalPath(Dictionary<(int, int), (int, int)> cameFrom, (int, int) endTile)
        {
            localPath.Clear();
            var current = endTile;
            while (!cameFrom.ContainsKey(current))
            {
                var fromTile = cameFrom[current];
                localPath.Enqueue(fromTile);
                current = fromTile;
            }
        }

        private (int, int) GetTileFromLink(uint link)
        {
            var linkData = new LinkData(link);
            if (linkData.right)
            {
                return ((int)linkData.x + 1, (int)(linkData.y + linkData.size / 2));
            }
            else
            {
                return ((int)(linkData.x + linkData.size / 2), (int)linkData.y + 1);
            }
        }

        private class TileNode : FastPriorityQueueNode
        {
            public readonly (int, int) coords;

            public TileNode((int, int) coords)
            {
                this.coords = coords;
            }
        }
    }
}