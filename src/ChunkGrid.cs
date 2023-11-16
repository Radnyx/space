namespace Space
{
    public class ChunkGrid
    {
        private int chunkSizeX, chunkSizeY;
        private int xChunks, yChunks;

        private Chunk[,] chunks;

        public ChunkGrid(ITileMap tileMap, int chunkSizeX, int chunkSizeY)
        {
            if (tileMap.GetWidth() % chunkSizeX != 0 || tileMap.GetHeight() % chunkSizeY != 0)
            {
                throw new InvalidOperationException("Tile map size is not divisible by given chunk size.");
            }

            this.chunkSizeX = chunkSizeX;
            this.chunkSizeY = chunkSizeY;

            xChunks = tileMap.GetWidth() / chunkSizeX;
            yChunks = tileMap.GetHeight() / chunkSizeY;
            chunks = new Chunk[chunkSizeX, chunkSizeY];

            for (var x = 0; x < xChunks; x++)
            {
                for (var y = 0; y < yChunks; y++)
                {
                    chunks[x, y] = new Chunk(tileMap, x * chunkSizeX, y * chunkSizeY, chunkSizeX, chunkSizeY);
                }
            }

            MergeConnectedRegions();
        }

        public void AddTileAt(int x, int y)
        {
            // 1. Recalculate regions
            // 2. For each region in the chunk, breadth first flood fill via
            //    the links and merge rooms with all adjacent regions.
            GetChunkAt(x, y).RecalculateRegions();
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

        /// <returns>The room at the given tile coordinates.</returns>
        public Room? GetRoomAt(int x, int y)
        {
            return GetChunkAt(x, y).GetRoomAt(x % chunkSizeX, y % chunkSizeY);
        }

        private Chunk GetChunkAt(int x, int y)
        {
            return chunks[x / chunkSizeX, y / chunkSizeY];
        }

        private void MergeConnectedRegions()
        {
            for (var x = 0; x < xChunks; x++)
            {
                for (var y = 0; y < yChunks; y++)
                {
                    if (x < xChunks - 1)
                    {
                        chunks[x, y].MergeRight(chunks[x + 1, y]);
                    }
                    if (y < yChunks - 1)
                    {
                        chunks[x, y].MergeDown(chunks[x, y + 1]);
                    }
                }
            }
        }
    }
}