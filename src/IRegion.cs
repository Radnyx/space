
using System.Collections.Generic;

namespace Space
{
    public interface IRegion
    {
        public int chunkX { get; }
        public int chunkY { get; }
        public int size { get; }

        public IRoom room { get; }

        HashSet<uint> links { get; }

        void IncrementSize();
        void DecrementSize();
        void AddSize(int size);
        void Destroy();

        void ReplaceRoom(IRoom room);

        void ResetLinks(Dictionary<uint, LinkPair> linkCache);
    }
}