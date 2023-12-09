using System.Collections.Generic;

namespace Space
{
    public class Region<K> : IRegion where K : notnull
    {
        public int chunkX { get; }
        public int chunkY { get; }
        public int size { private set; get; }

        public IRoom room { get; private set; }

        public HashSet<uint> links { get; }

        public readonly Dictionary<K, HashSet<IEntity<K>>> entities;

        public Region(int chunkX, int chunkY)
        {
            this.chunkX = chunkX;
            this.chunkY = chunkY;
            room = new Room<K>();
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

            RemoveEntitiesFromRoom();
        }

        public void ReplaceRoom(IRoom newRoom)
        {
            Destroy();

            room = newRoom;
            room.size += size;

            AddEntitiesToRoom();
        }

        public void ResetRoom()
        {
            RemoveEntitiesFromRoom();

            var oldSize = size;
            room.size -= size;
            size = 0;
            room = new Room<K>();
            AddSize(oldSize);

            AddEntitiesToRoom();
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

            ((Room<K>)room).AddEntity(group, entity);
        }

        public void RemoveEntity(K group, IEntity<K> entity)
        {
            entities[group].Remove(entity);

            ((Room<K>)room).RemoveEntity(group, entity);
        }

        public override string ToString()
        {
            return $"Region(room id={room.id}, size={size}, chunkX={chunkX}, chunkY={chunkY})";
        }

        private void AddEntitiesToRoom()
        {
            foreach (var (group, set) in entities)
            {
                foreach (var entity in set)
                {
                    ((Room<K>)room).AddEntity(group, entity);
                }
            }
        }

        private void RemoveEntitiesFromRoom()
        {
            foreach (var (group, set) in entities)
            {
                foreach (var entity in set)
                {
                    ((Room<K>)room).RemoveEntity(group, entity);
                }
            }
        }
    }
}