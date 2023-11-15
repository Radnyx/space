using Space;
using System.Collections.Generic;
using Xunit;

namespace SpaceTest
{
    public class ChunkGridTests
    {
        private class MockTileMap : ITileMap
        {
            private const string map =
                "..#....." +
                "..#....." +
                "..#....." +
                "..#....." +
                "...##..." +
                ".....###" +
                "........" +
                "........";

            public int GetHeight() => 8;

            public int GetWidth() => 8;

            public bool IsNavigable(int x, int y) => map[y * 8 + x] == '#';
        }

        private const string mockRoom1 =
            "11#22222" +
            "11#22222" +
            "11#22222" +
            "11#22222" +
            "111##222" +
            "11111###" +
            "11111111" +
            "11111111";

        [Fact]
        public void ChunkGridCreatesTwoRooms()
        {
            var mockTileMap = new MockTileMap();
            ChunkGrid grid = new(mockTileMap, 4, 4);

            List<Room> room1 = new();
            List<Room> room2 = new();

            for (var x = 0; x < 8; x++)
            {
                for (var y = 0; y < 8; y++)
                {
                    var c = mockRoom1[y * 8 + x];
                    if (c == '1')
                    {
                        room1.Add(grid.GetRoomAt(x, y));
                    }
                    else if (c == '2')
                    {
                        room2.Add(grid.GetRoomAt(x, y));
                    }
                }
            }

            Assert.Single(room1);
            Assert.Single(room2);
            Assert.NotEqual(room1[0], room2[0]);
        }
    }
}