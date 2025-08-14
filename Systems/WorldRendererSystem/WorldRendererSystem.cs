using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework.Graphics;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Tiled;
using System;
using System.Linq;
using System.Collections.Generic;
using XianCraft.Renderers.Tiled;
using MonoGame.Extended;
using MonoGame.Extended.Shapes;

using MonoGame.Extended.Collisions;

using OcclusionComponent = XianCraft.Components.CollisionComponent;
using SizeF = MonoGame.Extended.SizeF;

namespace XianCraft.Systems;

public class MetaTile
{
    public class MetaTileModel
    {
        public string BitMask { get; set; } = "";
        public uint GlobalIdentifier { get; set; }
        public string Layer { get; set; }
        public Point OriginPos { get; set; } = Point.Zero;
    }

    public string Name { get; set; }
    public bool IsAutoTile { get; set; } = false;
    public MetaTileModel Default { get; set; }
    public Dictionary<string, MetaTileModel> Models { get; set; } = new();

    public MetaTileModel GetMetaTileModel(string bitMask)
    {
        if (string.IsNullOrEmpty(bitMask))
            return Default;

        return Models.TryGetValue(bitMask, out var model) ? model : Default;
    }

    public override string ToString()
    {
        return $"{Name} (Default: {Default?.GlobalIdentifier}, Models: {Models.Count})";
    }
}

public class WorldRendererSystem : AEntitySetSystem<SpriteBatch>
{
    private readonly World _world;

    private EntitySet _cameraSet;
    private EntitySet _mouseInputSet;
    private EntitySet _playerSet;

    private Entity _cameraEntity => _cameraSet.GetEntities().ToArray().FirstOrDefault();
    private Entity _mouseEntity => _mouseInputSet.GetEntities().ToArray().FirstOrDefault();
    private Entity _playerEntity => _playerSet.GetEntities().ToArray().FirstOrDefault();

    private HashSet<Point> _loadedChunks = new HashSet<Point>();

    private TiledMap _metaMap;
    private TiledMapRenderer _mapRenderer;
    private Dictionary<string, MetaTile> _metaTiles = new Dictionary<string, MetaTile>();
    private int _mapOffsetX = 0;
    private int _mapOffsetY = 0;

    private TiledMapEffect _effect;
    private EntitySet _animateRendererSet;

    public WorldRendererSystem(World world, GraphicsDevice graphicsDevice, TiledMap metaMap, Effect effect) :
        base(world.GetEntities().With<Chunk>().AsSet())
    {
        _world = world;

        _cameraSet = _world.GetEntities().With<Camera>().AsSet();
        _mouseInputSet = _world.GetEntities().With<MouseInput>().AsSet();
        _animateRendererSet = _world.GetEntities().With<Position>().With<AnimateState>().AsSet();

        _mapRenderer = new TiledMapRenderer(graphicsDevice);
        _metaMap = metaMap;
        _effect = new TiledMapEffect(effect);
        _playerSet = _world.GetEntities().With<Player>().AsSet();

        LoadMetaTiles(metaMap);
        _mapRenderer.LoadMap(_metaMap);
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
                            var objNameList = objTile.Name.Split("_");
                            var objName = objNameList[0];
                            var objBitMask = objNameList.Length > 1 ? objNameList[1] : "";

                            var model = new MetaTile.MetaTileModel
                            {
                                BitMask = objBitMask,
                                GlobalIdentifier = (uint)tile.GlobalIdentifier,
                                Layer = tileLayer.Name,
                                OriginPos = new Point(x, y)
                            };

                            if (_metaTiles.TryGetValue(objName, out var metaTile))
                            {
                                metaTile.Models[objBitMask] = model;
                                metaTile.IsAutoTile = true;
                                if (objBitMask == "")
                                    metaTile.Default = model; // 更新默认模型
                            }
                            else
                            {
                                _metaTiles[objName] = new MetaTile
                                {
                                    Name = objName,
                                    Default = model,
                                    Models = new Dictionary<string, MetaTile.MetaTileModel> { { objBitMask, model } }
                                };
                            }
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

        foreach (var tile in _metaTiles)
        {
            Console.WriteLine($"Loaded meta tile: {tile.Key} -> {tile.Value}");
        }
    }

    private string GetBitMask(Dictionary<Point, Chunk> chunkDict, Chunk chunk, int x, int y)
    {
        // 四个方向：左、下、右、上
        int[,] directions = new int[,] { { -1, 0 }, { 0, 1 }, { 1, 0 }, { 0, -1 } };
        TerrainType selfType = chunk.TerrainData[x, y].Type;
        int size = Const.ChunkSize;
        string bitMask = "";

        for (int i = 0; i < 4; i++)
        {
            int nx = x + directions[i, 0];
            int ny = y + directions[i, 1];
            bool same = false;

            if (nx >= 0 && nx < size && ny >= 0 && ny < size)
            {
                // 当前区块内
                same = chunk.TerrainData[nx, ny].Type == selfType;
            }
            else
            {
                // 跨区块，查找相邻区块
                var chunkPos = chunk.Position;
                int neighborChunkX = (int)chunkPos.X;
                int neighborChunkY = (int)chunkPos.Y;
                int localX = nx;
                int localY = ny;

                if (nx < 0) { neighborChunkX -= 1; localX = size - 1; }
                if (nx >= size) { neighborChunkX += 1; localX = 0; }
                if (ny < 0) { neighborChunkY -= 1; localY = size - 1; }
                if (ny >= size) { neighborChunkY += 1; localY = 0; }

                var neighborKey = new Point(neighborChunkX, neighborChunkY);
                if (chunkDict.TryGetValue(neighborKey, out var neighbor))
                {
                    same = neighbor.TerrainData[localX, localY].Type == selfType;
                }
            }

            bitMask += same ? "1" : "0";
        }

        return bitMask;
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
            var chunk = entity.Get<Chunk>();
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

        var mapLayers = new Dictionary<string, TiledMapTileLayer>();
        foreach (var tileLayer in _metaMap.TileLayers)
        {
            var newLayer = new TiledMapTileLayer(tileLayer.Name, tileLayer.Type, width, height, tiledMap.TileWidth, tiledMap.TileHeight);
            tiledMap.AddLayer(newLayer);
            mapLayers[tileLayer.Name] = newLayer;
        }

        var chunkDict = new Dictionary<Point, Chunk>();
        foreach (var entity in chunkEntities)
        {
            var chunk = entity.Get<Chunk>();
            var chunkPos = chunk.Position;
            chunkDict[new Point((int)chunkPos.X, (int)chunkPos.Y)] = chunk;
        }

        foreach (var entity in chunkEntities)
        {
            var chunk = entity.Get<Chunk>();
            var chunkPos = chunk.Position;
            for (int x = 0; x < Const.ChunkSize; x++)
            {
                for (int y = 0; y < Const.ChunkSize; y++)
                {
                    var terrainType = chunk.TerrainData[x, y].Type;
                    if (_metaTiles.TryGetValue(terrainType.ToString(), out var metaTile))
                    {
                        var tileX = x + (int)chunkPos.X * Const.ChunkSize + _mapOffsetX;
                        var tileY = y + (int)chunkPos.Y * Const.ChunkSize + _mapOffsetY;
                        if (metaTile.IsAutoTile)
                        {
                            // 自动瓦片处理
                            var bitMask = GetBitMask(chunkDict, chunk, x, y);
                            //Console.WriteLine($"Chunk: {chunkPos}, Tile: ({x},{y}), BitMask: {bitMask}");
                            var model = metaTile.GetMetaTileModel(bitMask);
                            if (model != null)
                            {
                                mapLayers[model.Layer]?.SetTile((ushort)tileX, (ushort)tileY, model.GlobalIdentifier);
                            }
                        }
                        else
                        {
                            // 普通瓦片处理
                            var model = metaTile.GetMetaTileModel("");
                            mapLayers[model.Layer]?.SetTile((ushort)tileX, (ushort)tileY, metaTile.Default.GlobalIdentifier);
                        }
                    }
                }
            }
        }

        foreach (var tileset in _metaMap.Tilesets)
        {
            // FIXME: 这里的 GlobalIdentifier 可能需要调整
            tiledMap.AddTileset(tileset, 1);
        }

        // 这一步是最耗时的。
        _mapRenderer.LoadMap(tiledMap);

        stopwatch.Stop();
        Console.WriteLine($"BuildTiledMap 耗时: {stopwatch.ElapsedMilliseconds} ms {tiledMap.Width}x{tiledMap.Height} tiles, {chunkEntities.Length} chunks");
    }

    public void SyncChunks(ReadOnlySpan<Entity> chunkEntities)
    {
        var currentChunks = new HashSet<Point>();
        foreach (var entity in chunkEntities)
        {
            var chunk = entity.Get<Chunk>();
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

        var camera = _cameraEntity.Get<Camera>();

        DrawMap(camera);

        spriteBatch.Begin(
            SpriteSortMode.FrontToBack,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );

        DrawEntities(spriteBatch, camera);
        DrawMouse(spriteBatch, camera);

        spriteBatch.End();
    }

    private void DrawMap(Camera camera)
    {
        var mapOffset = Helper.TileToScreenCoords(
            _mapOffsetX, _mapOffsetY,
            _metaMap.TileWidth, _metaMap.TileHeight
        );
        var viewportOffset = new Vector2(
            camera.ViewportWidth / 2f,
            camera.ViewportHeight / 2f - _metaMap.TileHeight / 2f // 这个渲染不支持 tiled drawOffset属性, 我们手动调整一下
        );
        Matrix viewMatrix2 =
            Matrix.CreateTranslation(new Vector3(-camera.Position - mapOffset + viewportOffset, 0.0f)) *
            Matrix.CreateScale(camera.Zoom, camera.Zoom, 1) *
            Matrix.CreateTranslation(new Vector3(
                (1 - camera.Zoom) * camera.ViewportWidth / 2, (1 - camera.Zoom) * camera.ViewportHeight / 2, 0.0f));

        Matrix projectionMatrix2 = Matrix.CreateOrthographicOffCenter(0f, camera.ViewportWidth, camera.ViewportHeight, 0f, 0f, -1f);
        _mapRenderer.Draw(ref viewMatrix2, ref projectionMatrix2, _effect);
    }

    private bool TexturesIntersect(Entity a, Entity b)
    {
        var aBox = GetBoundingBox(a);
        var bBox = GetBoundingBox(b);

        if (!aBox.Intersects(bBox))
            return false;

        var aState = a.Get<AnimateState>();
        var bState = b.Get<AnimateState>();

        var aTex = aState.CurrentAnimation.TextureRegion.Texture;
        var bTex = bState.CurrentAnimation.TextureRegion.Texture;

        // 计算实际在大纹理集中的区域
        var aRegion = new Rectangle(
            aState.SourceRectangle.X + aState.CurrentAnimation.TextureRegion.Bounds.X,
            aState.SourceRectangle.Y + aState.CurrentAnimation.TextureRegion.Bounds.Y,
            aState.SourceRectangle.Width,
            aState.SourceRectangle.Height
        );
        var bRegion = new Rectangle(
            bState.SourceRectangle.X + bState.CurrentAnimation.TextureRegion.Bounds.X,
            bState.SourceRectangle.Y + bState.CurrentAnimation.TextureRegion.Bounds.Y,
            bState.SourceRectangle.Width,
            bState.SourceRectangle.Height
        );

        // 计算交集区域（世界坐标）
        var intersect = aBox.Intersection(bBox);
        if (intersect.Width <= 0 || intersect.Height <= 0)
            return false;

        // 读取像素数据
        Color[] aData = new Color[aRegion.Width * aRegion.Height];
        Color[] bData = new Color[bRegion.Width * bRegion.Height];
        aTex.GetData(0, aRegion, aData, 0, aData.Length);
        bTex.GetData(0, bRegion, bData, 0, bData.Length);

        // 遍历交集区域
        for (int y = 0; y < intersect.Height; y++)
        {
            for (int x = 0; x < intersect.Width; x++)
            {
                int ax = (int)(intersect.X - aBox.X + x);
                int ay = (int)(intersect.Y - aBox.Y + y);
                int bx = (int)(intersect.X - bBox.X + x);
                int by = (int)(intersect.Y - bBox.Y + y);

                if (ax < 0 || ay < 0 || bx < 0 || by < 0 ||
                    ax >= aRegion.Width || ay >= aRegion.Height ||
                    bx >= bRegion.Width || by >= bRegion.Height)
                    continue;

                Color aPixel = aData[ay * aRegion.Width + ax];
                Color bPixel = bData[by * bRegion.Width + bx];

                if (aPixel.A > 0 && bPixel.A > 0)
                    return true;
            }
        }
        return false;
    }

    private RectangleF GetBoundingBox(Entity entity)
    {
        ref var position = ref entity.Get<Position>();
        ref var animateState = ref entity.Get<AnimateState>();

        var sourceRect = animateState.SourceRectangle;
        var origin = animateState.Origin;

        var worldScreenPos = Helper.TileToScreenCoords(
            position.Value.X, position.Value.Y,
            _metaMap.TileWidth, _metaMap.TileHeight
        );

        return new RectangleF(
            worldScreenPos.X + origin.X,
            worldScreenPos.Y + origin.Y,
            sourceRect.Width,
            sourceRect.Height
        );
    }

    private void DrawEntities(SpriteBatch spriteBatch, Camera camera)
    {
        var playerBoundingBox = GetBoundingBox(_playerEntity);
        var playerPos = _playerEntity.Get<Position>();

        foreach (var entity in _animateRendererSet.GetEntities())
        {
            ref var animateState = ref entity.Get<AnimateState>();
            ref var position = ref entity.Get<Position>();

            if (animateState.CurrentAnimation == null)
                continue;

            var sprite = animateState.CurrentAnimation;
            var sourceRect = animateState.SourceRectangle;
            var origin = animateState.Origin;
            var transparency = 1f;

            if (animateState.EntityName == "tree")
            {
                var treeBoundingBox = GetBoundingBox(entity);
                if (treeBoundingBox.Intersects(playerBoundingBox))
                    if ((playerPos.Value.X + playerPos.Value.Y) < (position.Value.X + position.Value.Y))
                        if (TexturesIntersect(entity, _playerEntity))
                            transparency = 0.5f;
            }

            var scaledWidth = (int)(sourceRect.Width * camera.Zoom);
            var scaledHeight = (int)(sourceRect.Height * camera.Zoom);

            var worldScreenPos = Helper.TileToScreenCoords(
                position.Value.X, position.Value.Y,
                _metaMap.TileWidth, _metaMap.TileHeight
            );

            var relPos = (worldScreenPos + origin - camera.Position) * camera.Zoom;
            spriteBatch.Draw(
                sprite.TextureRegion.Texture,
                new Rectangle(
                    (int)(camera.ViewportWidth / 2f - scaledWidth / 2 + relPos.X),
                    (int)(camera.ViewportHeight / 2f - scaledHeight / 2 + relPos.Y),
                    scaledWidth,
                    scaledHeight
                ),
                new Rectangle(
                    sourceRect.X + sprite.TextureRegion.Bounds.X,
                    sourceRect.Y + sprite.TextureRegion.Bounds.Y,
                    sourceRect.Width,
                    sourceRect.Height
                ),
                sprite.Color * sprite.Transparency * transparency,
                sprite.Rotation,
                sprite.Origin,
                sprite.SpriteEffects,
                Math.Clamp((position.Value.X + position.Value.Y + 1) / 2000f, 0f, 1f)
            );

            // draw sprite rectangle for debugging
            /*
            spriteBatch.DrawRectangle(
                new Rectangle(
                    (int)(camera.ViewportWidth / 2f - scaledWidth / 2 + relPos.X),
                    (int)(camera.ViewportHeight / 2f - scaledHeight / 2 + relPos.Y),
                    scaledWidth,
                    scaledHeight
                ),
                Color.Red,
                1, 1f
            );
            */

            var outline = BuildTileOutline(position.Value.X, position.Value.Y, camera);
            spriteBatch.DrawPolygon(Vector2.Zero, outline, Color.White, 2f,
                Math.Clamp((position.Value.X + position.Value.Y + 1) / 2000f, 0f, 1f) * 0.99f
            );
        }
    }

    private Polygon BuildTileOutline(float worldX, float worldY, Camera camera)
    {
        var worldScreenPos = Helper.TileToScreenCoords(
            worldX, worldY,
            _metaMap.TileWidth, _metaMap.TileHeight
        );
        var relPos = (worldScreenPos - camera.Position) * camera.Zoom;

        float halfWidth = _metaMap.TileWidth * camera.Zoom / 2f;
        float halfHeight = _metaMap.TileHeight * camera.Zoom / 2f;

        var center = new Vector2(
           camera.ViewportWidth / 2f + relPos.X,
           camera.ViewportHeight / 2f + relPos.Y
        );
        Vector2[] diamond =
        [
            new Vector2(center.X, center.Y - halfHeight), // 上
            new Vector2(center.X + halfWidth, center.Y),  // 右
            new Vector2(center.X, center.Y + halfHeight), // 下
            new Vector2(center.X - halfWidth, center.Y),  // 左
        ];

        return new Polygon(diamond);
    }

    private void DrawMouse(SpriteBatch spriteBatch, Camera camera)
    {
        var mouseInput = _mouseEntity.Get<MouseInput>();

        int tileX = (int)Math.Floor(mouseInput.WorldPosition.X);
        int tileY = (int)Math.Floor(mouseInput.WorldPosition.Y);

        var polygon = BuildTileOutline(tileX + 0.5f, tileY + 0.5f, camera);
        spriteBatch.DrawPolygon(Vector2.Zero, polygon, Color.Yellow, 3f);
    }
}