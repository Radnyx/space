namespace Space
{
    public interface ITileMap
    {
        public int GetWidth();
        public int GetHeight();

        // No pathfinding through these tiles.
        public bool IsNavigable(int x, int y);

        // This tile does not belong to a room.
        public bool IsOutOfBounds(int x, int y);
    }
}