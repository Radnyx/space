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

        public Path? FindPath(int startTileX, int startTileY, int endTileX, int endTileY)
        {
            var startRegion = chunkGrid.GetRegionAt(startTileX, startTileY);
            var endRegion = chunkGrid.GetRegionAt(endTileX, endTileY);

            if (startRegion == null || endRegion == null || startRegion.room != endRegion.room)
            {
                return null;
            }

            return new Path(
                chunkGrid,
                HighLevelAStarSearch(startRegion, endRegion, startTileX, startTileY, endTileX, endTileY),
                endTileX,
                endTileY
            );
        }

        private Stack<(int, int)> HighLevelAStarSearch(
            Region startRegion, Region endRegion,
            int startTileX, int startTileY, int endTileX, int endTileY
        )
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
                    return ReconstructHighLevelPath(cameFrom, endRegion, startTileX, startTileY, endTileX, endTileY);
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

        private Stack<(int, int)> ReconstructHighLevelPath(
            Dictionary<Region, uint> cameFrom, Region endRegion,
            int startTileX, int startTileY, int endTileX, int endTileY
        )
        {
            var lastCoords = (endTileX, endTileY);

            var path = new Stack<(int, int)>();
            var current = endRegion;
            while (cameFrom.ContainsKey(current))
            {
                var link = cameFrom[current];
                var from = chunkGrid.GetOtherRegionFromLink(link, current);

                (int, int) coords;
                var linkData = new LinkData(link);
                if (linkData.right)
                {
                    coords = GetRightLinkPoint(linkData, startTileX, startTileY, lastCoords);

                    if (from.chunkX < current.chunkX)
                    {
                        coords.Item1 += 1;
                    }
                }
                else
                {
                    coords = GetBottomLinkPoint(linkData, startTileX, startTileY, lastCoords);

                    if (from.chunkY < current.chunkY)
                    {
                        coords.Item2 += 1;
                    }
                }

                path.Push(coords);
                current = from;

                lastCoords = coords;
            }
            return path;
        }

        /*
            Chooses a point along the link as our target. 
            Shoots a ray from a high level target point to the starting position. Wherever the ray intersects
            the previous link is that link's target. If the ray does not intersect the previous link, choose the 
            nearest extremity of the link along its axis. If the ray to the starting point is facing the other direction, 
            just pick the midpoint of the previous link.
        */
        private (int, int) GetRightLinkPoint(LinkData linkData, int startTileX, int startTileY, (int, int) lastCoords)
        {
            var (lastX, lastY) = lastCoords;
            float dx = startTileX - lastX;
            float dy = startTileY - lastY;

            float t = (linkData.x - lastX) / dx;
            if (t < 0)
            {
                return ((int)linkData.x, (int)(linkData.y + linkData.size / 2));
            }

            int yIntercept = Math.Clamp((int)Math.Round(lastY + t * dy), (int)linkData.y, (int)(linkData.y + linkData.size) - 1);

            return ((int)linkData.x, yIntercept);
        }

        private (int, int) GetBottomLinkPoint(LinkData linkData, int startTileX, int startTileY, (int, int) lastCoords)
        {
            var (lastX, lastY) = lastCoords;
            float dx = startTileX - lastX;
            float dy = startTileY - lastY;

            float t = (linkData.y - lastY) / dy;
            if (t < 0)
            {
                return ((int)(linkData.x + linkData.size / 2), (int)linkData.y);
            }

            int xIntercept = Math.Clamp((int)Math.Round(lastX + t * dx), (int)linkData.x, (int)(linkData.x + linkData.size) - 1);

            return (xIntercept, (int)linkData.y);
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