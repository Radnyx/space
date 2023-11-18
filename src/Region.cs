namespace Space
{

    public class Region
    {
        public Room room;

        public readonly List<uint> links;

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

        /// <summary>
        /// All links in this region are removed from the <c>linkCache</c>,
        /// and removed from their associated regions.
        /// </summary>
        public void ResetLinks(LinkCache linkCache)
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
    }
}