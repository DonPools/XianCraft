
using System;
using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using MonoGame.Extended.Tiled;
using System.Linq;

namespace XianCraft.Systems;

public class WorldGenerationSystem : AEntitySetSystem<float>
{
    private readonly World _world;
    private TiledMap _metaMap;
    private TerrainGenerator _terrainGenerator = new TerrainGenerator();

    private readonly HashSet<Point> _loadedChunks = new HashSet<Point>();
    private readonly Dictionary<Point, Entity> _chunkEntities = new Dictionary<Point, Entity>();

    public WorldGenerationSystem(World world, TiledMap metaMap):
        base(world.GetEntities().With<CameraComponent>().AsSet())
    {
        _world = world;
        _metaMap = metaMap;
    }

    public Entity BuildChunk(int x, int y)
    {
        var chunkPos = new Point(x, y);        
        var terrainData = new TerrainType[Const.ChunkSize, Const.ChunkSize];
        for (int i = 0; i < Const.ChunkSize; i++)
        {
            for (int j = 0; j < Const.ChunkSize; j++)
            {
                terrainData[i, j] = _terrainGenerator.GenerateChunkTerrain(chunkPos, Const.ChunkSize, i, j);                
            }
        }

        var entity = _world.CreateEntity();
        entity.Set(new ChunkComponent(chunkPos, terrainData));

        return entity;
    }

    private static double CalculateChunkDistance(Point from, Point to)
    {
        return Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
    }

    private void UpdateChunks(CameraComponent camera)
    {
        var cameraTile = Helper.ScreenToTileCoords(
            camera.Position.X, camera.Position.Y, _metaMap.TileWidth, _metaMap.TileHeight
        );

        var cameraChunk = new Point(
            (int)Math.Floor(cameraTile.X / Const.ChunkSize),
            (int)Math.Floor(cameraTile.Y / Const.ChunkSize)
        );
        var chunksToLoad = GetChunksInRadius(cameraChunk, 4, true);
        foreach (var chunkPos in chunksToLoad)
        {
            var entity = BuildChunk(chunkPos.X, chunkPos.Y);
            _loadedChunks.Add(chunkPos);
            _chunkEntities[chunkPos] = entity;
        }

        var chunksToUnload = new List<Point>();
        foreach (var loadedChunk in _loadedChunks)
        {
            double distance = CalculateChunkDistance(cameraChunk, loadedChunk);
            if (distance > 4)
            {
                chunksToUnload.Add(loadedChunk);
            }
        }
        
        foreach (var chunkPos in chunksToUnload)
        {
            if (_chunkEntities.TryGetValue(chunkPos, out var entity))
            {
                entity.Dispose();
                _loadedChunks.Remove(chunkPos);
                _chunkEntities.Remove(chunkPos);
            }            
        }
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

                if (distance <= radius && (!excludeLoaded || !_loadedChunks.Contains(chunkPos)))
                {
                    chunks.Add(chunkPos);
                }
            }
        }

        return chunks;
    }

    protected override void Update(float deltaTime, in Entity entity)
    {
        var camera = entity.Get<CameraComponent>();
        UpdateChunks(camera);
    }
    
}