using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using XianCraft.Utils;

namespace XianCraft.Systems;

public class UISystem : AEntitySetSystem<SpriteBatch>
{
    private readonly World _world;
    private readonly SpriteFont _font;
    private Texture2D _pixelTexture;
    private readonly GraphicsDevice _graphicsDevice; // 新增

    private static bool _showSystemInfo = false;
    private static KeyboardState _lastKeyboardState;

    public UISystem(World world, GraphicsDevice graphicsDevice, SpriteFont font) :
        base(world.GetEntities().With<GlobalState>().With<DebugInfo>().AsSet())
    {
        _world = world;
        _font = font;
        _graphicsDevice = graphicsDevice; // 保存

        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }


    protected override void Update(SpriteBatch spriteBatch, in Entity entity)
    {
        // 检查 F1 是否被按下
        var keyboardState = Keyboard.GetState();
        if (keyboardState.IsKeyDown(Keys.F1) && !_lastKeyboardState.IsKeyDown(Keys.F1))
        {
            _showSystemInfo = !_showSystemInfo;
        }
        _lastKeyboardState = keyboardState;

        var debugInfo = entity.Get<DebugInfo>();
        var globalState = entity.Get<GlobalState>(); // 获取全局状态
        spriteBatch.Begin();

        // 始终显示时间
        DrawWorldTime(spriteBatch, ref globalState);

        if (_showSystemInfo)
        {
            DrawSystemInfo(spriteBatch, debugInfo);
        }

        spriteBatch.End();
    }
    
    private void DrawWorldTime(SpriteBatch spriteBatch, ref GlobalState globalState)
    {
        // 假设 globalState.Clock.Now 为 DateTime（依据其它代码使用方式）
        var now = globalState.Clock.Now;
        // 计算游戏内的“天数”（可按需要修改）
        int day = now.Day; // 若不合适，可换成 (int)now.Subtract(gameStart).TotalDays + 1
        int hour = now.Hour;
        int minute = now.Minute;

        // 原先使用符号(☀/🌙)，改为纯中文且不含符号
        var timeText = $"第{day}天  {hour:00}:{minute:00}\n";
        timeText += $"光照参数:{LightingUtil.GetLightFactor(now):F2}"; // 显示时间缩放

        var margin = 12;
        var textSize = _font.MeasureString(timeText);
        int x = _graphicsDevice.Viewport.Width - (int)textSize.X - margin - 16;
        int y = margin;

        var bgRect = new Rectangle(x - 8, y - 6, (int)textSize.X + 16, (int)textSize.Y + 12);

        // 半透明背景
        spriteBatch.Draw(_pixelTexture, bgRect, new Color(0f, 0f, 0f, 0.55f));
        // 细边框
        spriteBatch.Draw(_pixelTexture, new Rectangle(bgRect.X, bgRect.Y, bgRect.Width, 2), Color.White * 0.25f);
        spriteBatch.Draw(_pixelTexture, new Rectangle(bgRect.X, bgRect.Bottom - 2, bgRect.Width, 2), Color.White * 0.25f);
        spriteBatch.Draw(_pixelTexture, new Rectangle(bgRect.X, bgRect.Y, 2, bgRect.Height), Color.White * 0.25f);
        spriteBatch.Draw(_pixelTexture, new Rectangle(bgRect.Right - 2, bgRect.Y, 2, bgRect.Height), Color.White * 0.25f);

        // 阴影
        var pos = new Vector2(x, y);
        spriteBatch.DrawString(_font, timeText, pos + new Vector2(1,1), Color.Black * 0.8f);
        // 主文字（可根据昼夜调颜色）
        var tint = hour >= 6 && hour < 18 ? Color.White : new Color(0.75f, 0.85f, 1f);
        spriteBatch.DrawString(_font, timeText, pos, tint);
    }

    private void DrawSystemInfo(SpriteBatch spriteBatch, DebugInfo debugInfo)
    {

        var systemInfo = debugInfo.SystemInfo;
        var textSize = _font.MeasureString(systemInfo);
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
        spriteBatch.DrawString(_font, systemInfo, position + Vector2.One, Color.Black * 0.8f);
        // 绘制主文字
        spriteBatch.DrawString(_font, systemInfo, position, Color.White);
    }


}