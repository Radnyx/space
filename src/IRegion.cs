
using System.Collections.Generic;

namespace Space
{
    public interface IRegion
    {
        public int chunkX { get; }
        public int chunkY { get; }
        public int size { get; }

        public Room room { get; }

        HashSet<uint> links { get; }

        void IncrementSize();
        void DecrementSize();
        void AddSize(int size);
        void Destroy();

        void ReplaceRoom(Room room);

        void ResetLinks(Dictionary<uint, LinkPair> linkCache);
    }
}