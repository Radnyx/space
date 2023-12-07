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

        private const string MAP_STRING_LARGE_2 =
            "................" +
            ".......#........" +
            "......#........." +
            ".......#........" +
            "................";

        private const string MAP_STRING_LARGE_3 =
            "................" +
            ".......##......." +
            ".........#..#.#." +
            ".......##....#.." +
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

        private const string MOCK_ROOMS_LARGE_2 =
            "1111111111111111" +
            "1111111#11111111" +
            "111111#111111111" +
            "1111111#11111111" +
            "1111111111111111";

        private const string MOCK_ROOMS_LARGE_3 =
            "1111111111111111" +
            "1111111#11111111" +
            "111111#2#1111111" +
            "1111111#11111111" +
            "1111111111111111";

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

        private static int mockEntityIDs = 0;

        private class MockEntity : IEntity<string>
        {
            public int id = mockEntityIDs++;

            public List<string> groups;

            public MockEntity(List<string> groups)
            {
                this.groups = groups;
            }

            public IEnumerable<string> GetGroups() => groups;
        }

        private class MockTileMap : ITileMap
        {

            public readonly List<char> map;

            public ChunkGrid<string> grid = null!;

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

            public bool IsNavigable(int x, int y) => !IsOutOfBounds(x, y);

            public bool IsOutOfBounds(int x, int y) => map[y * w + x] == '#';

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

        [Fact]
        public void RecalculatingSingleChunkKeepsRooms()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();

            var grid = mockTileMap.grid;

            Assert.Equal(grid.GetRoomAt(7, 4), grid.GetRoomAt(6, 0));

            mockTileMap.SetTile(6, 1, '#');
            mockTileMap.SetTile(6, 2, '#');
            mockTileMap.SetTile(6, 3, '#');

            Assert.Equal(grid.GetRoomAt(7, 4), grid.GetRoomAt(6, 0));
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

        [Fact]
        public void CorrectlyLoadsRoomsBetweenEdges()
        {
            var mockTileMap = new MockTileMap(MAP_STRING_LARGE_2, 16, 5, 8, 5);
            mockTileMap.OnReady();

            ValidateMap(mockTileMap, MOCK_ROOMS_LARGE_2, new int[1] { 77 });
        }

        [Fact]
        public void FinishingARoomOverEdgeCreatesNewRoom()
        {
            var mockTileMap = new MockTileMap(MAP_STRING_LARGE_2, 16, 5, 8, 5);
            mockTileMap.OnReady();
            mockTileMap.SetTile(8, 2, '#');

            Assert.NotEqual(
                mockTileMap.grid.GetRoomAt(7, 2),
                mockTileMap.grid.GetRoomAt(9, 2)
            );

            ValidateMap(mockTileMap, MOCK_ROOMS_LARGE_3, new int[2] { 75, 1 });
        }

        [Fact(Skip = "TODO")]
        public void AddingTileShouldntRecalculateFarAwayRegions()
        {
            var mockTileMap = new MockTileMap(MAP_STRING_LARGE_3, 16, 5, 8, 5);
            mockTileMap.OnReady();

            var originalRoomID = mockTileMap.grid.GetRegionAt(8, 2);

            mockTileMap.SetTile(13, 1, '#');

            var newRoomId = mockTileMap.grid.GetRegionAt(8, 2);

            Assert.Equal(originalRoomID, newRoomId);
        }

        [Fact]
        public void AddsEntitiesToRegionStorage()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();
            var grid = mockTileMap.grid;
            var links = mockTileMap.grid.linkCache;

            var entity1 = new MockEntity(new() { "group1", "group2" });
            var entity2 = new MockEntity(new() { "group1" });
            var entity3 = new MockEntity(new() { "group2", "group3" });
            grid.RegisterEntityToRegion(entity1, 0, 0);
            grid.RegisterEntityToRegion(entity2, 1, 3);
            grid.RegisterEntityToRegion(entity3, 6, 1);

            var entities1 = grid.FindClosestEntitiesByRegion("group1", 7, 7)!;
            var entities1Empty = grid.FindClosestEntitiesByRegion("group1", 6, 2);
            Assert.Contains(entity1, entities1);
            Assert.Contains(entity2, entities1);
            Assert.DoesNotContain(entity3, entities1);
            Assert.Null(entities1Empty);

            var entities2 = grid.FindClosestEntitiesByRegion("group2", 7, 7)!;
            Assert.Contains(entity1, entities2);
            Assert.DoesNotContain(entity2, entities2);
            Assert.DoesNotContain(entity3, entities2);

            var entities2Other = grid.FindClosestEntitiesByRegion("group2", 5, 4)!;
            var entities3 = grid.FindClosestEntitiesByRegion("group3", 5, 4)!;
            Assert.Equal(entities2Other, entities3);
            Assert.Single(entities3);
            Assert.Contains(entity3, entities3);
        }

        [Fact]
        public void RemovesEntitiesFromRegionStorage()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();
            var grid = mockTileMap.grid;
            var links = mockTileMap.grid.linkCache;

            var entity1 = new MockEntity(new() { "group1", "group2" });
            var entity2 = new MockEntity(new() { "group1" });
            var entity3 = new MockEntity(new() { "group2", "group3" });
            grid.RegisterEntityToRegion(entity1, 0, 0);
            grid.RegisterEntityToRegion(entity2, 1, 3);
            grid.RegisterEntityToRegion(entity3, 6, 1);

            grid.RemoveEntity(entity1);
            grid.RemoveEntity(entity2);
            grid.RemoveEntity(entity3);

            var entities1 = grid.FindClosestEntitiesByRegion("group1", 7, 7);
            var entities2 = grid.FindClosestEntitiesByRegion("group2", 7, 7);
            var entities3 = grid.FindClosestEntitiesByRegion("group3", 5, 4);
            Assert.Null(entities1);
            Assert.Null(entities2);
            Assert.Null(entities3);
        }

        [Fact]
        public void OverridesEntityStorage()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();
            var grid = mockTileMap.grid;
            var links = mockTileMap.grid.linkCache;

            var entity1 = new MockEntity(new() { "group1", "group2" });
            var entity2 = new MockEntity(new() { "group1" });
            var entity3 = new MockEntity(new() { "group2", "group3" });
            grid.RegisterEntityToRegion(entity1, 0, 0);
            grid.RegisterEntityToRegion(entity2, 1, 3);
            grid.RegisterEntityToRegion(entity3, 6, 1);

            grid.RegisterEntityToRegion(entity1, 4, 0);

            var entities1 = grid.FindClosestEntitiesByRegion("group1", 7, 7)!;
            var entities2 = grid.FindClosestEntitiesByRegion("group2", 7, 7);
            var entities3 = grid.FindClosestEntitiesByRegion("group3", 5, 4)!;
            var entities2Other = grid.FindClosestEntitiesByRegion("group2", 7, 4);
            Assert.Contains(entity2, entities1);
            Assert.Single(entities1);

            Assert.Null(entities2);

            Assert.Single(entities3);
            Assert.Contains(entity3, entities3);

            Assert.Equal(2, entities2Other.Count);
            Assert.Contains(entity1, entities2Other);
            Assert.Contains(entity3, entities2Other);
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
                Assert.True(
                    entry.Key.size == entry.Value,
                    $"{entry.Key.size} != {entry.Value}\n" + string.Join(", ", regions)
                );
            }
        }

        private HashSet<IRegion> GetAllRegions(MockTileMap map)
        {
            HashSet<IRegion> regions = new();
            for (int x = 0; x < map.GetWidth(); x++)
            {
                for (int y = 0; y < map.GetHeight(); y++)
                {
                    var region = map.grid.GetRegionAt(x, y);
                    if (region != null)
                    {
                        regions.Add(region);
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