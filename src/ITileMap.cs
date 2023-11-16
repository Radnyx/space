namespace Space
{
    public interface ITileMap
    {
        public int GetWidth();
        public int GetHeight();
        public bool IsNavigable(int x, int y);
    }
}