namespace Space
{
    public class Chunk
    {
        private int topLeftX, topLeftY;
        private int width, height;

        private Region?[,] regionTiles;

        public Chunk(int topLeftX, int topLeftY, int width, int height)
        {
            this.topLeftX = topLeftX;
            this.topLeftY = topLeftY;
            this.width = width;
            this.height = height;
            regionTiles = new Region?[width, height];

            Room room = new();

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    regionTiles[x, y] = new Region(room);
                }
            }
        }

        private void FloodFill(int x, int y)
        {
            if (regionTiles[x, y] == null)
            {

            }
        }
    }
}