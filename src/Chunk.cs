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

        private readonly ITileMap tileMap;

        private readonly int topLeftX, topLeftY;
        private readonly int width, height;

        private Region?[,] regionTiles;

        public List<Region> regions { private set; get; }

        public Chunk(ITileMap tileMap, int topLeftX, int topLeftY, int width, int height)
        {
            this.tileMap = tileMap;
            this.topLeftX = topLeftX;
            this.topLeftY = topLeftY;
            this.width = width;
            this.height = height;

            regionTiles = new Region?[width, height];
            regions = new(width);

            RecalculateRegions();
        }

        /// <returns>The room at the given tile coordinates within the chunk.</returns>
        public Room? GetRoomAt(int x, int y)
        {
            return regionTiles[x, y]?.room;
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