using System;

namespace Space
{
    public class Room
    {
        private static Random random = new Random();

        public int size = 0;

        public int id;

        public Room()
        {
            id = random.Next();
        }

        // TODO: special properties and room types

        public void MergeFrom(Room other)
        {
            id = other.id;
            size += other.size;
        }
    }
}