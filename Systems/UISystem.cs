using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;

namespace XianCraft.Systems;

public class UISystem : AEntitySetSystem<SpriteBatch>
{
    private readonly World _world;
    private readonly SpriteFont _font;
    private Texture2D _pixelTexture;

    private EntitySet _cameraSet;
    private EntitySet _mouseInputSet;
    private EntitySet _chunkSet;
    private Entity _cameraEntity => _cameraSet.GetEntities().ToArray().FirstOrDefault();
    private Entity _mouseEntity => _mouseInputSet.GetEntities().ToArray().FirstOrDefault();

    public UISystem(World world, GraphicsDevice graphicsDevice, SpriteFont font) :
        base(world.GetEntities().AsSet())
    {
        _world = world;
        _font = font;

        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _cameraSet = _world.GetEntities().With<Camera>().AsSet();
        _mouseInputSet = _world.GetEntities().With<MouseInput>().AsSet();
        _chunkSet = _world.GetEntities().With<Chunk>().AsSet();
    }


    protected override void Update(SpriteBatch spriteBatch, ReadOnlySpan<Entity> entities)
    {

        var camera = _cameraEntity.Get<Camera>();
        var mouseInput = _mouseEntity.Get<MouseInput>();

        spriteBatch.Begin();

        var debugInfo = BuildDebugString(camera, mouseInput, entities);
        var textSize = _font.MeasureString(debugInfo);
        var position = new Vector2(15, 15);

        // 绘制半透明背景
        var backgroundRect = new Rectangle(10, 10, (int)textSize.X + 20, (int)textSize.Y + 20);
        spriteBatch.Draw(_pixelTexture, backgroundRect, Color.Black * 0.7f);

        // 绘制边框
        var borderThickness = 2;
        spriteBatch.Draw(_pixelTexture, new Rectangle(10, 10, (int)textSize.X + 20, borderThickness), Color.White * 0.8f);
        spriteBatch.Draw(_pixelTexture, new Rectangle(10, (int)textSize.Y + 28, (int)textSize.X + 20, borderThickness), Color.White * 0.8f);
        spriteBatch.Draw(_pixelTexture, new Rectangle(10, 10, borderThickness, (int)textSize.Y + 20), Color.White * 0.8f);
        spriteBatch.Draw(_pixelTexture, new Rectangle((int)textSize.X + 28, 10, borderThickness, (int)textSize.Y + 20), Color.White * 0.8f);

        // 绘制调试文字，使用阴影效果
        // 绘制阴影
        spriteBatch.DrawString(_font, debugInfo, position + Vector2.One, Color.Black * 0.8f);
        // 绘制主文字
        spriteBatch.DrawString(_font, debugInfo, position, Color.White);

        spriteBatch.End();
    }

    private string BuildDebugString(Camera camera, MouseInput mouseInput, ReadOnlySpan<Entity> entities)
    { 
        // 内存和GC信息
        long totalMemory = GC.GetTotalMemory(false);
        int gc0 = GC.CollectionCount(0);
        int gc1 = GC.CollectionCount(1);
        int gc2 = GC.CollectionCount(2);

        // 进程信息
        var process = System.Diagnostics.Process.GetCurrentProcess();
        long workingSet = process.WorkingSet64;
        int handleCount = process.HandleCount;

        var debugInfo = $"[内存/GC]" +
                        $"\n托管堆内存: {totalMemory / 1024 / 1024:F2} MB" +
                        $"\nGC次数: Gen0={gc0}, Gen1={gc1}, Gen2={gc2}" +
                        $"\n工作集: {workingSet / 1024 / 1024:F2} MB" +
                        $"\n句柄数: {handleCount}";

        debugInfo += $"\n\n[统计]\n" +
                    $"实体数量: {entities.Length}\n" +
                    $"相机位置: {camera.Position.X:F1}, {camera.Position.Y:F1}\n" +
                    $"相机缩放: {camera.Zoom}\n" +
                    $"Viewport: {camera.ViewportWidth} x {camera.ViewportHeight}\n" +
                    $"鼠标位置: {mouseInput.Position.X:F1}, {mouseInput.Position.Y:F1}\n" +
                    $"鼠标世界位置: {mouseInput.WorldPosition.X:F1}, {mouseInput.WorldPosition.Y:F1}\n";

        var worldX = (int)Math.Floor(mouseInput.WorldPosition.X);
        var worldY = (int)Math.Floor(mouseInput.WorldPosition.Y);
        
        int FloorDiv(int a, int b) => (a >= 0) ? (a / b) : ((a - b + 1) / b);
        var chunkX = FloorDiv(worldX, Const.ChunkSize);
        var chunkY = FloorDiv(worldY, Const.ChunkSize);
        var x = ((worldX % Const.ChunkSize) + Const.ChunkSize) % Const.ChunkSize;
        var y = ((worldY % Const.ChunkSize) + Const.ChunkSize) % Const.ChunkSize;

        var chunkEntities = _chunkSet.GetEntities();

        debugInfo += $"鼠标所在区块: {chunkX}, {chunkY} ({x}, {y})\n";

        foreach (var entity in chunkEntities)
        {
            var chunk = entity.Get<Chunk>();
            //Console.WriteLine($"Chunk: {chunk.Position.X}, {chunk.Position.Y} ({x}, {y})");
            if (chunk.Position.X == chunkX && chunk.Position.Y == chunkY)
            {
                debugInfo += $"地形类型: {chunk.TerrainData[x, y].Type}";
                break;
            }
        }

        return debugInfo;
    }
}