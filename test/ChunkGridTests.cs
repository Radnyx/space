using Space;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SpaceTest
{
    public class ChunkGridTests
    {
        private const string MAP_STRING_1 =
            "..#....." +
            "..#....." +
            "..#....." +
            "..#....." +
            "...##..." +
            "..#..###" +
            ".#......" +
            "........";

        private const string MAP_STRING_2 =
            "..#..#.." +
            "..###..." +
            "..#....." +
            "..#....." +
            "...##..." +
            "..#..###" +
            ".#....#." +
            "#.....#.";

        private class MockTileMap : ITileMap
        {
            private readonly string mapString;

            public readonly List<char> map;

            public ChunkGrid grid = null!;

            public MockTileMap(string mapString = MAP_STRING_1)
            {
                this.mapString = mapString;
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

        private const string mockRooms2 =
            "11#22222" +
            "11#22222" +
            "11#22222" +
            "11#22222" +
            "111##222" +
            "11#33###" +
            "1#333333" +
            "#3333333";

        private const string mockRooms3 =
            "11#44#22" +
            "11###222" +
            "11#22222" +
            "11#22222" +
            "111##222" +
            "11#33###" +
            "1#3333#5" +
            "#33333#5";

        [Fact]
        public void CreatesTwoRooms()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();

            ValidateMap(mockTileMap, mockRooms, new int[2] { 30, 23 });
        }

        [Fact]
        public void CreatesLinks()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();
            var chunks = mockTileMap.grid.chunks;
            var links = mockTileMap.grid.linkCache;
            Assert.Equal(4, links.Count);

            var link1 = links[0b1_000100_000000000000_000000000011u];
            var link2 = links[0b0_000010_000000000011_000000000000u];
            var link3 = links[0b0_000011_000000000011_000000000101u];
            var link4 = links[0b1_000011_000000000101_000000000011u];
            Assert.Equal(chunks[0, 0].regions[1], link1.r1);
            Assert.Equal(chunks[1, 0].regions[0], link1.r2);

            Assert.Equal(chunks[0, 0].regions[0], link2.r1);
            Assert.Equal(chunks[0, 1].regions[0], link2.r2);

            Assert.Equal(chunks[1, 0].regions[0], link3.r1);
            Assert.Equal(chunks[1, 1].regions[1], link3.r2);

            Assert.Equal(chunks[0, 1].regions[0], link4.r1);
            Assert.Equal(chunks[1, 1].regions[0], link4.r2);
        }

        [Fact]
        public void AddingTileAddsAnotherRoom()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();
            mockTileMap.SetTile(0, 7, '#');

            var chunks = mockTileMap.grid.chunks;
            Assert.Equal(2, chunks[0, 1].regions.Count);

            var links = mockTileMap.grid.linkCache;
            Assert.Equal(4, links.Count);

            var link1 = links[0b1_000100_000000000000_000000000011u];
            var link2 = links[0b0_000010_000000000011_000000000000u];
            var link3 = links[0b0_000011_000000000011_000000000101u];
            var link4 = links[0b1_000011_000000000101_000000000011u];
            Assert.Equal(chunks[0, 0].regions[1], link1.r1);
            Assert.Equal(chunks[1, 0].regions[0], link1.r2);

            Assert.Equal(chunks[0, 0].regions[0], link2.r1);
            Assert.Equal(chunks[0, 1].regions[0], link2.r2);

            Assert.Equal(chunks[1, 0].regions[0], link3.r1);
            Assert.Equal(chunks[1, 1].regions[1], link3.r2);

            Assert.Equal(chunks[0, 1].regions[1], link4.r1);
            Assert.Equal(chunks[1, 1].regions[0], link4.r2);

            Assert.Equal(6, chunks[0, 1].regions[0].size);
            Assert.Equal(6, chunks[0, 1].regions[1].size);
            Assert.Equal(chunks[1, 1].regions[0].room, chunks[0, 1].regions[1].room);

            ValidateMap(mockTileMap, mockRooms2, new int[3] { 14, 23, 15 });
        }

        [Fact]
        public void AddingMultipleTilesInARow()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();
            mockTileMap.SetTile(0, 7, '#');
            mockTileMap.SetTile(6, 7, '#');
            mockTileMap.SetTile(3, 1, '#');
            mockTileMap.SetTile(6, 6, '#');
            mockTileMap.SetTile(5, 0, '#');
            mockTileMap.SetTile(4, 1, '#');

            var chunks = mockTileMap.grid.chunks;
            Assert.Equal(3, chunks[0, 0].regions.Count);
            Assert.Equal(2, chunks[1, 0].regions.Count);
            Assert.Equal(3, chunks[1, 1].regions.Count);

            var links = mockTileMap.grid.linkCache;
            Assert.Equal(5, links.Count);

            ValidateMap(mockTileMap, mockRooms3, new int[5] { 14, 18, 11, 2, 2 });
        }

        [Fact]
        public void RemovingTileCombinesRegions()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();
            var chunks = mockTileMap.grid.chunks;
            mockTileMap.SetTile(3, 1, '#');
            mockTileMap.SetTile(3, 1, '.');

            Assert.Equal(2, chunks[0, 0].regions.Count);
        }

        [Fact]
        public void RemovingMultipleTilesInARow()
        {
            var mockTileMap = new MockTileMap(MAP_STRING_2);
            mockTileMap.OnReady();
            mockTileMap.SetTile(0, 7, '.');
            mockTileMap.SetTile(6, 7, '.');
            mockTileMap.SetTile(3, 1, '.');
            mockTileMap.SetTile(6, 6, '.');
            mockTileMap.SetTile(5, 0, '.');
            mockTileMap.SetTile(4, 1, '.');

            Assert.Equal(mockTileMap.map, MAP_STRING_1.ToList());

            var chunks = mockTileMap.grid.chunks;
            Assert.Equal(2, chunks[0, 0].regions.Count);
            Assert.Single(chunks[1, 0].regions);
            Assert.Single(chunks[0, 1].regions);
            Assert.Equal(2, chunks[1, 1].regions.Count);

            var links = mockTileMap.grid.linkCache;
            Assert.Equal(4, links.Count);

            var link1 = links[0b1_000100_000000000000_000000000011u];
            var link2 = links[0b0_000010_000000000011_000000000000u];
            var link3 = links[0b0_000011_000000000011_000000000101u];
            var link4 = links[0b1_000011_000000000101_000000000011u];
            Assert.Equal(chunks[0, 0].regions[1], link1.r1);
            Assert.Equal(chunks[1, 0].regions[0], link1.r2);

            Assert.Equal(chunks[0, 0].regions[0], link2.r1);
            Assert.Equal(chunks[0, 1].regions[0], link2.r2);

            Assert.Equal(chunks[1, 0].regions[0], link3.r1);
            Assert.Equal(chunks[1, 1].regions[1], link3.r2);

            Assert.Equal(chunks[0, 1].regions[0], link4.r1);
            Assert.Equal(chunks[1, 1].regions[0], link4.r2);

            ValidateMap(mockTileMap, mockRooms, new int[2] { 30, 23 });
        }

        private void ValidateMap(MockTileMap map, string expectedRooms, int[] sizes)
        {
            HashSet<Room?>[] rooms = new HashSet<Room?>[sizes.Length];
            for (var i = 0; i < rooms.Length; i++)
            {
                rooms[i] = new();
            }

            for (var x = 0; x < map.GetWidth(); x++)
            {
                for (var y = 0; y < map.GetHeight(); y++)
                {
                    var c = expectedRooms[y * map.GetWidth() + x];
                    var room = map.grid.GetRoomAt(x, y);

                    if (c == '#')
                    {
                        Assert.Null(room);
                    }
                    else
                    {
                        rooms[c - '1'].Add(room);
                    }
                }
            }

            for (var i = 0; i < rooms.Length; i++)
            {
                var room = rooms[i];
                Assert.Single(room);
                Assert.NotNull(room.First());
                Assert.Equal(sizes[i], room.First()!.size);

                if (i < rooms.Length - 1)
                {
                    Assert.NotEqual(room.First(), rooms[i + 1].First());
                }
            }
        }
    }
}