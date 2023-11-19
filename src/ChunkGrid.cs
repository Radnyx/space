using System.Diagnostics;

namespace Space
{
    public class ChunkGrid
    {
        private const int TILE_MAP_MAX_WIDTH_AND_HEIGHT = 1 << 12;
        private const int CHUNK_MAX_WIDTH_AND_HEIGHT = 1 << 6;
        private const int REGION_BFS_QUEUE_CAPACITY = 16;

        public readonly Chunk[,] chunks;
        public readonly LinkCache linkCache;

        private ITileMap tileMap;
        private readonly int chunkSizeX, chunkSizeY;
        private readonly int xChunks, yChunks;

        public ChunkGrid(ITileMap tileMap, int chunkSizeX, int chunkSizeY)
        {
            if (tileMap.GetWidth() % chunkSizeX != 0 || tileMap.GetHeight() % chunkSizeY != 0)
            {
                throw new InvalidOperationException("Tile map size is not divisible by given chunk size.");
            }

            if (tileMap.GetWidth() > TILE_MAP_MAX_WIDTH_AND_HEIGHT || tileMap.GetHeight() > TILE_MAP_MAX_WIDTH_AND_HEIGHT)
            {
                throw new InvalidOperationException($"Tile map width and height must be at most {TILE_MAP_MAX_WIDTH_AND_HEIGHT} tiles each.");
            }

            if (chunkSizeX > CHUNK_MAX_WIDTH_AND_HEIGHT || chunkSizeY > CHUNK_MAX_WIDTH_AND_HEIGHT)
            {
                throw new InvalidOperationException($"Chunk width and height must be at most {CHUNK_MAX_WIDTH_AND_HEIGHT} tiles each.");
            }

            this.tileMap = tileMap;
            this.chunkSizeX = chunkSizeX;
            this.chunkSizeY = chunkSizeY;

            xChunks = tileMap.GetWidth() / chunkSizeX;
            yChunks = tileMap.GetHeight() / chunkSizeY;

            linkCache = new(4 * xChunks * yChunks);

            chunks = new Chunk[chunkSizeX, chunkSizeY];

            for (var x = 0; x < xChunks; x++)
            {
                for (var y = 0; y < yChunks; y++)
                {
                    chunks[x, y] = new Chunk(
                        tileMap, linkCache,
                        x * chunkSizeX, y * chunkSizeY, chunkSizeX, chunkSizeY
                    );
                }
            }

            ConnectAdjacentRegions();
        }

        public void AddTileAt(int x, int y)
        {
            int chunkTileX = x % chunkSizeX;
            int chunkTileY = y % chunkSizeY;
            int chunkX = x / chunkSizeX;
            int chunkY = y / chunkSizeY;
            var chunk = chunks[chunkX, chunkY];
            int oldRegionCount = chunk.regions.Count;

            if (CanParitionRegionsWithinChunk(x, y) || CanParitionRoomOutsideOfChunk(x, y))
            {
                chunk.RecalculateRegions();
                RecalculateLinksForChunk(chunkX, chunkY);
            }
            else
            {
                var region = chunk.regionTiles[chunkTileX, chunkTileY];
                region?.DecrementSize();
                chunk.regionTiles[chunkTileX, chunkTileY] = null;

                if (IsChunkTileOnEdge(chunkTileX, chunkTileY))
                {
                    region?.ResetLinks(linkCache);
                    RecalculateLinksForChunk(chunkX, chunkY);
                }
                return;
            }

            if (oldRegionCount == chunk.regions.Count)
            {
                return;
            }

            Debug.Assert(oldRegionCount < chunk.regions.Count, "AddTileAt should only add regions.");

            // TODO: merge outward index = 1 onward, but merge from the outside into index = 0 !
            foreach (var region in chunk.regions)
            {
                MergeRoomsBreadthFirst(region);
            }
        }

        public void RemoveTileAt(int x, int y)
        {
            int chunkTileX = x % chunkSizeX;
            int chunkTileY = y % chunkSizeY;
            var chunk = GetChunkAt(x, y);

            // 1. Get the regions in the 4 adjacent tiles (inside the chunk).
            var regions = chunk.GetRegionsAdjacentTo(chunkTileX, chunkTileY);

            // 2a. No neighbors. New region, new room, connect if necessary.
            if (regions.Count == 0)
            {
                var region = chunk.CreateNewRegion(chunkTileX, chunkTileY);

                if (IsChunkTileOnEdge(chunkTileX, chunkTileY))
                {
                    RecalculateLinksForChunk(x / chunkSizeX, y / chunkSizeX);
                    MergeRoomsBreadthFirst(region);
                }

                return;
            }

            // 2b. Use the region belonging to the largest room.
            var regionOfBiggestRoom = regions.MaxBy(RoomSize)!;
            chunk.regionTiles[chunkTileX, chunkTileY] = regionOfBiggestRoom;
            regionOfBiggestRoom.IncrementSize();

            if (regions.Count == 1 && !IsChunkTileOnEdge(chunkTileX, chunkTileY))
            {
                // no effect on other regions or links
                return;
            }

            // 3. Replace the other regions' tiles with this one.
            foreach (var region in regions)
            {
                region.ResetLinks(linkCache);
                chunk.ReplaceRegion(region, regionOfBiggestRoom);
            }

            // 4. Recalculate the links
            RecalculateLinksForChunk(x / chunkSizeX, y / chunkSizeX);

            // 5. Merge room into all connected regions.
            MergeRoomsBreadthFirst(regionOfBiggestRoom);
        }

        /// <returns>
        /// The room at the given tile coordinates.
        /// </returns>
        public Room? GetRoomAt(int x, int y) => GetRegionAt(x, y)?.room;

        /// <returns>
        /// The region at the given tile coordinates.
        /// </returns>
        public Region? GetRegionAt(int x, int y) => GetChunkAt(x, y).regionTiles[x % chunkSizeX, y % chunkSizeY];

        /// <returns>
        /// The chunk at the given tile coordinates.
        /// </returns>
        public Chunk GetChunkAt(int x, int y) => chunks[x / chunkSizeX, y / chunkSizeY];

        private bool IsChunkTileOnEdge(int chunkTileX, int chunkTileY) =>
            chunkTileX == 0 || chunkTileY == 0 || chunkTileX == chunkSizeX - 1 || chunkTileY == chunkSizeY - 1;

        private void RecalculateLinksForChunk(int x, int y)
        {
            var currentChunk = chunks[x, y];
            if (x > 0)
            {
                chunks[x - 1, y].RecalculateLinksRight(currentChunk);
            }
            if (y > 0)
            {
                chunks[x, y - 1].RecalculateLinksDown(currentChunk);
            }
            if (x < xChunks - 1)
            {
                currentChunk.RecalculateLinksRight(chunks[x + 1, y]);
            }
            if (y < yChunks - 1)
            {
                currentChunk.RecalculateLinksDown(chunks[x, y + 1]);
            }
        }

        private void MergeRoomsBreadthFirst(Region region)
        {
            // TODO: Could try priority queue and try to merge with regions
            // closest to our original chunk, increasing the likelihood that we
            // can stop early (i.e., no new room has been created).

            HashSet<Region> seen = new(REGION_BFS_QUEUE_CAPACITY);
            Queue<Region> queue = new(REGION_BFS_QUEUE_CAPACITY);
            queue.Enqueue(region);

            while (queue.Count > 0)
            {
                var r = queue.Dequeue();

                foreach (var link in r.links)
                {
                    var linkPair = linkCache[link];
                    var otherRegion = linkPair.GetOtherRegion(r);

                    if (seen.Contains(otherRegion)) continue;
                    if (otherRegion.room == r.room) continue;

                    otherRegion.ReplaceRoom(r.room);

                    queue.Enqueue(otherRegion);
                    seen.Add(otherRegion);
                }
            }
        }

        private void ConnectAdjacentRegions()
        {
            for (var x = 0; x < xChunks; x++)
            {
                for (var y = 0; y < yChunks; y++)
                {
                    if (x < xChunks - 1)
                    {
                        chunks[x, y].MergeRight(chunks[x + 1, y]);
                        chunks[x, y].RecalculateLinksRight(chunks[x + 1, y]);
                    }
                    if (y < yChunks - 1)
                    {
                        chunks[x, y].MergeDown(chunks[x, y + 1]);
                        chunks[x, y].RecalculateLinksDown(chunks[x, y + 1]);
                    }
                }
            }
        }

        private bool CanParitionRegionsWithinChunk(int x, int y)
        {
            return CanPartitionRegions(x, y, false);
        }

        private bool CanParitionRoomOutsideOfChunk(int x, int y)
        {
            return CanPartitionRegions(x, y, true);
        }

        /// <returns>
        /// True if adding a tile at this location has the posibility of
        /// partitioning a region into multiple.
        /// </returns>
        /// <param name="cantCrossEdges">
        /// If false, consider all tiles over the edge of the chunk to be non-navigable.
        /// If true, actually inspect the tile on the other chunk.
        /// </param>
        /// <remarks>
        /// In other words, if you could walk from one adjacent tile to
        /// another adjacent tile, adding a tile here would make that impossible
        /// within the 3x3 space.
        /// </remarks>
        private bool CanPartitionRegions(int x, int y, bool cantCrossEdges)
        {
            int chunkTileX = x % chunkSizeX;
            int chunkTileY = y % chunkSizeY;
            bool onLeftEdge = x <= 0 || (cantCrossEdges && chunkTileX == 0);
            bool onTopEdge = y <= 0 || (cantCrossEdges && chunkTileY == 0);
            bool onRightEdge = x >= tileMap.GetWidth() - 1 || (cantCrossEdges && chunkTileX == chunkSizeX - 1);
            bool onBottomEdge = y >= tileMap.GetHeight() - 1 || (cantCrossEdges && chunkTileY == chunkSizeY - 1);

            bool a = onLeftEdge || onTopEdge || !tileMap.IsNavigable(x - 1, y - 1);
            bool b = onTopEdge || !tileMap.IsNavigable(x, y - 1);
            bool c = onRightEdge || onTopEdge || !tileMap.IsNavigable(x + 1, y - 1);
            bool d = onLeftEdge || !tileMap.IsNavigable(x - 1, y);
            bool f = onRightEdge || !tileMap.IsNavigable(x + 1, y);
            bool g = onLeftEdge || onBottomEdge || !tileMap.IsNavigable(x - 1, y + 1);
            bool h = onBottomEdge || !tileMap.IsNavigable(x, y + 1);
            bool i = onRightEdge || onBottomEdge || !tileMap.IsNavigable(x + 1, y + 1);

            /**
                Horrifying eldritch computer generated expression to quickly check the surrounding tiles. 
                I couldn't think of a simpler way that didn't involve loops. So I filtered the 256 possibilities 
                to the 123 cases where this returns true, built a giant boolean expression, and simplified it.
            */
            return
                (a && !b && c && !h) ||
                (a && !b && !d && f) ||
                (a && !b && !d && h) ||
                (a && !d && g && !h) ||
                (a && !f && !h && i) ||
                (b && !d && !f && h) ||
                (b && !d && g && !h) ||
                (b && !f && !h && i) ||
                (!b && c && d && !f) ||
                (!b && c && !f && h) ||
                (!b && c && g && !h) ||
                (!b && d && f && !h) ||
                (c && !f && !h && i) ||
                (d && !f && !h && i) ||
                (!d && f && g && !h) ||
                (!f && g && !h && i);
        }

        private static int RoomSize(Region r) => r.room.size;
    }
}