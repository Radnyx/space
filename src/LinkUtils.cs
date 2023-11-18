global using LinkCache = System.Collections.Generic.Dictionary<uint, Space.LinkPair>;

namespace Space
{
    public struct LinkPair
    {
        public Region? r1, r2;
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
    }
}