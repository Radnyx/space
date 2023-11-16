using Space;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SpaceTest
{
    public class ChunkGridTests
    {
        private class MockTileMap : ITileMap
        {
            private readonly string mapString =
                "..#....." +
                "..#....." +
                "..#....." +
                "..#....." +
                "...##..." +
                "..#..###" +
                ".#......" +
                "........";

            private readonly List<char> map;

            public ChunkGrid grid = null!;

            public MockTileMap()
            {
                map = mapString.ToList();
            }

            public void OnReady()
            {
                grid = new(this, 4, 4);
            }

            public int GetHeight() => 8;

            public int GetWidth() => 8;

            public bool IsNavigable(int x, int y) => map[y * 8 + x] != '#';

            public void SetTile(int x, int y, char c)
            {
                map[y * 8 + x] = c;
                if (c == '#')
                {
                    grid.AddTileAt(x, y);
                }
                else if (c == '.')
                {
                    grid.RemoveTileAt(x, y);
                }
            }
        }

        private const string mockRooms =
            "11#22222" +
            "11#22222" +
            "11#22222" +
            "11#22222" +
            "111##222" +
            "11#11###" +
            "1#111111" +
            "11111111";

        [Fact]
        public void CreatesTwoRooms()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();

            HashSet<Room?> room1 = new();
            HashSet<Room?> room2 = new();

            for (var x = 0; x < 8; x++)
            {
                for (var y = 0; y < 8; y++)
                {
                    var c = mockRooms[y * 8 + x];
                    var room = mockTileMap.grid.GetRoomAt(x, y);
                    if (c == '1')
                    {
                        room1.Add(room);
                    }
                    else if (c == '2')
                    {
                        room2.Add(room);
                    }
                    else
                    {
                        Assert.Null(room);
                    }
                }
            }

            Assert.Single(room1);
            Assert.Single(room2);
            Assert.NotEqual(room1.First(), room2.First());
            Assert.NotNull(room1.First());
            Assert.NotNull(room2.First());
            Assert.Equal(30, room1.First()!.size);
            Assert.Equal(23, room2.First()!.size);
        }

        [Fact]
        public void AddingTileAddsAnotherRoom()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();
            mockTileMap.SetTile(0, 7, '#');
        }
    }
}