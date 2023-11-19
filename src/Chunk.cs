namespace Space
{
    public class Chunk
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

        public readonly Region?[,] regionTiles;

        public List<Region> regions { private set; get; }

        private readonly ITileMap tileMap;
        private readonly LinkCache linkCache;

        private readonly int width, height;

        public Chunk(ITileMap tileMap, LinkCache linkCache, int topLeftX, int topLeftY, int width, int height)
        {
            this.tileMap = tileMap;
            this.linkCache = linkCache;
            this.topLeftX = topLeftX;
            this.topLeftY = topLeftY;
            this.width = width;
            this.height = height;

            regionTiles = new Region?[width, height];
            regions = new(width);

            RecalculateRegions();
        }

        /// <summary>
        /// Replaces one region with another. 
        /// <br/><br/>
        /// All tiles belonging to the <c>original</c> region will now belong to the <c>other</c>, 
        /// destroying the original region and adding the original's size to the other's room.
        /// </summary>
        public void ReplaceRegion(Region original, Region other)
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

        public void RecalculateLinksRight(Chunk other)
        {
            int startLinkY = 0;
            uint currentLinkSize = 0;
            Region? lastThisRegion = null;
            Region? lastOtherRegion = null;
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

        public void RecalculateLinksDown(Chunk other)
        {
            int startLinkX = 0;
            uint currentLinkSize = 0;
            Region? lastThisRegion = null;
            Region? lastOtherRegion = null;
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

        private void AddLink(Region r1, Region r2, int x, int y, uint size, bool direction)
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
        /// Merges adjacent rooms between this right edge and the other Chunk's left edge.
        /// </summary>
        public void MergeRight(Chunk other)
        {
            for (var y = 0; y < height; y++)
            {
                var thisRegion = regionTiles[width - 1, y];
                var otherRegion = other.regionTiles[0, y];
                if (thisRegion == null || otherRegion == null)
                {
                    continue;
                }

                if (thisRegion.room != otherRegion.room)
                {
                    thisRegion.room.MergeFrom(otherRegion.room);
                    otherRegion.room = thisRegion.room;
                }
            }
        }

        /// <summary>
        /// Merges adjacent rooms between this bottom edge and the other Chunk's top edge.
        /// </summary>
        public void MergeDown(Chunk other)
        {
            for (var x = 0; x < width; x++)
            {
                var thisRegion = regionTiles[x, height - 1];
                var otherRegion = other.regionTiles[x, 0];
                if (thisRegion == null || otherRegion == null)
                {
                    continue;
                }

                if (thisRegion.room != otherRegion.room)
                {
                    thisRegion.room.MergeFrom(otherRegion.room);
                    otherRegion.room = thisRegion.room;
                }
            }
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

        public Region CreateNewRegion(int x, int y)
        {
            var region = new Region();
            regions.Add(region);
            region.IncrementSize();
            regionTiles[x, y] = region;
            return region;
        }

        public HashSet<Region> GetRegionsAdjacentTo(int x, int y)
        {
            HashSet<Region> regions = new(4);

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

            Region? region = null;

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
                            region = new Region();
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
                            region = new Region();
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
                tileMap.IsNavigable(topLeftX + x, topLeftY + y) &&
                regionTiles[x, y] == null;
        }
    }
}