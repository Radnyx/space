using Space;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SpaceTest
{
    public class ChunkTests
    {
        private class MockTileMap : ITileMap
        {
            private string map;
            public string expectedRegions;

            private int width, height;

            public MockTileMap(string map, string expectedRegions)
            {
                this.map = map;
                this.expectedRegions = expectedRegions;
                this.width = 4;
                this.height = 4;
            }

            public MockTileMap(string map, int width, int height)
            {
                this.map = map;
                this.width = width;
                this.height = height;
                expectedRegions = "";
            }

            public int GetHeight() => width;

            public int GetWidth() => height;

            public bool IsNavigable(int x, int y) => !IsOutOfBounds(x, y);

            public bool IsOutOfBounds(int x, int y) => map[y * width + x] == '#';
        }

        private MockTileMap map1 = new(
            "..#." +
            "..#." +
            "..#." +
            "..#.",
            "11#2" +
            "11#2" +
            "11#2" +
            "11#2"
        );

        private MockTileMap map2 = new(
            "...." +
            "...." +
            "...." +
            "....",
            "1111" +
            "1111" +
            "1111" +
            "1111"
        );

        private MockTileMap map3 = new(
            "...#" +
            "...." +
            "...." +
            "....",
            "111#" +
            "1111" +
            "1111" +
            "1111"
        );

        private MockTileMap map4 = new(
            "#..." +
            ".###" +
            "...." +
            "....",
            "#111" +
            "2###" +
            "2222" +
            "2222"
        );

        private MockTileMap map5 = new(
            ".#.." +
            ".##." +
            ".#.#" +
            "#...",
            "1#22" +
            "1##2" +
            "1#3#" +
            "#333"
        );

        private MockTileMap map6 = new(
           "####" +
           "####" +
           "####" +
           "####",
           "####" +
           "####" +
           "####" +
           "####"
       );

        private MockTileMap mapLinkRight = new(
            "........" +
            "...#...." +
            "........" +
            "........" +
            "....####" +
            "...##..." +
            "........" +
            "####...." +
            "........" +
            "........",
            8, 10
         );

        private MockTileMap mapLinkDown = new(
            ".........." +
            "...#..#..#" +
            "..#...#..." +
            "..........",
            10, 4
        );

        private MockTileMap mapLinkDown2 = new(
            "..#....." +
            "..#....." +
            "..#....." +
            "..#....." +
            "...##..." +
            "..#..###" +
            ".#......" +
            "#.......",
            8, 8
        );

        [Fact]
        public void ChunkCalculatesInitialRegions1()
        {
            ValidateMap(map1, new int[2] { 8, 4 });
        }

        [Fact]
        public void ChunkCalculatesInitialRegions2()
        {
            ValidateMap(map2, new int[1] { 16 });
        }

        [Fact]
        public void ChunkCalculatesInitialRegions3()
        {
            ValidateMap(map3, new int[1] { 15 });
        }

        [Fact]
        public void ChunkCalculatesInitialRegions4()
        {
            ValidateMap(map4, new int[2] { 3, 9 });
        }

        [Fact]
        public void ChunkCalculatesInitialRegions5()
        {
            ValidateMap(map5, new int[3] { 3, 3, 4 });
        }

        [Fact]
        public void ChunkCalculatesInitialRegions6()
        {
            ValidateMap(map6, new int[0] { });
        }

        [Fact]
        public void CreatesLinksRight()
        {
            Dictionary<System.UInt32, LinkPair> linkCache = new();
            Chunk<string> chunk1 = new(mapLinkRight, linkCache, 0, 0, 4, 10);
            Chunk<string> chunk2 = new(mapLinkRight, linkCache, 4, 0, 4, 10);
            chunk1.RecalculateLinksRight(chunk2);

            Assert.Equal(4, linkCache.Count);

            var link1 = linkCache[0b1000001000000000000000000000011u];
            var link2 = linkCache[0b1000010000000000010000000000011u];
            var link3 = linkCache[0b1000001000000000110000000000011u];
            var link4 = linkCache[0b1000010000000001000000000000011u];

            Assert.Equal(link1.r1, chunk1.regions[0]);
            Assert.Equal(link1.r2, chunk2.regions[0]);

            Assert.Equal(link2.r1, chunk1.regions[0]);
            Assert.Equal(link2.r2, chunk2.regions[0]);

            Assert.Equal(link3.r1, chunk1.regions[0]);
            Assert.Equal(link3.r2, chunk2.regions[1]);

            Assert.Equal(link4.r1, chunk1.regions[1]);
            Assert.Equal(link4.r2, chunk2.regions[1]);
        }

        [Fact]
        public void CreatesLinksDown()
        {
            Dictionary<System.UInt32, LinkPair> linkCache = new();
            Chunk<string> chunk1 = new(mapLinkDown, linkCache, 0, 0, 5, 2);
            Chunk<string> chunk2 = new(mapLinkDown, linkCache, 5, 0, 5, 2);
            Chunk<string> chunk3 = new(mapLinkDown, linkCache, 0, 2, 5, 2);
            Chunk<string> chunk4 = new(mapLinkDown, linkCache, 5, 2, 5, 2);
            chunk1.RecalculateLinksDown(chunk3);
            chunk2.RecalculateLinksDown(chunk4);

            Assert.Equal(4, linkCache.Count);

            var link1 = linkCache[0b0000010000000000001000000000000u];
            var link2 = linkCache[0b0000001000000000001000000000100u];
            var link3 = linkCache[0b0000001000000000001000000000101u];
            var link4 = linkCache[0b0000010000000000001000000000111u];

            Assert.Equal(link1.r1, chunk1.regions[0]);
            Assert.Equal(link1.r2, chunk3.regions[0]);

            Assert.Equal(link2.r1, chunk1.regions[0]);
            Assert.Equal(link2.r2, chunk3.regions[0]);

            Assert.Equal(link3.r1, chunk2.regions[0]);
            Assert.Equal(link3.r2, chunk4.regions[0]);

            Assert.Equal(link4.r1, chunk2.regions[0]);
            Assert.Equal(link4.r2, chunk4.regions[0]);
        }

        [Fact]
        public void CreatesLinksDown2()
        {
            Dictionary<System.UInt32, LinkPair> linkCache = new();
            Chunk<string> chunk1 = new(mapLinkDown2, linkCache, 0, 0, 4, 4);
            Chunk<string> chunk2 = new(mapLinkDown2, linkCache, 0, 4, 4, 4);
            chunk1.RecalculateLinksDown(chunk2);

            Assert.Single(linkCache);

            var link1 = linkCache[0b0000010000000000011000000000000u];

            Assert.Equal(link1.r1, chunk1.regions[0]);
            Assert.Equal(link1.r2, chunk2.regions[0]);
        }

        private void ValidateMap(MockTileMap map, int[] sizes)
        {
            Chunk<string> chunk = new(map, new(), 0, 0, 4, 4);

            HashSet<IRoom?>[] rooms = new HashSet<IRoom?>[sizes.Length];
            for (var i = 0; i < rooms.Length; i++)
            {
                rooms[i] = new();
            }

            for (var x = 0; x < 4; x++)
            {
                for (var y = 0; y < 4; y++)
                {
                    var c = map.expectedRegions[y * 4 + x];
                    var room = chunk.regionTiles[x, y]?.room;

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

            Assert.True(sizes.ToHashSet().SetEquals(chunk.regions.Select(r => r.size)));

            for (var i = 0; i < rooms.Length; i++)
            {
                var room = rooms[i];
                Assert.Single(room);
                Assert.NotNull(room.First());
                Assert.Equal(sizes[i], room.First()!.size);

                for (var j = 0; j < i; j++)
                {
                    Assert.NotEqual(room.First(), rooms[j].First());
                }
            }
        }
    }
}