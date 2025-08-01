using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework.Graphics;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using System;
using System.Linq;
using System.Collections.Generic;
using MonoGame.Extended;

namespace XianCraft.Systems;

public class MetaTile
{
    public string Name { get; set; }
    public uint GlobalIdentifier { get; set; }
    public string Layer { get; set; }
}

public class WorldRendererSystem : AEntitySetSystem<SpriteBatch>
{
    private readonly World _world;

    private EntitySet _cameraSet;
    private Entity _cameraEntity => _cameraSet.GetEntities().ToArray().FirstOrDefault();
    private HashSet<Point> _loadedChunks = new HashSet<Point>();

    private TiledMap _metaMap;
    private TiledMapRenderer _mapRenderer;
    private Dictionary<string, MetaTile> _metaTiles = new Dictionary<string, MetaTile>();
    private int _mapOffsetX = 0;
    private int _mapOffsetY = 0;

    private TiledMapEffect _effect;

    public WorldRendererSystem(World world, GraphicsDevice graphicsDevice, TiledMap metaMap, Effect effect) :
        base(world.GetEntities().With<ChunkComponent>().AsSet())
    {
        _world = world;
        _cameraSet = _world.GetEntities().With<CameraComponent>().AsSet();
        _mapRenderer = new TiledMapRenderer(graphicsDevice);
        _metaMap = metaMap;
        _effect = new TiledMapEffect(effect);

        LoadMetaTiles(metaMap);
    }

    private void LoadMetaTiles(TiledMap metaMap)
    {
        var defLayer = metaMap.GetLayer<TiledMapObjectLayer>("Definitions");
        foreach (var objTile in defLayer.Objects)
        {
            // Tiled 给的坐标比较怪，直接用这个进行转换, 时间出真知。
            var x = (int)Math.Floor(objTile.Position.X / metaMap.TileHeight);
            var y = (int)Math.Floor(objTile.Position.Y / metaMap.TileHeight);

            foreach (var tileLayer in metaMap.TileLayers)
            {
                for (int i = 0; i < tileLayer.Tiles.Length; i++)
                {
                    var tile = tileLayer.Tiles[i];
                    if (tile.X == x && tile.Y == y)
                    {
                        if (tile.GlobalIdentifier > 0)
                        {
                            var metaTile = new MetaTile
                            {
                                Name = objTile.Name,
                                GlobalIdentifier = (uint)tile.GlobalIdentifier,
                                Layer = tileLayer.Name
                            };
                            _metaTiles[objTile.Name] = metaTile;
                            break;
                        }
                    }
                }
            }
        }

        // 检查所有 TerrainType 是否都已加载
        var missingTypes = new List<string>();
        foreach (var type in Enum.GetValues(typeof(TerrainType)))
        {
            string name = type.ToString();
            if (!_metaTiles.ContainsKey(name))
                missingTypes.Add(name);
        }

        if (missingTypes.Count > 0)
            throw new Exception($"Missing meta tiles for TerrainType: {string.Join(", ", missingTypes)}");
    }

    public void BuildTiledMap(ReadOnlySpan<Entity> chunkEntities)
    {

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;
        foreach (var entity in chunkEntities)
        {
            var chunk = entity.Get<ChunkComponent>();
            var chunkPos = chunk.Position;
            minX = Math.Min(minX, (int)chunkPos.X);
            minY = Math.Min(minY, (int)chunkPos.Y);
            maxX = Math.Max(maxX, (int)chunkPos.X);
            maxY = Math.Max(maxY, (int)chunkPos.Y);
        }

        _mapOffsetX = -minX * Const.ChunkSize;
        _mapOffsetY = -minY * Const.ChunkSize;
        var width = (maxX - minX + 1) * Const.ChunkSize;
        var height = (maxY - minY + 1) * Const.ChunkSize;

        var tiledMap = new TiledMap(
            _metaMap.Name, _metaMap.Type,
            width, height,
            _metaMap.TileWidth, _metaMap.TileHeight,
            _metaMap.RenderOrder, _metaMap.Orientation, _metaMap.BackgroundColor
        );

        foreach (var tileLayer in _metaMap.TileLayers)
        {
            var newLayer = new TiledMapTileLayer(tileLayer.Name, tileLayer.Type, width, height, tiledMap.TileWidth, tiledMap.TileHeight);
            foreach (var entity in chunkEntities)
            {
                var chunk = entity.Get<ChunkComponent>();
                var chunkPos = chunk.Position;
                for (int x = 0; x < Const.ChunkSize; x++)
                {
                    for (int y = 0; y < Const.ChunkSize; y++)
                    {
                        var terrainType = chunk.TerrainData[x, y];
                        if (_metaTiles.TryGetValue(terrainType.ToString(), out var metaTile))
                        {
                            if (metaTile.Layer != tileLayer.Name)
                                continue; // 只添加当前层的瓦片
                            var tileX = x + (int)chunkPos.X * Const.ChunkSize + _mapOffsetX;
                            var tileY = y + (int)chunkPos.Y * Const.ChunkSize + _mapOffsetY;
                            newLayer.SetTile((ushort)tileX, (ushort)tileY, metaTile.GlobalIdentifier);
                        }
                    }
                }
            }
            tiledMap.AddLayer(newLayer);
        }

        foreach (var tileset in _metaMap.Tilesets)
        {
            // FIXME: 这里的 GlobalIdentifier 可能需要调整
            tiledMap.AddTileset(tileset, 0);
        }

        // 这一步是最耗时的。
        _mapRenderer.LoadMap(tiledMap);

        stopwatch.Stop();
        Console.WriteLine($"BuildTiledMap 耗时: {stopwatch.ElapsedMilliseconds} ms");
    }

    public void SyncChunks(ReadOnlySpan<Entity> chunkEntities)
    {
        var currentChunks = new HashSet<Point>();
        foreach (var entity in chunkEntities)
        {
            var chunk = entity.Get<ChunkComponent>();
            var chunkPos = new Point((int)chunk.Position.X, (int)chunk.Position.Y);
            currentChunks.Add(chunkPos);
        }

        if (currentChunks.SetEquals(_loadedChunks))
            return; // 没有变化

        _loadedChunks = currentChunks;
        BuildTiledMap(chunkEntities);
    }

    protected override void Update(SpriteBatch spriteBatch, ReadOnlySpan<Entity> chunkEntities)
    {
        SyncChunks(chunkEntities);

        var camera = _cameraEntity.Get<CameraComponent>();

        var mapOffset = Helper.TileToScreenCoords(
            _mapOffsetX, _mapOffsetY,
            _metaMap.TileWidth, _metaMap.TileHeight
        );
        var viewportOffset = new Vector2(
            camera.ViewportWidth / 2f,
            camera.ViewportHeight / 2f - 9f * _metaMap.TileHeight
        );
        Matrix viewMatrix2 =
            Matrix.CreateTranslation(new Vector3(-camera.Position - mapOffset + viewportOffset, 0.0f)) *
            Matrix.CreateScale(camera.Zoom, camera.Zoom, 1) *
            Matrix.CreateTranslation(new Vector3(
                (1 - camera.Zoom) * camera.ViewportWidth / 2, (1 - camera.Zoom) * camera.ViewportHeight / 2, 0.0f));

        Matrix projectionMatrix2 = Matrix.CreateOrthographicOffCenter(0f, camera.ViewportWidth, camera.ViewportHeight, 0f, 0f, -1f);
        _mapRenderer.Draw(ref viewMatrix2, ref projectionMatrix2, _effect);

        spriteBatch.Begin();
        spriteBatch.DrawCircle(
            new Vector2(camera.ViewportWidth / 2f, camera.ViewportHeight / 2f),
            5f,
            32,
            Color.Red
        );
        spriteBatch.End();
    }
}