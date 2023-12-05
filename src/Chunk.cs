using System.Collections.Generic;

namespace Space
{
    public class Chunk<K> : IChunk where K : notnull
    {
        private struct FloodFillPointer
        {
            public int x1, x2, y, dy;

            public FloodFillPointer(int x1, int x2, int y, int dy)
            {
                this.x1 = x1;
                this.x2 = x2;
                this.y = y;
                this.dy = dy;
            }
        }

        public readonly int topLeftX, topLeftY;

        public readonly IRegion?[,] regionTiles;

        public List<IRegion> regions { private set; get; }

        private readonly ITileMap tileMap;
        private readonly Dictionary<uint, LinkPair> linkCache;

        private readonly int width, height;

        public Chunk(
            ITileMap tileMap, Dictionary<uint, LinkPair> linkCache,
            int topLeftX, int topLeftY, int width, int height
            )
        {
            this.tileMap = tileMap;
            this.linkCache = linkCache;
            this.topLeftX = topLeftX;
            this.topLeftY = topLeftY;
            this.width = width;
            this.height = height;

            regionTiles = new Region<K>?[width, height];
            regions = new(width);

            RecalculateRegions();
        }

        /// <summary>
        /// Replaces one region with another. 
        /// <br/><br/>
        /// All tiles belonging to the <c>original</c> region will now belong to the <c>other</c>, 
        /// destroying the original region and adding the original's size to the other's room.
        /// </summary>
        public void ReplaceRegion(IRegion original, IRegion other)
        {
            if (original == other) return;

            other.AddSize(original.size);

            original.Destroy();
            regions.Remove(original);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (regionTiles[x, y] == original)
                    {
                        regionTiles[x, y] = other;
                    }
                }
            }
        }

        public void RecalculateLinksRight(Chunk<K> other)
        {
            int startLinkY = 0;
            uint currentLinkSize = 0;
            IRegion? lastThisRegion = null;
            IRegion? lastOtherRegion = null;
            for (int y = 0; y < height; y++)
            {
                var thisRegion = regionTiles[width - 1, y];
                var otherRegion = other.regionTiles[0, y];

                var continuing = thisRegion == lastThisRegion && otherRegion == lastOtherRegion;

                if (!continuing && currentLinkSize > 0)
                {
                    AddLink(
                        lastThisRegion!, lastOtherRegion!,
                        topLeftX + width - 1, topLeftY + startLinkY, currentLinkSize, true
                    );

                    // reset
                    currentLinkSize = 0;
                }

                // we found two adjacent regions
                if (thisRegion != null && otherRegion != null)
                {
                    // we are continuing the current link
                    if (continuing)
                    {
                        currentLinkSize++;
                    }
                    // we are ready to start a new link
                    else
                    {
                        startLinkY = y;
                        currentLinkSize = 1;
                    }
                }

                lastThisRegion = thisRegion;
                lastOtherRegion = otherRegion;
            }

            // we finished the loop while working on a link
            if (currentLinkSize > 0)
            {
                AddLink(
                    lastThisRegion!, lastOtherRegion!,
                    topLeftX + width - 1, topLeftY + startLinkY, currentLinkSize, true
                );
            }
        }

        public void RecalculateLinksDown(Chunk<K> other)
        {
            int startLinkX = 0;
            uint currentLinkSize = 0;
            IRegion? lastThisRegion = null;
            IRegion? lastOtherRegion = null;
            for (int x = 0; x < width; x++)
            {
                var thisRegion = regionTiles[x, height - 1];
                var otherRegion = other.regionTiles[x, 0];

                var continuing = thisRegion == lastThisRegion && otherRegion == lastOtherRegion;

                if (!continuing && currentLinkSize > 0)
                {

                    AddLink(
                        lastThisRegion!, lastOtherRegion!,
                        topLeftX + startLinkX, topLeftY + height - 1, currentLinkSize, false
                    );

                    // reset
                    currentLinkSize = 0;
                }

                // we found two adjacent regions
                if (thisRegion != null && otherRegion != null)
                {
                    // we are continuing the current link
                    if (continuing)
                    {
                        currentLinkSize++;
                    }
                    // we are ready to start a new link
                    else
                    {
                        startLinkX = x;
                        currentLinkSize = 1;
                    }
                }

                lastThisRegion = thisRegion;
                lastOtherRegion = otherRegion;
            }

            // we finished the loop while working on a link
            if (currentLinkSize > 0)
            {
                AddLink(
                    lastThisRegion!, lastOtherRegion!,
                    topLeftX + startLinkX, topLeftY + height - 1, currentLinkSize, false
                );
            }
        }

        private void AddLink(IRegion r1, IRegion r2, int x, int y, uint size, bool direction)
        {
            var link = LinkUtils.Hash((uint)x, (uint)y, size, direction);

            if (linkCache.ContainsKey(link))
            {
                // remove any dangling link references
                linkCache[link].r1!.links.Remove(link);
                linkCache[link].r2!.links.Remove(link);
            }

            linkCache[link] = new LinkPair(r1, r2);

            r1.links.Add(link);
            r2.links.Add(link);
        }

        /// <summary>
        /// Finds distinct regions in the chunk separated by non-navigable tiles.
        /// Creates new region and room objects and classifies all non-navigable 
        /// tiles by their region.
        /// </summary>
        public void RecalculateRegions()
        {
            foreach (var region in regions)
            {
                region.ResetLinks(linkCache);
                region.Destroy();
            }

            regions.Clear();

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    regionTiles[x, y] = null;
                }
            }

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    FloodFill(x, y);
                }
            }
        }

        public IRegion CreateNewRegion(int x, int y)
        {
            var region = new Region<K>(GetChunkX(), GetChunkY());
            regions.Add(region);
            region.IncrementSize();
            regionTiles[x, y] = region;
            return region;
        }

        public HashSet<IRegion> GetRegionsAdjacentTo(int x, int y)
        {
            HashSet<IRegion> regions = new(4);

            if (x > 0)
            {
                var region = regionTiles[x - 1, y];
                if (region != null) regions.Add(region);
            }

            if (y > 0)
            {
                var region = regionTiles[x, y - 1];
                if (region != null) regions.Add(region);
            }

            if (x < width - 1)
            {
                var region = regionTiles[x + 1, y];
                if (region != null) regions.Add(region);
            }

            if (y < height - 1)
            {
                var region = regionTiles[x, y + 1];
                if (region != null) regions.Add(region);
            }

            return regions;
        }

        private void FloodFill(int startX, int startY)
        {
            if (!FloodFillAvailable(startX, startY))
            {
                return;
            }

            IRegion? region = null;

            Stack<FloodFillPointer> s = new(width);
            s.Push(new FloodFillPointer(startX, startX, startY, 1));
            s.Push(new FloodFillPointer(startX, startX, startY - 1, -1));

            while (s.Count > 0)
            {
                var pointer = s.Pop();
                var x = pointer.x1;
                if (FloodFillAvailable(x, pointer.y))
                {
                    while (FloodFillAvailable(x - 1, pointer.y))
                    {
                        if (region == null)
                        {
                            region = new Region<K>(GetChunkX(), GetChunkY());
                            regions.Add(region);
                        }

                        region.IncrementSize();
                        regionTiles[x - 1, pointer.y] = region;
                        x--;
                    }

                    if (x < pointer.x1)
                    {
                        s.Push(new FloodFillPointer(x, pointer.x1 - 1, pointer.y - pointer.dy, -pointer.dy));
                    }
                }

                while (pointer.x1 <= pointer.x2)
                {
                    while (FloodFillAvailable(pointer.x1, pointer.y))
                    {
                        if (region == null)
                        {
                            region = new Region<K>(GetChunkX(), GetChunkY());
                            regions.Add(region);
                        }

                        region.IncrementSize();
                        regionTiles[pointer.x1, pointer.y] = region;
                        pointer.x1++;
                    }
                    if (pointer.x1 > x)
                    {
                        s.Push(new FloodFillPointer(x, pointer.x1 - 1, pointer.y + pointer.dy, pointer.dy));
                    }
                    if (pointer.x1 - 1 > pointer.x2)
                    {
                        s.Push(new FloodFillPointer(pointer.x2 + 1, pointer.x1 - 1, pointer.y - pointer.dy, -pointer.dy));
                    }
                    pointer.x1++;
                    while (pointer.x1 < pointer.x2 && !FloodFillAvailable(pointer.x1, pointer.y))
                    {
                        pointer.x1++;
                    }
                    x = pointer.x1;
                }
            }
        }

        private bool FloodFillAvailable(int x, int y)
        {
            return
                x >= 0 && x < width && y >= 0 && y < height &&
                !tileMap.IsOutOfBounds(topLeftX + x, topLeftY + y) &&
                regionTiles[x, y] == null;
        }

        private int GetChunkX()
        {
            return topLeftX / width;
        }

        private int GetChunkY()
        {
            return topLeftY / height;
        }
    }
}