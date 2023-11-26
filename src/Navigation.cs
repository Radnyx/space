using System;
using System.Collections.Generic;
using Godot;
using Priority_Queue;

namespace Space
{
    public class Navigation
    {
        public class Path
        {
            public readonly List<uint> highLevelPath;

            public readonly int endX, endY;

            public Path(List<uint> highLevelPath, int endX, int endY)
            {
                this.highLevelPath = highLevelPath;
                this.endX = endX;
                this.endY = endY;
            }

            public (int, int) GetNextTilePosition(int currentTileX, int currentTileY)
            {


                return (-1, -1);
            }
        }

        private static readonly (int, int)[] ADJACENT_DIRECTIONS = new[] {
            (-1, 0), (1, 0), (0, -1), (0, 1)
        };

        private readonly ChunkGrid chunkGrid;

        public Navigation(ChunkGrid chunkGrid)
        {
            this.chunkGrid = chunkGrid;
        }

        public Path? FindPath(int startX, int startY, int endX, int endY)
        {
            var startRegion = chunkGrid.GetRegionAt(startX, startY);
            var endRegion = chunkGrid.GetRegionAt(endX, endY);

            if (startRegion == null || endRegion == null || startRegion.room != endRegion.room)
            {
                return null;
            }

            return new Path(
                HighLevelAStarSearch(startRegion, endRegion),
                endX,
                endY
            );
        }

        private List<uint> HighLevelAStarSearch(Region startRegion, Region endRegion)
        {
            var maxNodes = chunkGrid.xChunks * chunkGrid.yChunks;
            var openList = new FastPriorityQueue<RegionNode>(maxNodes);
            openList.Enqueue(new(startRegion), 0f);

            // link a given region came from
            var cameFrom = new Dictionary<Region, uint>();

            var costs = new Dictionary<Region, float>();
            costs[startRegion] = 0f;

            while (openList.Count > 0)
            {
                var current = openList.Dequeue();
                var currentRegion = current.region;
                if (currentRegion == endRegion)
                {
                    return HighLevelReconstructPath(cameFrom, endRegion);
                }

                var links = current.region.links;
                foreach (var link in links)
                {
                    // TODO: skip links with size < size of the agent
                    var otherRegion = chunkGrid.GetOtherRegionFromLink(link, currentRegion);

                    var cost = costs[currentRegion] + 1;
                    if (!costs.ContainsKey(otherRegion) || cost < costs[otherRegion])
                    {
                        costs[otherRegion] = cost;
                        cameFrom[otherRegion] = link;
                        float priority = cost + HighLevelHeuristic(otherRegion, endRegion);
                        openList.Enqueue(new(otherRegion), priority);
                    }
                }
            }

            throw new Exception($"Could not find path from {startRegion} to {endRegion}.");
        }

        private float HighLevelHeuristic(Region fromRegion, Region endRegion)
        {
            float dx = endRegion.chunkX - fromRegion.chunkX;
            float dy = endRegion.chunkY - fromRegion.chunkY;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private List<uint> HighLevelReconstructPath(Dictionary<Region, uint> cameFrom, Region endRegion)
        {
            var path = new List<uint>();
            var current = endRegion;
            while (!cameFrom.ContainsKey(current))
            {
                var link = cameFrom[current];
                path.Add(link);
                current = chunkGrid.GetOtherRegionFromLink(link, current);
            }
            return path;
        }

        private List<(int, int)> LocalAStarSearch(uint startLinkHash, uint endLinkHash)
        {
            var startTile = GetTileFromLink(startLinkHash);
            var endTile = GetTileFromLink(endLinkHash);

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
                    return LocalReconstructPath(cameFrom, endTile);
                }

                foreach (var (directionX, directionY) in ADJACENT_DIRECTIONS)
                {
                    // TODO: skip tiles not surrounded by enough tiles to fit the agent size
                    var otherTile = (currentTile.Item1 + directionX, currentTile.Item2 + directionY);

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

            throw new Exception($"Could not find path from {LinkUtils.ToString(startLinkHash)} to {LinkUtils.ToString(endLinkHash)}.");
        }

        private float LocalHeuristic((int, int) fromTile, (int, int) endTile)
        {
            float dx = endTile.Item1 - fromTile.Item1;
            float dy = endTile.Item2 - fromTile.Item2;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private List<(int, int)> LocalReconstructPath(Dictionary<(int, int), (int, int)> cameFrom, (int, int) endTile)
        {
            var path = new List<(int, int)>();
            var current = endTile;
            while (!cameFrom.ContainsKey(current))
            {
                var fromTile = cameFrom[current];
                path.Add(fromTile);
                current = fromTile;
            }
            return path;
        }

        private (int, int) GetTileFromLink(uint link)
        {
            var linkData = new LinkData(link);
            if (linkData.right)
            {
                return ((int)linkData.x, (int)(linkData.y + linkData.size / 2));
            }
            else
            {
                return ((int)(linkData.x + linkData.size / 2), (int)linkData.y);
            }
        }

        private class RegionNode : FastPriorityQueueNode
        {
            public readonly Region region;

            public RegionNode(Region region)
            {
                this.region = region;
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