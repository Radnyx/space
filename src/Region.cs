namespace Space
{
    using Link = Int32;

    public class Region
    {
        public Room room;

        public readonly List<Link> links;

        public int size { private set; get; }

        public Region()
        {
            room = new Room();
            links = new(4);
        }

        public void IncrementSize()
        {
            size++;
            room.size++;
        }

        public void Destroy()
        {
            room.size -= size;
        }
    }
}