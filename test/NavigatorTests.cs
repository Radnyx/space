using Space;
using Space.Navigation;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Path = Space.Navigation.Path;

namespace SpaceTest
{
    public class NavigatorTests
    {
        private const string MAP_STRING_1 =
            "..#.....#..." +
            "............" +
            "...######..." +
            "...#....#..." +
            "...#....#..." +
            "..#.....##.#" +
            ".#.........." +
            "........#...";

        private class MockTileMap : ITileMap
        {

            public readonly List<char> map;

            public ChunkGrid<string> grid = null!;

            private readonly string mapString;
            private int w, h, cw, ch;

            public MockTileMap(string mapString = MAP_STRING_1)
            {
                this.mapString = mapString;
                this.w = 12;
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

            public bool IsOutOfBounds(int x, int y) => x < 0 || y >= h || map[y * w + x] == '#';

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
        public void FollowsPathThroughLink()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();

            Navigator navigator = new(mockTileMap.grid);

            Path path = navigator.FindPath(0, 0, 7, 6)!;

            Assert.Equal(2, path.highLevelPath.Count);
            Assert.Equal((2, 4), path.highLevelPath.Peek());

            Assert.Equal((0, 2), path.GetNextTilePosition(0, 1));
            Assert.Single(path.highLevelPath);

            Assert.Equal((1, 2), path.GetNextTilePosition(0, 2));
            Assert.Equal((1, 3), path.GetNextTilePosition(1, 2));
            Assert.Equal((1, 4), path.GetNextTilePosition(1, 3));
            Assert.Equal((2, 4), path.GetNextTilePosition(1, 4));
            Assert.Equal((1, 4), path.GetNextTilePosition(2, 4));

            Assert.Empty(path.highLevelPath);
        }

        [Fact]
        public void CancelsPathIfRegionsDisconnected()
        {
            var mockTileMap = new MockTileMap();
            mockTileMap.OnReady();

            Navigator navigator = new(mockTileMap.grid);

            Path path = navigator.FindPath(0, 0, 7, 6)!;

            Assert.Equal(2, path.highLevelPath.Count);
            Assert.Equal((2, 4), path.highLevelPath.Peek());

            Assert.Equal((0, 2), path.GetNextTilePosition(0, 1));
            Assert.Single(path.highLevelPath);

            Assert.Equal((1, 2), path.GetNextTilePosition(0, 2));
            Assert.Equal((1, 3), path.GetNextTilePosition(1, 2));
            Assert.Equal((1, 4), path.GetNextTilePosition(1, 3));
            Assert.Equal((2, 4), path.GetNextTilePosition(1, 4));

            mockTileMap.SetTile(0, 7, '#'); // close off regions

            Assert.Null(path.GetNextTilePosition(2, 4));
        }
    }
}