using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Benchmarking;

namespace Space
{
    public class ChunkGrid<K> : IChunkGrid where K : notnull
    {
        private const int TILE_MAP_MAX_WIDTH_AND_HEIGHT = 1 << 12;
        private const int CHUNK_MAX_WIDTH_AND_HEIGHT = 1 << 6;
        private const int REGION_BFS_QUEUE_CAPACITY = 64;
        private const int REGION_BFS_HASHSET_CAPACITY = 256;
        private const int FIND_ENTITIES_MAX_ITERATIONS = 32;

        public readonly Chunk<K>[,] chunks;
        public readonly Dictionary<uint, LinkPair> linkCache;
        public int xChunks { get; }
        public int yChunks { get; }
        public int chunkSizeX { get; }
        public int chunkSizeY { get; }

        public delegate void UpdateChunkEventHandler(int chunkX, int chunkY);
        public event UpdateChunkEventHandler? UpdateChunk;

        public delegate void UpdateRegionEventHandler(IRegion region);
        public event UpdateRegionEventHandler? UpdateRegion;

        private ITileMap tileMap;

        private Dictionary<IEntity<K>, Region<K>> entityRegions;

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

            chunks = new Chunk<K>[xChunks, yChunks];

            entityRegions = new(100);

            for (var x = 0; x < xChunks; x++)
            {
                for (var y = 0; y < yChunks; y++)
                {
                    chunks[x, y] = new Chunk<K>(
                        tileMap, linkCache,
                        x * chunkSizeX, y * chunkSizeY, chunkSizeX, chunkSizeY
                    );
                }
            }

            ConnectAdjacentRegions();
        }

        public bool IsNavigableAt(int x, int y)
        {
            return tileMap.IsNavigable(x, y);
        }

        public void AddTileAt(int x, int y)
        {
#if DEBUG
            Benchmark.Start("AddTileAt");
#endif
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

                if (region?.size == 0)
                {
                    chunk.regions.Remove(region);
                }

                if (IsChunkTileOnEdge(chunkTileX, chunkTileY))
                {
                    region?.ResetLinks(linkCache);
                    RecalculateLinksForChunk(chunkX, chunkY);
                }

#if DEBUG
                Benchmark.Stop("AddTileAt");
#endif
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

#if DEBUG
                Benchmark.Stop("AddTileAt");
#endif
                return;
            }

            Debug.Assert(oldRegionCount < chunk.regions.Count, "AddTileAt shouldn't decrease number of regions.");

            // 3a. Pick one region to maintain its old room.
            var firstRegion = chunk.regions[0];
            if (firstRegion.links.Count > 0)
            {
                MergeRoomFromOutside(firstRegion);
            }

            // 3b. All other regions will proliferate their new rooms outward.
            HashSet<IRegion> seen = new(REGION_BFS_HASHSET_CAPACITY);
            for (int i = 1; i < chunk.regions.Count; i++)
            {
                MergeRoomsBreadthFirst(chunk.regions[i], seen);
            }

            RecalculateRegionsOverEdge(x, y);

#if DEBUG
            Benchmark.Stop("AddTileAt");
#endif
        }

        private void RecalculateRegionsOverEdge(int x, int y)
        {
            HashSet<IRegion> seen = new(REGION_BFS_HASHSET_CAPACITY);

            var tilesPositionsOverEdge = GetTilePositionsOverEdge(x, y);
            foreach (var (overEdgeX, overEdgeY) in tilesPositionsOverEdge)
            {
                var region = GetRegionAt(overEdgeX, overEdgeY) as Region<K>;

                if (region == null) continue;

                region.ResetRoom();

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
        public IRoom? GetRoomAt(int x, int y) => GetRegionAt(x, y)?.room;

        /// <returns>
        /// The region at the given tile coordinates.
        /// </returns>
        public IRegion? GetRegionAt(int x, int y)
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

        public IRegion GetOtherRegionFromLink(uint link, IRegion thisRegion)
        {
            return linkCache[link].GetOtherRegion(thisRegion);
        }

        public void RegisterEntityToRegion(IEntity<K> entity, int tileX, int tileY)
        {
            var region = GetRegionAt(tileX, tileY) as Region<K>;
            if (region == null)
            {
                return;
            }

            Region<K>? oldRegion = null;

            if (entityRegions.ContainsKey(entity))
            {
                oldRegion = entityRegions[entity];
            }

            if (region != oldRegion)
            {
                foreach (var group in entity.GetGroups())
                {
                    oldRegion?.RemoveEntity(group, entity);
                    region.AddEntity(group, entity);
                }

                entityRegions[entity] = region;
            }
        }

        public void RemoveEntity(IEntity<K> entity)
        {
            if (entityRegions.ContainsKey(entity))
            {
                var region = entityRegions[entity];

                foreach (var group in entity.GetGroups())
                {
                    region.RemoveEntity(group, entity);
                }

                entityRegions.Remove(entity);
            }
        }

        /// <summary>
        /// Finds entities in the room at the given <c>tileX</c> and <c>tileY</c>.
        /// </summary>
        /// <returns>All entities of a given <c>group</c> inhabiting the room. Null if no entities are found.</returns>
        /// <remarks>O(1)</remarks>
        public HashSet<IEntity<K>>? GetEntitiesInRoomAt(K group, int tileX, int tileY)
        {
            var region = GetRegionAt(tileX, tileY);
            if (region == null)
            {
                return null;
            }

            var entities = ((Room<K>)region.room).entities;

            if (entities.ContainsKey(group))
            {
                var result = entities[group];
                if (result.Count == 0)
                {
                    return null;
                }

                return result;
            }

            return null;
        }

        /// <summary>
        /// Finds the closest region containing entities of the given <c>group</c>.
        /// </summary>
        /// <param name="tileX">X position in grid to start searching.</param>
        /// <param name="tileY">Y position in grid to start searching.</param>
        /// <returns>Set of entities belonging to the closest populated region. Null if no entities are found.</returns>
        /// <remarks>Runs in constant time limited by <c>MAX_ITERATIONS</c>.</remarks>
        public HashSet<IEntity<K>>? FindClosestEntitiesTo(K group, int tileX, int tileY)
        {
            HashSet<IRegion> seen = new(REGION_BFS_HASHSET_CAPACITY);
            Queue<IRegion> queue = new(REGION_BFS_QUEUE_CAPACITY);

            var region = GetRegionAt(tileX, tileY);
            if (region == null)
            {
                return null;
            }

            queue.Enqueue(region);

            int iterations = 0;
            while (queue.Count > 0 && iterations < FIND_ENTITIES_MAX_ITERATIONS)
            {
                Region<K> r = (Region<K>)queue.Dequeue();

                if (r.entities.ContainsKey(group))
                {
                    var entities = r.entities[group];
                    if (entities.Count > 0)
                    {
                        return entities;
                    }
                }

                foreach (var link in r.links)
                {
                    var linkPair = linkCache[link];
                    var otherRegion = linkPair.GetOtherRegion(r);

                    if (seen.Contains(otherRegion)) continue;

                    queue.Enqueue(otherRegion);
                    seen.Add(otherRegion);
                }

                iterations++;
            }

            return null;
        }

        public bool AreRegionsConnected(IRegion region1, IRegion region2)
        {
            foreach (var link in region1.links)
            {
                var other = linkCache[link].GetOtherRegion(region1);
                if (other == region2)
                {
                    return true;
                }
            }
            return false;
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

        private void MergeRoomFromOutside(IRegion firstRegion)
        {
            if (firstRegion.links.Count == 0)
            {
                return;
            }
            var otherRegion = linkCache[firstRegion.links.First()].GetOtherRegion(firstRegion);
            firstRegion.ReplaceRoom(otherRegion.room);
        }

        private void MergeRoomsBreadthFirst(IRegion region, HashSet<IRegion> seen)
        {
            Queue<IRegion> queue = new(REGION_BFS_QUEUE_CAPACITY);
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

            var rooms = new HashSet<IRoom>();

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

            bool a = onLeftEdge || onTopEdge || tileMap.IsOutOfBounds(x - 1, y - 1);
            bool b = onTopEdge || tileMap.IsOutOfBounds(x, y - 1);
            bool c = onRightEdge || onTopEdge || tileMap.IsOutOfBounds(x + 1, y - 1);
            bool d = onLeftEdge || tileMap.IsOutOfBounds(x - 1, y);
            bool f = onRightEdge || tileMap.IsOutOfBounds(x + 1, y);
            bool g = onLeftEdge || onBottomEdge || tileMap.IsOutOfBounds(x - 1, y + 1);
            bool h = onBottomEdge || tileMap.IsOutOfBounds(x, y + 1);
            bool i = onRightEdge || onBottomEdge || tileMap.IsOutOfBounds(x + 1, y + 1);

            return Utils.Partitions3by3Area(a, b, c, d, f, g, h, i);
        }

        private static int RoomSize(IRegion r) => r.room.size;
    }
}