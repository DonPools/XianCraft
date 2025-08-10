
using System;
using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using MonoGame.Extended.Tiled;

namespace XianCraft.Systems;

public class WorldGenerationSystem : AEntitySetSystem<GameTime>
{
    private readonly World _world;
    private TiledMap _metaMap;

    private TerrainGenerator _terrainGenerator = new TerrainGenerator(1);

    private readonly HashSet<Point> _loadedChunks = new();
    private readonly Dictionary<Point, List<Entity>> _chunkEntities = new();
    private EntityManager _entityManager;

    public WorldGenerationSystem(World world, EntityManager entityManager, TiledMap metaMap) :
        base(world.GetEntities().With<Camera>().AsSet())
    {
        _world = world;
        _metaMap = metaMap;
        _entityManager = entityManager;
    }

    public List<Entity> BuildChunk(int x, int y)
    {
        var enityList = new List<Entity>();
        var chunkPos = new Point(x, y);        
        var terrainData = new Terrain[Const.ChunkSize, Const.ChunkSize];
        for (int i = 0; i < Const.ChunkSize; i++)
        {
            for (int j = 0; j < Const.ChunkSize; j++)
            {
                terrainData[i, j] = _terrainGenerator.GenerateChunkTerrain(chunkPos, Const.ChunkSize, i, j);                
            }
        }

        var entity = _world.CreateEntity();        
        entity.Set(new Chunk{
            Position = chunkPos,
            TerrainData = terrainData
        });
        enityList.Add(entity);

        for (int i = 0; i < Const.ChunkSize; i++)
        {
            for (int j = 0; j < Const.ChunkSize; j++)
            {
                var terrain = terrainData[i, j];
                if (terrain.HasTree)
                {
                    var position = new Vector2(
                        chunkPos.X * Const.ChunkSize + i,
                        chunkPos.Y * Const.ChunkSize + j
                    );
                    var treeEntity = _entityManager.CreateTreeEntity(position);
                    enityList.Add(treeEntity);
                }
            }
        }

        return enityList;
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
            var entity = BuildChunk(chunkPos.X, chunkPos.Y);
            _loadedChunks.Add(chunkPos);
            _chunkEntities[chunkPos] = entity;
        }

        var chunksToUnload = new List<Point>();
        foreach (var loadedChunk in _loadedChunks)
        {
            double distance = CalculateChunkDistance(cameraChunk, loadedChunk);
            if (distance > Const.RenderDistance)
            {
                chunksToUnload.Add(loadedChunk);
            }
        }
        
        foreach (var chunkPos in chunksToUnload)
        {
            if (_chunkEntities.TryGetValue(chunkPos, out var entityList))
            {
                foreach (var entity in entityList)
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

    protected override void Update(GameTime gameTime, in Entity entity)
    {
        var camera = entity.Get<Camera>();
        UpdateChunks(camera);
    }
    
}