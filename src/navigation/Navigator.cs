using System;
using System.Collections.Generic;
using Godot;
using Priority_Queue;

namespace Space.Navigation
{
    public class Navigator
    {
        private readonly ChunkGrid chunkGrid;

        public Navigator(ChunkGrid chunkGrid)
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
                chunkGrid,
                HighLevelAStarSearch(startRegion, endRegion),
                endX,
                endY
            );
        }

        private Queue<uint> HighLevelAStarSearch(Region startRegion, Region endRegion)
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
                    return ReconstructHighLevelPath(cameFrom, endRegion);
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

        private Queue<uint> ReconstructHighLevelPath(Dictionary<Region, uint> cameFrom, Region endRegion)
        {
            var path = new Queue<uint>();
            var current = endRegion;
            while (!cameFrom.ContainsKey(current))
            {
                var link = cameFrom[current];
                path.Enqueue(link);
                current = chunkGrid.GetOtherRegionFromLink(link, current);
            }
            return path;
        }

        private class RegionNode : FastPriorityQueueNode
        {
            public readonly Region region;

            public RegionNode(Region region)
            {
                this.region = region;
            }
        }
    }
}