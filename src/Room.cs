using System;
using System.Collections.Generic;

namespace Space
{
    public class Room<K> : IRoom where K : notnull
    {
        private static Random random = new Random();

        public int size { set; get; } = 0;

        public int id { private set; get; }

        public Dictionary<K, HashSet<IEntity<K>>> entities { get; }

        public Room()
        {
            id = random.Next();
            entities = new();
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
    }
}