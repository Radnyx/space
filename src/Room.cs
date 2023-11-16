namespace Space
{
    public class Room
    {
        public int size = 0;

        // TODO: special properties and room types

        public void MergeFrom(Room other)
        {
            size += other.size;
        }
    }
}