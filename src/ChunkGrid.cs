using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Space
{
    public class ChunkGrid
    {
        private const int TILE_MAP_MAX_WIDTH_AND_HEIGHT = 1 << 12;
        private const int CHUNK_MAX_WIDTH_AND_HEIGHT = 1 << 6;
        private const int REGION_BFS_QUEUE_CAPACITY = 64;
        private const int REGION_BFS_HASHSET_CAPACITY = 256;

        public readonly Chunk[,] chunks;
        public readonly LinkCache linkCache;
        public readonly int xChunks, yChunks;

        public delegate void UpdateChunkEventHandler(int chunkX, int chunkY);
        public event UpdateChunkEventHandler? UpdateChunk;

        public delegate void UpdateRegionEventHandler(Region region);
        public event UpdateRegionEventHandler? UpdateRegion;

        private ITileMap tileMap;
        private readonly int chunkSizeX, chunkSizeY;

        public ChunkGrid(ITileMap tileMap, int chunkSizeX, int chunkSizeY)
        {
            if (tileMap.GetWidth() % chunkSizeX != 0 || tileMap.GetHeight() % chunkSizeY != 0)
            {
                throw new InvalidOperationException("Tile map size is not divisible by given chunk size.");
            }

            if (tileMap.GetWidth() > TILE_MAP_MAX_WIDTH_AND_HEIGHT || tileMap.GetHeight() > TILE_MAP_MAX_WIDTH_AND_HEIGHT)
            {
                throw new InvalidOperationException($"Tile map width and height must be at most {TILE_MAP_MAX_WIDTH_AND_HEIGHT} tiles each.");
            }

            if (chunkSizeX > CHUNK_MAX_WIDTH_AND_HEIGHT || chunkSizeY > CHUNK_MAX_WIDTH_AND_HEIGHT)
            {
                throw new InvalidOperationException($"Chunk width and height must be at most {CHUNK_MAX_WIDTH_AND_HEIGHT} tiles each.");
            }

            this.tileMap = tileMap;
            this.chunkSizeX = chunkSizeX;
            this.chunkSizeY = chunkSizeY;

            xChunks = tileMap.GetWidth() / chunkSizeX;
            yChunks = tileMap.GetHeight() / chunkSizeY;

            linkCache = new(4 * xChunks * yChunks);

            chunks = new Chunk[xChunks, yChunks];

            for (var x = 0; x < xChunks; x++)
            {
                for (var y = 0; y < yChunks; y++)
                {
                    chunks[x, y] = new Chunk(
                        tileMap, linkCache,
                        x * chunkSizeX, y * chunkSizeY, chunkSizeX, chunkSizeY
                    );
                }
            }

            ConnectAdjacentRegions();
        }

        public void AddTileAt(int x, int y)
        {
            int chunkTileX = x % chunkSizeX;
            int chunkTileY = y % chunkSizeY;
            int chunkX = x / chunkSizeX;
            int chunkY = y / chunkSizeY;
            var chunk = chunks[chunkX, chunkY];

            UpdateChunk?.Invoke(chunkX, chunkY);

            // 1. If we definitely can't add new regions, don't bother floodfilling.
            if (!CanParitionRegionsWithinChunk(x, y) && !CanParitionRoomOutsideOfChunk(x, y))
            {
                var region = chunk.regionTiles[chunkTileX, chunkTileY];
                region?.DecrementSize();
                chunk.regionTiles[chunkTileX, chunkTileY] = null;

                if (IsChunkTileOnEdge(chunkTileX, chunkTileY))
                {
                    region?.ResetLinks(linkCache);
                    RecalculateLinksForChunk(chunkX, chunkY);
                }
                return;
            }

            // 2. Re-floodfill this chunk.
            int oldRegionCount = chunk.regions.Count;

            chunk.RecalculateRegions();
            RecalculateLinksForChunk(chunkX, chunkY);

            if (oldRegionCount == chunk.regions.Count)
            {
                foreach (var region in chunk.regions)
                {
                    MergeRoomFromOutside(region);
                }

                RecalculateRegionsOverEdge(x, y);
                return;
            }

            Debug.Assert(oldRegionCount < chunk.regions.Count, "AddTileAt shouldn't remove adjacent regions.");

            // 3a. Pick one region to maintain its old room.
            var firstRegion = chunk.regions[0];
            if (firstRegion.links.Count > 0)
            {
                MergeRoomFromOutside(firstRegion);
            }

            // 3b. All other regions will proliferate their new rooms outward.
            HashSet<Region> seen = new(REGION_BFS_HASHSET_CAPACITY);
            for (int i = 1; i < chunk.regions.Count; i++)
            {
                MergeRoomsBreadthFirst(chunk.regions[i], seen);
            }

            RecalculateRegionsOverEdge(x, y);
        }

        private void RecalculateRegionsOverEdge(int x, int y)
        {
            HashSet<Region> seen = new(REGION_BFS_HASHSET_CAPACITY);

            var tilesPositionsOverEdge = GetTilePositionsOverEdge(x, y);
            foreach (var (overEdgeX, overEdgeY) in tilesPositionsOverEdge)
            {
                var region = GetRegionAt(overEdgeX, overEdgeY);

                if (region == null) continue;

                var oldSize = region.size;
                region.ClearSize();
                region.room = new Room();
                region.AddSize(oldSize);
                MergeRoomsBreadthFirst(region, seen);
            }
        }

        public void RemoveTileAt(int x, int y)
        {
            int chunkTileX = x % chunkSizeX;
            int chunkTileY = y % chunkSizeY;
            int chunkX = x / chunkSizeX;
            int chunkY = y / chunkSizeY;
            var chunk = chunks[chunkX, chunkY];

            UpdateChunk?.Invoke(chunkX, chunkY);

            // 1. Get the regions in the 4 adjacent tiles (inside the chunk).
            var regions = chunk.GetRegionsAdjacentTo(chunkTileX, chunkTileY);

            // 2a. No neighbors. New region, new room, connect if necessary.
            if (regions.Count == 0)
            {
                var region = chunk.CreateNewRegion(chunkTileX, chunkTileY);

                if (IsChunkTileOnEdge(chunkTileX, chunkTileY))
                {
                    RecalculateLinksForChunk(x / chunkSizeX, y / chunkSizeX);
                    MergeRoomsBreadthFirst(region, new(REGION_BFS_HASHSET_CAPACITY));
                }

                return;
            }

            // 2b. Use the region belonging to the largest room.
            var regionOfBiggestRoom = regions.MaxBy(RoomSize)!;
            chunk.regionTiles[chunkTileX, chunkTileY] = regionOfBiggestRoom;
            regionOfBiggestRoom.IncrementSize();

            if (regions.Count == 1 && !IsChunkTileOnEdge(chunkTileX, chunkTileY))
            {
                // no effect on other regions or links
                return;
            }

            // 3. Replace the other regions' tiles with this one.
            foreach (var region in regions)
            {
                region.ResetLinks(linkCache);
                chunk.ReplaceRegion(region, regionOfBiggestRoom);
            }

            // 4. Recalculate the links
            RecalculateLinksForChunk(x / chunkSizeX, y / chunkSizeX);

            // 5. Merge room into all connected regions.
            MergeRoomsBreadthFirst(regionOfBiggestRoom, new(REGION_BFS_HASHSET_CAPACITY));
        }

        /// <returns>
        /// The room at the given tile coordinates.
        /// </returns>
        public Room? GetRoomAt(int x, int y) => GetRegionAt(x, y)?.room;

        /// <returns>
        /// The region at the given tile coordinates.
        /// </returns>
        public Region? GetRegionAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= tileMap.GetWidth() || y >= tileMap.GetHeight())
            {
                return null;
            }
            return chunks[x / chunkSizeX, y / chunkSizeY].regionTiles[x % chunkSizeX, y % chunkSizeY];
        }

        private bool IsChunkTileOnEdge(int chunkTileX, int chunkTileY) =>
            chunkTileX == 0 || chunkTileY == 0 || chunkTileX == chunkSizeX - 1 || chunkTileY == chunkSizeY - 1;

        /*
        public List<(int, int)> GetAdjacentTilePositions(int x, int y)
        {
            var tilePositions = new List<(int, int)>(4);
            if (x > 0)
            {
                tilePositions.Add((x - 1, y));
            }
            if (y > 0)
            {
                tilePositions.Add((x, y - 1));
            }
            if (x < tileMap.GetWidth() - 1)
            {
                tilePositions.Add((x + 1, y));
            }
            if (y < tileMap.GetHeight() - 1)
            {
                tilePositions.Add((x, y + 1));
            }
            return tilePositions;
        }
        */

        public List<(int, int)> GetTilePositionsOverEdge(int x, int y)
        {
            var tilePositions = new List<(int, int)>(4);
            int chunkTileX = x % chunkSizeX;
            int chunkTileY = y % chunkSizeY;
            if (chunkTileX == 0)
            {
                tilePositions.Add((x - 1, y));
            }
            if (chunkTileY == 0)
            {
                tilePositions.Add((x, y - 1));
            }
            if (chunkTileX == chunkSizeX - 1)
            {
                tilePositions.Add((x + 1, y));
            }
            if (chunkTileY == chunkSizeY - 1)
            {
                tilePositions.Add((x, y + 1));
            }
            return tilePositions;
        }

        private void RecalculateLinksForChunk(int x, int y)
        {
            var currentChunk = chunks[x, y];
            if (x > 0)
            {
                chunks[x - 1, y].RecalculateLinksRight(currentChunk);
            }
            if (y > 0)
            {
                chunks[x, y - 1].RecalculateLinksDown(currentChunk);
            }
            if (x < xChunks - 1)
            {
                currentChunk.RecalculateLinksRight(chunks[x + 1, y]);
            }
            if (y < yChunks - 1)
            {
                currentChunk.RecalculateLinksDown(chunks[x, y + 1]);
            }
        }

        private void MergeRoomFromOutside(Region firstRegion)
        {
            if (firstRegion.links.Count == 0)
            {
                return;
            }
            var otherRegion = linkCache[firstRegion.links.First()].GetOtherRegion(firstRegion);
            firstRegion.ReplaceRoom(otherRegion.room);
        }

        private void MergeRoomsBreadthFirst(Region region, HashSet<Region> seen)
        {
            // TODO: Could try priority queue and try to merge with regions
            // closest to our original chunk, increasing the likelihood that we
            // can stop early (i.e., no new room has been created).

            Queue<Region> queue = new(REGION_BFS_QUEUE_CAPACITY);
            queue.Enqueue(region);

            while (queue.Count > 0)
            {
                var r = queue.Dequeue();

                UpdateRegion?.Invoke(r);

                foreach (var link in r.links)
                {
                    var linkPair = linkCache[link];
                    var otherRegion = linkPair.GetOtherRegion(r);

                    if (seen.Contains(otherRegion)) continue;
                    if (otherRegion.room == r.room) continue;

                    otherRegion.ReplaceRoom(r.room);

                    queue.Enqueue(otherRegion);
                    seen.Add(otherRegion);
                }
            }
        }

        private void ConnectAdjacentRegions()
        {
            for (var x = 0; x < xChunks; x++)
            {
                for (var y = 0; y < yChunks; y++)
                {
                    if (x < xChunks - 1)
                    {
                        chunks[x, y].RecalculateLinksRight(chunks[x + 1, y]);
                    }
                    if (y < yChunks - 1)
                    {
                        chunks[x, y].RecalculateLinksDown(chunks[x, y + 1]);
                    }
                }
            }

            var rooms = new HashSet<Room>();

            for (var x = 0; x < xChunks; x++)
            {
                for (var y = 0; y < yChunks; y++)
                {
                    foreach (var region in chunks[x, y].regions)
                    {
                        if (rooms.Contains(region.room))
                        {
                            continue;
                        }

                        MergeRoomsBreadthFirst(region, new(REGION_BFS_HASHSET_CAPACITY));

                        rooms.Add(region.room);
                    }
                }
            }
        }

        private bool CanParitionRegionsWithinChunk(int x, int y)
        {
            return CanPartitionRegions(x, y, false);
        }

        private bool CanParitionRoomOutsideOfChunk(int x, int y)
        {
            return CanPartitionRegions(x, y, true);
        }

        /// <returns>
        /// True if adding a tile at this location has the posibility of
        /// partitioning a region into multiple.
        /// </returns>
        /// <param name="cantCrossEdges">
        /// If false, consider all tiles over the edge of the chunk to be non-navigable.
        /// If true, actually inspect the tile on the other chunk.
        /// </param>
        /// <remarks>
        /// In other words, if you could walk from one adjacent tile to
        /// another adjacent tile, adding a tile here would make that impossible
        /// within the 3x3 space.
        /// </remarks>
        private bool CanPartitionRegions(int x, int y, bool cantCrossEdges)
        {
            // TODO: a look up table will be a bit faster and probably more readable...

            int chunkTileX = x % chunkSizeX;
            int chunkTileY = y % chunkSizeY;
            bool onLeftEdge = x <= 0 || (cantCrossEdges && chunkTileX == 0);
            bool onTopEdge = y <= 0 || (cantCrossEdges && chunkTileY == 0);
            bool onRightEdge = x >= tileMap.GetWidth() - 1 || (cantCrossEdges && chunkTileX == chunkSizeX - 1);
            bool onBottomEdge = y >= tileMap.GetHeight() - 1 || (cantCrossEdges && chunkTileY == chunkSizeY - 1);

            bool a = onLeftEdge || onTopEdge || !tileMap.IsNavigable(x - 1, y - 1);
            bool b = onTopEdge || !tileMap.IsNavigable(x, y - 1);
            bool c = onRightEdge || onTopEdge || !tileMap.IsNavigable(x + 1, y - 1);
            bool d = onLeftEdge || !tileMap.IsNavigable(x - 1, y);
            bool f = onRightEdge || !tileMap.IsNavigable(x + 1, y);
            bool g = onLeftEdge || onBottomEdge || !tileMap.IsNavigable(x - 1, y + 1);
            bool h = onBottomEdge || !tileMap.IsNavigable(x, y + 1);
            bool i = onRightEdge || onBottomEdge || !tileMap.IsNavigable(x + 1, y + 1);

            /**
                Horrifying eldritch computer generated expression to quickly check the surrounding tiles. 
                I couldn't think of a simpler way that didn't involve loops. So I filtered the 256 possibilities 
                to the 123 cases where this returns true, built a giant boolean expression, and simplified it.
            */
            return
                (a && !b && c && !h) ||
                (a && !b && !d && f) ||
                (a && !b && !d && h) ||
                (a && !d && g && !h) ||
                (a && !f && !h && i) ||
                (b && !d && !f && h) ||
                (b && !d && g && !h) ||
                (b && !f && !h && i) ||
                (!b && c && d && !f) ||
                (!b && c && !f && h) ||
                (!b && c && g && !h) ||
                (!b && d && f && !h) ||
                (c && !f && !h && i) ||
                (d && !f && !h && i) ||
                (!d && f && g && !h) ||
                (!f && g && !h && i);
        }

        private static int RoomSize(Region r) => r.room.size;
    }
}