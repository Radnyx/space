namespace Space
{
    public class ChunkGrid
    {
        private Chunk[,] chunks;

        public ChunkGrid(ITileMap tileMap, int chunkSizeX, int chunkSizeY)
        {
            if (tileMap.GetWidth() % chunkSizeX != 0 || tileMap.GetHeight() % chunkSizeY != 0)
            {
                throw new InvalidOperationException("Tile map size is not divisible by given chunk size.");
            }

            var xChunks = tileMap.GetWidth() / chunkSizeX;
            var yChunks = tileMap.GetHeight() / chunkSizeY;
            chunks = new Chunk[chunkSizeX, chunkSizeY];

            for (var x = 0; x < xChunks; x++)
            {
                for (var y = 0; y < yChunks; y++)
                {
                    chunks[x, y] = new Chunk(x * chunkSizeX, y * chunkSizeY, chunkSizeX, chunkSizeY);
                }
            }
        }

        /// <returns>The room at the given tile coordinates.</returns>
        public Room GetRoomAt(int x, int y)
        {
            return new Room();
        }
    }
}