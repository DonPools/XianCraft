
using System;
using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using MonoGame.Extended.Tiled;
using System.Linq;

namespace XianCraft.Systems;

public class WorldGenerationSystem : AEntitySetSystem<GameTime>
{
    private readonly World _world;
    private EntityManager _entityManager;
    private TiledMap _metaMap;

    private TerrainGenerator _terrainGenerator = new TerrainGenerator(1);

    private Dictionary<Point, Chunk> _loadedChunks = new();
    private readonly Dictionary<Point, List<Entity>> _chunkEntities = new(); // 树木等

    private readonly EntitySet _mapEntitySet;
    private Entity _mapEntity => _mapEntitySet.GetEntities().ToArray().FirstOrDefault();

    public WorldGenerationSystem(World world, EntityManager entityManager, TiledMap metaMap) :
        base(world.GetEntities().With<Camera>().AsSet())
    {
        _world = world;
        _metaMap = metaMap;
        _entityManager = entityManager;
        _mapEntitySet = world.GetEntities().With<TerrainMap>().AsSet();
    }

    public Chunk BuildChunk(int x, int y)
    {
        var chunkPos = new Point(x, y);
        var terrainData = new Terrain[Const.ChunkSize, Const.ChunkSize];
        for (int i = 0; i < Const.ChunkSize; i++)
        {
            for (int j = 0; j < Const.ChunkSize; j++)
            {
                terrainData[i, j] = _terrainGenerator.GenerateChunkTerrain(chunkPos, Const.ChunkSize, i, j);
            }
        }

        var chunk = new Chunk
        {
            Position = chunkPos,
            TerrainData = terrainData
        };

        return chunk;
    }

    private static double CalculateChunkDistance(Point from, Point to)
    {
        return Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
    }

    private void UpdateChunks(Camera camera)
    {
        var cameraTile = Helper.ScreenToTileCoords(
            camera.Position.X, camera.Position.Y, _metaMap.TileWidth, _metaMap.TileHeight
        );

        var cameraChunk = new Point(
            (int)Math.Floor(cameraTile.X / Const.ChunkSize),
            (int)Math.Floor(cameraTile.Y / Const.ChunkSize)
        );
        var chunksToLoad = GetChunksInRadius(cameraChunk, Const.RenderDistance, true);
        foreach (var chunkPos in chunksToLoad)
        {
            List<Entity> entityList = new();
            var chunk = BuildChunk(chunkPos.X, chunkPos.Y);
            _loadedChunks[chunkPos] = chunk;
            for (int i = 0; i < Const.ChunkSize; i++)
            {
                for (int j = 0; j < Const.ChunkSize; j++)
                {
                    var terrain = chunk.TerrainData[i, j];
                    if (terrain.HasTree)
                    {
                        var position = new Vector2(
                            chunkPos.X * Const.ChunkSize + i + 0.5f,
                            chunkPos.Y * Const.ChunkSize + j + 0.5f
                        );
                        var entity = _entityManager.CreateTreeEntity(position);
                        entityList.Add(entity);
                    }
                }
            }
            _chunkEntities[chunkPos] = entityList;
        }

        var chunksToUnload = new List<Point>();
        foreach (var loadedChunk in _loadedChunks.Values)
        {
            double distance = CalculateChunkDistance(cameraChunk, loadedChunk.Position);
            if (distance > Const.RenderDistance)
                chunksToUnload.Add(loadedChunk.Position);
        }

        foreach (var chunkPos in chunksToUnload)
        {
            _loadedChunks.Remove(chunkPos);
            if (_chunkEntities.TryGetValue(chunkPos, out var entityList))
            {
                foreach (var entity in entityList)
                    entity.Dispose();

                _chunkEntities.Remove(chunkPos);
            }
        }

        if (chunksToLoad.Count == 0 && chunksToUnload.Count == 0)
            return;

        // 更新地图实体
        var minChunkX = int.MaxValue;
        var minChunkY = int.MaxValue;
        var maxChunkX = int.MinValue;
        var maxChunkY = int.MinValue;

        foreach (var chunk in _loadedChunks.Values)
        {
            var chunkPos = chunk.Position;
            minChunkX = Math.Min(minChunkX, chunkPos.X);
            minChunkY = Math.Min(minChunkY, chunkPos.Y);
            maxChunkX = Math.Max(maxChunkX, chunkPos.X);
            maxChunkY = Math.Max(maxChunkY, chunkPos.Y);
        }
        var minX = minChunkX * Const.ChunkSize;
        var minY = minChunkY * Const.ChunkSize;
        var maxX = (maxChunkX + 1) * Const.ChunkSize - 1;
        var maxY = (maxChunkY + 1) * Const.ChunkSize - 1;

        Console.WriteLine($"更新地图范围: ({minX},{minY}) - ({maxX},{maxY})");
        var terrainMap = new TerrainMap(minX, minY, maxX, maxY);
        foreach (var chunk in _loadedChunks.Values)
        {
            var chunkPos = chunk.Position;
            for (int i = 0; i < Const.ChunkSize; i++)
            {
                for (int j = 0; j < Const.ChunkSize; j++)
                {
                    var terrain = chunk.TerrainData[i, j];
                    int x = i + chunkPos.X * Const.ChunkSize;
                    int y = j + chunkPos.Y * Const.ChunkSize;
                    terrainMap[x, y] = terrain;
                }
            }
        }

        _mapEntity.Set(terrainMap);
    }

    /// <summary>
    /// 获取指定中心点和半径内的所有区块
    /// </summary>
    private List<Point> GetChunksInRadius(Point centerChunk, double radius, bool excludeLoaded = false)
    {
        var chunks = new List<Point>();

        for (int x = centerChunk.X - (int)Math.Ceiling(radius); x <= centerChunk.X + (int)Math.Ceiling(radius); x++)
        {
            for (int y = centerChunk.Y - (int)Math.Ceiling(radius); y <= centerChunk.Y + (int)Math.Ceiling(radius); y++)
            {
                var chunkPos = new Point(x, y);
                double distance = CalculateChunkDistance(centerChunk, chunkPos);

                if (distance <= radius && (!excludeLoaded || !_loadedChunks.ContainsKey(chunkPos)))
                {
                    chunks.Add(chunkPos);
                }
            }
        }

        return chunks;
    }

    protected override void Update(GameTime gameTime, in Entity entity)
    {
        var camera = entity.Get<Camera>();
        UpdateChunks(camera);
    }

}