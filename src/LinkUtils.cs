global using LinkCache = System.Collections.Generic.Dictionary<uint, Space.LinkPair>;

namespace Space
{
    public struct LinkPair
    {
        public readonly Region r1, r2;

        public LinkPair(Region r1, Region r2)
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

        public static Region GetOtherRegion(this LinkPair link, Region region)
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
            return $"({x}, {y}), size={size}, right={right}";
        }
    }
}