
using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace XianCraft.Systems;

public class StatUISystem: ISystem<SpriteBatch>
{
    private readonly World _world;
    private readonly SpriteFont _font;
    private Texture2D _pixelTexture;
    private bool _isEnabled = true;

    public StatUISystem(World world, GraphicsDevice graphicsDevice, SpriteFont font)
    {
        _world = world;
        _font = font;

        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public void Update(SpriteBatch spriteBatch)
    {
        if (!_isEnabled) return;

        spriteBatch.Begin();

        var debugInfo = BuildDebugString();
        var textSize = _font.MeasureString(debugInfo);
        var position = new Vector2(15, 15);

        // 绘制半透明背景
        var backgroundRect = new Rectangle(10, 10, (int)textSize.X + 20, (int)textSize.Y + 20);
        spriteBatch.Draw(_pixelTexture, backgroundRect, Color.Black * 0.7f);

        // 绘制边框
        var borderThickness = 2;
        // 上边框
        spriteBatch.Draw(_pixelTexture, new Rectangle(10, 10, (int)textSize.X + 20, borderThickness), Color.White * 0.8f);
        // 下边框
        spriteBatch.Draw(_pixelTexture, new Rectangle(10, (int)textSize.Y + 28, (int)textSize.X + 20, borderThickness), Color.White * 0.8f);
        // 左边框
        spriteBatch.Draw(_pixelTexture, new Rectangle(10, 10, borderThickness, (int)textSize.Y + 20), Color.White * 0.8f);
        // 右边框
        spriteBatch.Draw(_pixelTexture, new Rectangle((int)textSize.X + 28, 10, borderThickness, (int)textSize.Y + 20), Color.White * 0.8f);

        // 绘制调试文字，使用阴影效果
        // 绘制阴影
        spriteBatch.DrawString(_font, debugInfo, position + Vector2.One, Color.Black * 0.8f);
        // 绘制主文字
        spriteBatch.DrawString(_font, debugInfo, position, Color.White);

        spriteBatch.End();
    }
    
    private string BuildDebugString()
    {
        var camera = _world.GetEntities().With<CameraComponent>().AsSet().GetEntities()[0].Get<CameraComponent>();
        var mouseInput = _world.GetEntities().With<MouseInput>().AsSet().GetEntities()[0].Get<MouseInput>();

        // 内存和GC信息
        long totalMemory = GC.GetTotalMemory(false);
        var memoryInfo = GC.GetGCMemoryInfo();
        int gc0 = GC.CollectionCount(0);
        int gc1 = GC.CollectionCount(1);
        int gc2 = GC.CollectionCount(2);

        // 进程信息
        var process = System.Diagnostics.Process.GetCurrentProcess();
        long workingSet = process.WorkingSet64;
        int handleCount = process.HandleCount;

        var debugInfo = $"实体数量: {_world.GetEntities().AsSet().Count}\n" +
                        $"相机位置: {camera.Position.X:F1}, {camera.Position.Y:F1}\n" +
                        $"相机缩放: {camera.Zoom}\n" +
                        $"Viewport: {camera.ViewportWidth} x {camera.ViewportHeight}\n" +
                        $"鼠标位置: {mouseInput.Position.X:F1}, {mouseInput.Position.Y:F1}\n" +
                        $"鼠标世界位置: {mouseInput.WorldPosition.X:F1}, {mouseInput.WorldPosition.Y:F1}\n" +
                        $"\n[内存/GC]" +
                        $"\n托管堆内存: {totalMemory / 1024 / 1024:F2} MB" +
                        $"\nGC次数: Gen0={gc0}, Gen1={gc1}, Gen2={gc2}" +
                        $"\n工作集: {workingSet / 1024 / 1024:F2} MB" +
                        $"\n句柄数: {handleCount}";
        return debugInfo;
    }

    public void Dispose()
    {
    }
}