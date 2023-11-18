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

        private const string MAP_STRING_COMPLICATED =
            "###...###" +
            "###...###" +
            "###...###" +
            "...###..." +
            "...###..." +
            "...###...";


        private const string MOCK_ROOMS =
            "11#22222" +
            "11#22222" +
            "11#22222" +
            "11#22222" +
            "111##222" +
            "11#11###" +
            "1#111111" +
            "11111111";

        private const string MOCK_ROOMS_2 =
            "11#22222" +
            "11#22222" +
            "11#22222" +
            "11#22222" +
            "111##222" +
            "11#33###" +
            "1#333333" +
            "#3333333";

        private const string MOCK_ROOMS_3 =
            "11#44#22" +
            "11###222" +
            "11#22222" +
            "11#22222" +
            "111##222" +
            "11#33###" +
            "1#3333#5" +
            "#33333#5";

        private const string MOCK_ROOMS_COMPLICATED_1 =
            "###111###" +
            "#2#111###" +
            "###111###" +
            "1111#1111" +
            "111###111" +
            "111###111";

        private const string MOCK_ROOMS_COMPLICATED_2 =
            "###111###" +
            "###111###" +
            "###1#11##" +
            "22#1#1111" +
            "222###111" +
            "222###111";

        private const string MOCK_ROOMS_COMPLICATED_3 =
            "###111###" +
            "###11####" +
            "#111#22##" +
            "11#1#2222" +
            "111###222" +
            "111###222";

        private class MockTileMap : ITileMap
        {

            public readonly List<char> map;

            public ChunkGrid grid = null!;

            private readonly string mapString;
            private int w, h, cw, ch;

            public MockTileMap(string mapString = MAP_STRING_1)
            {
                this.mapString = mapString;
                this.w = 8;
                this.h = 8;
                this.cw = 4;
                this.ch = 4;
                map = mapString.ToList();
            }

            public MockTileMap(string mapString, int w, int h, int cw, int ch)
            {
                this.mapString = mapString;
                this.w = w;
                this.h = h;
                this.cw = cw;
                this.ch = ch;
                map = mapString.ToList();
            }

            public void OnReady()
            {
                grid = new(this, cw, ch);
            }

            public int GetHeight() => h;

            public int GetWidth() => w;

            public bool IsNavigable(int x, int y) => map[y * w + x] != '#';

            public void SetTile(int x, int y, char c)
            {
                map[y * w + x] = c;
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

        [Fact]
        public void CreatesTwoRooms()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();

            ValidateMap(mockTileMap, MOCK_ROOMS, new int[2] { 30, 23 });
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

            ValidateMap(mockTileMap, MOCK_ROOMS_2, new int[3] { 14, 23, 15 });
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

            ValidateMap(mockTileMap, MOCK_ROOMS_3, new int[5] { 14, 18, 11, 2, 2 });
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

            ValidateMap(mockTileMap, MOCK_ROOMS, new int[2] { 30, 23 });
        }

        [Fact]
        public void ComplicatedAddingAndRemoving1()
        {
            var mockTileMap = new MockTileMap(MAP_STRING_COMPLICATED, 9, 6, 3, 3);
            mockTileMap.OnReady();
            mockTileMap.SetTile(1, 1, '.');
            mockTileMap.SetTile(3, 3, '.');
            mockTileMap.SetTile(5, 3, '.');

            ValidateMap(mockTileMap, MOCK_ROOMS_COMPLICATED_1, new int[2] { 29, 1 });
        }

        [Fact]
        public void ComplicatedAddingAndRemoving2()
        {
            var mockTileMap = new MockTileMap(MAP_STRING_COMPLICATED, 9, 6, 3, 3);
            mockTileMap.OnReady();
            mockTileMap.SetTile(3, 3, '.');
            mockTileMap.SetTile(5, 3, '.');
            mockTileMap.SetTile(2, 3, '#');
            mockTileMap.SetTile(4, 2, '#');
            mockTileMap.SetTile(6, 2, '.');

            ValidateMap(mockTileMap, MOCK_ROOMS_COMPLICATED_2, new int[2] { 20, 8 });

            mockTileMap.SetTile(2, 2, '.');
            mockTileMap.SetTile(1, 2, '.');
            mockTileMap.SetTile(5, 1, '#');

            ValidateMap(mockTileMap, MOCK_ROOMS_COMPLICATED_3, new int[2] { 17, 12 });
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