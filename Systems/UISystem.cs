using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace XianCraft.Systems;

public class UISystem : AEntitySetSystem<SpriteBatch>
{
    private readonly World _world;
    private readonly SpriteFont _font;
    private Texture2D _pixelTexture;

    private static bool _showSystemInfo = false;
    private static KeyboardState _lastKeyboardState;

    public UISystem(World world, GraphicsDevice graphicsDevice, SpriteFont font) :
        base(world.GetEntities().With<DebugInfo>().AsSet())
    {
        _world = world;
        _font = font;

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
        spriteBatch.Begin();

        if (_showSystemInfo)
        {
            DrawSystemInfo(spriteBatch, debugInfo);
        }

        spriteBatch.End();
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