namespace Space
{
    public static class Utils
    {
        public static bool Partitions3by3Area(bool a, bool b, bool c, bool d, bool f, bool g, bool h, bool i)
        {
            /**
               Horrifying eldritch computer generated expression to quickly check the surrounding tiles. 
               I couldn't think of a simpler way that didn't involve loops. So I filtered the 256 possibilities 
               to the 123 cases where this returns true, built a giant boolean expression, and simplified it.

               Should probably use a pre-computed table instead.
           */
            return
                (a && !b && c && !h) ||
                (a && !b && !d && f) ||
                (a && !b && !d && h) ||
                (a && !d && g && !h) ||
                (a && !f && !h && i) ||
                (b && !d && !f && h) ||
                (b && !d && g && !h) ||
                (b && !f && !h && i) ||
                (!b && c && d && !f) ||
                (!b && c && !f && h) ||
                (!b && c && g && !h) ||
                (!b && d && f && !h) ||
                (c && !f && !h && i) ||
                (d && !f && !h && i) ||
                (!d && f && g && !h) ||
                (!f && g && !h && i);
        }
    }
}