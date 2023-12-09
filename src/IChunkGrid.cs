
namespace Space
{
    public interface IChunkGrid
    {
        public int xChunks { get; }
        public int yChunks { get; }
        public int chunkSizeX { get; }
        public int chunkSizeY { get; }

        public bool IsNavigableAt(int tileX, int tileY);

        public IRegion? GetRegionAt(int tileX, int tileY);

        public IRegion GetOtherRegionFromLink(uint link, IRegion thisRegion);

        public bool AreRegionsConnected(IRegion region1, IRegion region2);
    }
}