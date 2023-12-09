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

        public readonly Stack<(int, int)> localPath;

        public readonly Stack<(int, int)> highLevelPath;

        public readonly int endTileX, endTileY;

        private readonly IChunkGrid chunkGrid;

        public Path(IChunkGrid chunkGrid, Stack<(int, int)> highLevelPath, int endTileX, int endTileY)
        {
            this.chunkGrid = chunkGrid;
            this.highLevelPath = highLevelPath;
            this.endTileX = endTileX;
            this.endTileY = endTileY;

            localPath = new(chunkGrid.chunkSizeX + chunkGrid.chunkSizeY);
        }

        public (int, int)? GetNextTilePosition(int currentTileX, int currentTileY)
        {
            if (localPath.Count > 0 && localPath.Peek() == (currentTileX, currentTileY))
            {
                localPath.Pop();
            }

            if (localPath.Count == 0)
            {
                (int, int) targetTile;
                if (highLevelPath.Count > 0)
                {
                    targetTile = highLevelPath.Pop();
                }
                else
                {
                    targetTile = (endTileX, endTileY);
                }

                LocalAStarSearch((currentTileX, currentTileY), targetTile);
            }

            if (localPath.Count == 0)
            {
                return null;
            }

            return localPath.Peek();
        }

        private void LocalAStarSearch((int, int) startTile, (int, int) endTile)
        {
            if (IsDisconnected(startTile, endTile))
            {
                return;
            }

            int startChunkX = startTile.Item1 / chunkGrid.chunkSizeX;
            int startChunkY = startTile.Item2 / chunkGrid.chunkSizeY;
            int endChunkX = endTile.Item1 / chunkGrid.chunkSizeX;
            int endChunkY = endTile.Item2 / chunkGrid.chunkSizeY;

            var maxNodes = 2 * chunkGrid.chunkSizeX * chunkGrid.chunkSizeY;
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

                    if (
                        !chunkGrid.IsNavigableAt(otherTile.Item1, otherTile.Item2) ||
                        (!WithinChunk(startChunkX, startChunkY, otherTile) && !WithinChunk(endChunkX, endChunkY, otherTile))
                    )
                    {
                        continue;
                    }

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
            while (cameFrom.ContainsKey(current))
            {
                localPath.Push(current);

                var fromTile = cameFrom[current];
                current = fromTile;
            }
        }

        private bool IsDisconnected((int, int) startTile, (int, int) endTile)
        {
            var region1 = chunkGrid.GetRegionAt(startTile.Item1, startTile.Item2);
            var region2 = chunkGrid.GetRegionAt(endTile.Item1, endTile.Item2);
            return (
                region1 == null ||
                region2 == null ||
                (region1 != region2 && !chunkGrid.AreRegionsConnected(region1, region2))
            );
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