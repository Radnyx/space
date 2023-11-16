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

            public MockTileMap(string map, string expectedRegions)
            {
                this.map = map;
                this.expectedRegions = expectedRegions;
            }

            public int GetHeight() => 4;

            public int GetWidth() => 4;

            public bool IsNavigable(int x, int y) => map[y * 4 + x] != '#';
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

        private void ValidateMap(MockTileMap map, int[] sizes)
        {
            Chunk chunk = new(map, 0, 0, 4, 4);

            HashSet<Room?>[] rooms = new HashSet<Room?>[sizes.Length];
            for (var i = 0; i < rooms.Length; i++)
            {
                rooms[i] = new();
            }

            for (var x = 0; x < 4; x++)
            {
                for (var y = 0; y < 4; y++)
                {
                    var c = map.expectedRegions[y * 4 + x];
                    var room = chunk.GetRoomAt(x, y);

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