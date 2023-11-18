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
            int chunkX = x / chunkSizeX;
            int chunkY = y / chunkSizeY;
            var chunk = chunks[chunkX, chunkY];
            int oldRegionCount = chunk.regions.Count;

            // TODO: More sophisticated check in a 3x3 region to see if
            //       it's even possible that new regions are added.
            //       However, if the tile is placed on the edge of a chunk,
            //       the links need to be recalculated for that edge.

            chunk.RecalculateRegions();
            RecalculateLinksForChunk(chunkX, chunkY);

            if (oldRegionCount == chunk.regions.Count)
            {
                return;
            }

            Debug.Assert(oldRegionCount < chunk.regions.Count, "AddTileAt should only add regions.");

            foreach (var region in chunk.regions)
            {
                MergeRoomsBreadthFirst(region);
            }
        }

        public void RemoveTileAt(int x, int y)
        {
            // 1. get the regions in the 4 adjacent tiles
            // 2. merge all the rooms of these regions
            // 2. pick one of the regions, replace all
            //    tiles in these regions with that one region
            // 3. increment size of this region by 1
            // 4. if no regions are adjacent (surrounded by solid tiles),
            //    then simply create a new region and new room
        }

        /// <returns>
        /// The room at the given tile coordinates.
        /// </returns>
        public Room? GetRoomAt(int x, int y)
        {
            return GetChunkAt(x, y).GetRoomAt(x % chunkSizeX, y % chunkSizeY);
        }

        /// <returns>
        /// The chunk at the given tile coordinates.
        /// </returns>
        public Chunk GetChunkAt(int x, int y)
        {
            return chunks[x / chunkSizeX, y / chunkSizeY];
        }

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
    }
}