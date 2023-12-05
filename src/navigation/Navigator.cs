using System;
using System.Collections.Generic;
using Priority_Queue;

namespace Space.Navigation
{
    public class Navigator
    {
        private readonly IChunkGrid chunkGrid;

        public Navigator(IChunkGrid chunkGrid)
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
            IRegion startRegion, IRegion endRegion,
            int startTileX, int startTileY, int endTileX, int endTileY
        )
        {
            var maxNodes = chunkGrid.xChunks * chunkGrid.yChunks;
            var openList = new FastPriorityQueue<RegionNode>(maxNodes);
            openList.Enqueue(new(startRegion), 0f);

            // link a given region came from
            var cameFrom = new Dictionary<IRegion, uint>();

            var costs = new Dictionary<IRegion, float>();
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

        private float HighLevelHeuristic(IRegion fromRegion, IRegion endRegion)
        {
            float dx = endRegion.chunkX - fromRegion.chunkX;
            float dy = endRegion.chunkY - fromRegion.chunkY;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        private Stack<(int, int)> ReconstructHighLevelPath(
            Dictionary<IRegion, uint> cameFrom, IRegion endRegion,
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

                    coords = EnsureRightTilePositionIsNavigable(linkData, coords);
                }
                else
                {
                    coords = GetBottomLinkPoint(linkData, startTileX, startTileY, lastCoords);

                    if (from.chunkY < current.chunkY)
                    {
                        coords.Item2 += 1;
                    }

                    coords = EnsureBottomTilePositionIsNavigable(linkData, coords);
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

        private (int, int) EnsureRightTilePositionIsNavigable(LinkData linkData, (int, int) coords)
        {
            // Player has placed a non-navigable tile here that belongs to a room?
            if (!chunkGrid.IsNavigableAt(coords.Item1, coords.Item2))
            {
                // Pick closest point along the link.
                for (int y = coords.Item2 + 1; y < linkData.y + linkData.size; y++)
                {
                    if (chunkGrid.IsNavigableAt(coords.Item1, y))
                    {
                        coords.Item2 = y;
                        return coords;
                    }
                }

                for (int y = coords.Item2 - 1; y >= linkData.y; y--)
                {
                    if (chunkGrid.IsNavigableAt(coords.Item1, y))
                    {
                        coords.Item2 = y;
                        return coords;
                    }
                }
            }

            return coords;
        }

        private (int, int) EnsureBottomTilePositionIsNavigable(LinkData linkData, (int, int) coords)
        {
            // Player has placed a non-navigable tile here that belongs to a room?
            if (!chunkGrid.IsNavigableAt(coords.Item1, coords.Item2))
            {
                // Pick closest point along the link.
                if (!chunkGrid.IsNavigableAt(coords.Item1, coords.Item2))
                {
                    for (int x = coords.Item1 + 1; x < linkData.x + linkData.size; x++)
                    {
                        if (chunkGrid.IsNavigableAt(x, coords.Item2))
                        {
                            coords.Item1 = x;
                            break;
                        }
                    }
                }

                if (!chunkGrid.IsNavigableAt(coords.Item1, coords.Item2))
                {
                    for (int x = coords.Item1 - 1; x >= linkData.x; x--)
                    {
                        if (chunkGrid.IsNavigableAt(x, coords.Item2))
                        {
                            coords.Item1 = x;
                            break;
                        }
                    }
                }
            }

            return coords;
        }

        private class RegionNode : FastPriorityQueueNode
        {
            public readonly IRegion region;

            public RegionNode(IRegion region)
            {
                this.region = region;
            }
        }
    }
}