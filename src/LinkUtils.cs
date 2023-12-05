namespace Space
{
    public struct LinkData
    {
        public uint x, y, size;
        public bool right;

        public LinkData(uint link)
        {
            this.x = link & (4096 - 1);
            this.y = (link >> 12) & (4096 - 1);
            this.size = (link >> 24) & (64 - 1);
            this.right = ((link >> 30) & 1) == 1;
        }
    }

    public struct LinkPair
    {
        public readonly IRegion r1, r2;

        public LinkPair(IRegion r1, IRegion r2)
        {
            this.r1 = r1;
            this.r2 = r2;
        }
    }

    public static class LinkUtils
    {
        public static uint Hash(uint x, uint y, uint size, bool right)
        {
            uint link = 0;
            link |= x & (4096 - 1);
            link |= (y & (4096 - 1)) << 12;
            link |= (size & (64 - 1)) << 24;
            if (right)
            {
                link |= 1 << 30;
            }
            return link;
        }

        public static IRegion GetOtherRegion(this LinkPair link, IRegion region)
        {
            if (link.r1 == region)
            {
                return link.r2!;
            }
            return link.r1!;
        }

        public static string ToString(uint link)
        {
            uint x = link & (4096 - 1);
            uint y = (link >> 12) & (4096 - 1);
            uint size = (link >> 24) & (64 - 1);
            bool right = ((link >> 30) & 1) == 1;
            return $"Link(x={x}, y={y}, size={size}, right={right}";
        }
    }
}