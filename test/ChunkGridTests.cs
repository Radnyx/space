using Space;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        private const string MAP_STRING_COMPLICATED_CORNER_CASE =
            "###...###" +
            "###...###" +
            "###...###" +
            "....#...." +
            "...###..." +
            "...###...";

        private const string MAP_STRING_LARGE =
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................";

        private const string MAP_STRING_TINY =
            ".." +
            ".." +
            ".." +
            "..";


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

        private readonly HashSet<(int, int)> MOCK_RANDOM_ADD_REMOVE_1 = new() {
           (4, 7), (10, 4), (0, 2), (4, 14), (3, 10), (3, 13), (2, 12),
           (10, 14), (6, 11), (15, 8), (4, 11), (3, 2), (11, 0), (1, 15), (7, 4), (6, 11)
        };

        private readonly HashSet<(int, int)> MOCK_RANDOM_ADD_REMOVE_2 = new() {
            (11, 2), (7, 12), (9, 3),(15, 10),(6, 8),(4, 12),(12, 10),(10, 12),(14, 5),(8, 11),(12, 5),(8, 14),(12, 13),(3, 1),(7, 3),(4, 0),
            (0, 7),(11, 14),(5, 4),(11, 15),(4, 11),(8, 15),(4, 15),(2, 8),(9, 14),(6, 14),(0, 0),(11, 2),(4, 7),(13, 7),(3, 12),(5, 4),(9, 6),
            (4, 10),(3, 4),(5, 3),(6, 5),(4, 14),(5, 9),(13, 1),(15, 9),(6, 9),(10, 10),(15, 10),(12, 3),(13, 5),(8, 8),(14, 3),(10, 9),(7, 2),
            (3, 11),(10, 9),(4, 11),(2, 7),(5, 14),(3, 5),(5, 3),(3, 10),(3, 1),(7, 14),(4, 14),(7, 6),(2, 2),(15, 12),(12, 0),(5, 0),(4, 11),
            (13, 1),(6, 11),(13, 4),(7, 3),(2, 10),(9, 0),(9, 1),(12, 15),(7, 10),(4, 15),(14, 9),(8, 9),(10, 8),(7, 0),(8, 9),(7, 1),(15, 15),
            (15, 12),(3, 13),(8, 3),(10, 10),(9, 6),(4, 11),(6, 0),(3, 11),(4, 1),(0, 7),(14, 10),(6, 13),(8, 7),(12, 11),(9, 2),(4, 10)
        };

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
        public void AddingTileAddsRegion()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();
            mockTileMap.SetTile(3, 1, '#');

            var chunks = mockTileMap.grid.chunks;
            Assert.Equal(3, chunks[0, 0].regions.Count);
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
            AssertRegionsSumToRooms(mockTileMap);
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

            var links = mockTileMap.grid.linkCache;
            Assert.Equal(4, links.Count);

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

            var chunks = mockTileMap.grid.chunks;
            var links = mockTileMap.grid.linkCache;
            Assert.Equal(5, links.Count);

            var link = links[LinkUtils.Hash(3, 2, 1, false)];
            Assert.Equal(chunks[1, 0].regions[0], link.r1);
            Assert.Equal(chunks[1, 1].regions[0], link.r2);

            link = links[LinkUtils.Hash(5, 2, 1, false)];
            Assert.Equal(chunks[1, 0].regions[0], link.r1);
            Assert.Equal(chunks[1, 1].regions[1], link.r2);

            link = links[LinkUtils.Hash(5, 2, 1, true)];
            Assert.Equal(chunks[1, 0].regions[0], link.r1);
            Assert.Equal(chunks[2, 0].regions[0], link.r2);

            link = links[LinkUtils.Hash(6, 2, 1, false)];
            Assert.Equal(chunks[2, 0].regions[0], link.r1);
            Assert.Equal(chunks[2, 1].regions[0], link.r2);

            link = links[LinkUtils.Hash(5, 3, 1, true)];
            Assert.Equal(chunks[1, 1].regions[1], link.r1);
            Assert.Equal(chunks[2, 1].regions[0], link.r2);

            ValidateMap(mockTileMap, MOCK_ROOMS_COMPLICATED_2, new int[2] { 20, 8 });

            mockTileMap.SetTile(2, 2, '.');
            mockTileMap.SetTile(1, 2, '.');
            mockTileMap.SetTile(5, 1, '#');

            ValidateMap(mockTileMap, MOCK_ROOMS_COMPLICATED_3, new int[2] { 17, 12 });

            Assert.Equal(7, links.Count);

            link = links[LinkUtils.Hash(1, 2, 1, false)];
            Assert.Equal(chunks[0, 0].regions[0], link.r1);
            Assert.Equal(chunks[0, 1].regions[0], link.r2);

            link = links[LinkUtils.Hash(2, 2, 1, true)];
            Assert.Equal(chunks[0, 0].regions[0], link.r1);
            Assert.Equal(chunks[1, 0].regions[0], link.r2);

            link = links[LinkUtils.Hash(3, 2, 1, false)];
            Assert.Equal(chunks[1, 0].regions[0], link.r1);
            Assert.Equal(chunks[1, 1].regions[0], link.r2);

            link = links[LinkUtils.Hash(5, 2, 1, false)];
            Assert.Equal(chunks[1, 0].regions[1], link.r1);
            Assert.Equal(chunks[1, 1].regions[1], link.r2);

            link = links[LinkUtils.Hash(5, 2, 1, true)];
            Assert.Equal(chunks[1, 0].regions[1], link.r1);
            Assert.Equal(chunks[2, 0].regions[0], link.r2);

            link = links[LinkUtils.Hash(6, 2, 1, false)];
            Assert.Equal(chunks[2, 0].regions[0], link.r1);
            Assert.Equal(chunks[2, 1].regions[0], link.r2);

            link = links[LinkUtils.Hash(5, 3, 1, true)];
            Assert.Equal(chunks[1, 1].regions[1], link.r1);
            Assert.Equal(chunks[2, 1].regions[0], link.r2);
        }

        [Fact]
        public void ComplicatedAddingAndRemoving3()
        {
            var mockTileMap = new MockTileMap(MAP_STRING_COMPLICATED_CORNER_CASE, 9, 6, 3, 3);
            mockTileMap.OnReady();
            mockTileMap.SetTile(2, 3, '#');

            Assert.Null(mockTileMap.grid.GetRoomAt(2, 3));
        }

        [Fact]
        public void RandomlyAddAndRemoveTiles1()
        {
            var mockTileMap = new MockTileMap(MAP_STRING_LARGE, 16, 16, 4, 4);
            mockTileMap.OnReady();
            var chunks = mockTileMap.grid.chunks;
            var links = mockTileMap.grid.linkCache;

            foreach (var position in MOCK_RANDOM_ADD_REMOVE_1)
            {
                mockTileMap.SetTile(position.Item1, position.Item2, '#');
            }

            foreach (var position in MOCK_RANDOM_ADD_REMOVE_1)
            {
                mockTileMap.SetTile(position.Item1, position.Item2, '.');
            }

            Assert.Equal(24, links.Count);
            Assert.Equal(1, CountRooms(mockTileMap));
            Assert.Equal(16, GetAllRegions(mockTileMap).Count);
            AssertRegionsSumToRooms(mockTileMap);
        }

        public static string SpliceText(string text, int lineLength)
        {
            return Regex.Replace(text, "(.{" + lineLength + "})", "$1\n");
        }

        [Fact]
        public void RandomlyAddAndRemoveTiles2()
        {
            var mockTileMap = new MockTileMap(MAP_STRING_LARGE, 16, 16, 4, 4);
            mockTileMap.OnReady();
            var chunks = mockTileMap.grid.chunks;
            var links = mockTileMap.grid.linkCache;

            foreach (var position in MOCK_RANDOM_ADD_REMOVE_2)
            {
                mockTileMap.SetTile(position.Item1, position.Item2, '#');
            }

            var topLeftChunkStr = string.Join("\n", chunks[0, 0].regions[0].links.Select(LinkUtils.ToString));
            Assert.True(links.ContainsKey(LinkUtils.Hash(0, 3, 3, false)), topLeftChunkStr);
            Assert.True(links.ContainsKey(LinkUtils.Hash(3, 2, 2, true)), topLeftChunkStr);
            Assert.True(links.ContainsKey(LinkUtils.Hash(4, 3, 1, false)));
            Assert.True(links.ContainsKey(LinkUtils.Hash(6, 3, 1, false)));
            Assert.True(links.ContainsKey(LinkUtils.Hash(10, 3, 2, false)));
            Assert.True(links.ContainsKey(LinkUtils.Hash(11, 1, 1, true)));

            Assert.True(links.ContainsKey(LinkUtils.Hash(3, 6, 1, true)));
            Assert.True(links.ContainsKey(LinkUtils.Hash(1, 7, 1, false)));
            Assert.True(links.ContainsKey(LinkUtils.Hash(3, 7, 1, false)));

            Assert.True(links.ContainsKey(LinkUtils.Hash(7, 4, 2, true)));
            Assert.True(links.ContainsKey(LinkUtils.Hash(5, 7, 1, false)));
            Assert.True(links.ContainsKey(LinkUtils.Hash(7, 7, 1, false)));

            Assert.True(links.ContainsKey(LinkUtils.Hash(11, 6, 2, true)));
            Assert.True(links.ContainsKey(LinkUtils.Hash(11, 4, 1, true)));
            Assert.True(links.ContainsKey(LinkUtils.Hash(9, 7, 1, false)));
            Assert.True(links.ContainsKey(LinkUtils.Hash(11, 7, 1, false)));

            Assert.True(links.ContainsKey(LinkUtils.Hash(12, 7, 1, false)));
            Assert.True(links.ContainsKey(LinkUtils.Hash(14, 7, 2, false)));

            Assert.Equal(28, links.Count);
            Assert.Equal(24, GetAllRegions(mockTileMap).Count);

            foreach (var position in MOCK_RANDOM_ADD_REMOVE_2)
            {
                mockTileMap.SetTile(position.Item1, position.Item2, '.');
            }

            foreach (var link in links)
            {
                Assert.Equal(4u, (link.Key >> 24) & 63);
            }

            Assert.Equal(24, links.Count);
            Assert.Equal(1, CountRooms(mockTileMap));
            Assert.Equal(16, GetAllRegions(mockTileMap).Count);
            AssertRegionsSumToRooms(mockTileMap);
        }

        [Fact]
        public void AddTileAtEdgeWithoutRecalculateRemovesOldLinks()
        {
            var mockTileMap = new MockTileMap(MAP_STRING_LARGE, 2, 4, 2, 2);
            mockTileMap.OnReady();
            mockTileMap.SetTile(0, 1, '#');

            Assert.Single(mockTileMap.grid.chunks[0, 0].regions[0].links);
            Assert.Single(mockTileMap.grid.chunks[0, 1].regions[0].links);
        }

        private void AssertRegionsSumToRooms(MockTileMap map)
        {
            Dictionary<Room, int> totals = new();
            var regions = GetAllRegions(map);
            foreach (var region in regions)
            {
                totals[region.room] = totals.GetValueOrDefault(region.room) + region.size;
            }

            foreach (var entry in totals)
            {
                Assert.Equal(entry.Key.size, entry.Value);
            }

        }

        private HashSet<Region> GetAllRegions(MockTileMap map)
        {
            HashSet<Region> regions = new();
            for (int x = 0; x < map.GetWidth(); x++)
            {
                for (int y = 0; y < map.GetHeight(); y++)
                {
                    var room = map.grid.GetRegionAt(x, y);
                    if (room != null)
                    {
                        regions.Add(room);
                    }
                }
            }
            return regions;
        }

        private int CountRooms(MockTileMap map)
        {
            HashSet<Room> rooms = new();
            for (int x = 0; x < map.GetWidth(); x++)
            {
                for (int y = 0; y < map.GetHeight(); y++)
                {
                    var room = map.grid.GetRoomAt(x, y);
                    if (room != null)
                    {
                        rooms.Add(room);
                    }
                }
            }
            return rooms.Count;
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
                        Assert.True(room == null, $"Room at ({x}, {y}), size = {room?.size}, should be null.");
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

            AssertRegionsSumToRooms(map);
        }
    }
}