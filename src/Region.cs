using System.Collections.Generic;

namespace Space
{
    public class Region<K> : IRegion where K : notnull
    {
        public int chunkX { get; }
        public int chunkY { get; }
        public int size { private set; get; }

        public Room room { get; private set; }

        public HashSet<uint> links { get; }

        public Dictionary<K, HashSet<IEntity<K>>> entities { get; }

        public Region(int chunkX, int chunkY)
        {
            this.chunkX = chunkX;
            this.chunkY = chunkY;
            room = new Room();
            links = new(8);
            entities = new();
        }

        public void IncrementSize()
        {
            AddSize(1);
        }

        public void AddSize(int i)
        {
            size += i;
            room.size += i;
        }

        public void DecrementSize()
        {
            size--;
            room.size--;
        }

        /// <summary>
        /// All links in this region are removed from the <c>linkCache</c>,
        /// and removed from their associated regions.
        /// </summary>
        public void ResetLinks(Dictionary<uint, LinkPair> linkCache)
        {
            foreach (var link in links)
            {
                if (linkCache.ContainsKey(link))
                {
                    var linkPair = linkCache[link];
                    linkPair.GetOtherRegion(this).links.Remove(link);
                    linkCache.Remove(link);
                }
            }
            links.Clear();
        }

        public void Destroy()
        {
            room.size -= size;
        }

        public void ReplaceRoom(Room newRoom)
        {
            Destroy();
            room = newRoom;
            room.size += size;
        }

        public void ResetRoom()
        {
            var oldSize = size;
            room.size -= size;
            size = 0;
            room = new Room();
            AddSize(oldSize);
        }

        public void AddEntity(K group, IEntity<K> entity)
        {
            HashSet<IEntity<K>> set;

            if (!entities.ContainsKey(group))
            {
                set = new();
                entities.Add(group, set);
            }
            else
            {
                set = entities[group];
            }

            set.Add(entity);
        }

        public void RemoveEntity(K group, IEntity<K> entity)
        {
            entities[group].Remove(entity);
        }

        public override string ToString()
        {
            return $"Region(room id={room.id}, size={size}, chunkX={chunkX}, chunkY={chunkY})";
        }
    }
}