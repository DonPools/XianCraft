
using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;

namespace XianCraft.Systems;

public class CameraSystem : AEntitySetSystem<float>
{
    private readonly GraphicsDevice _graphicsDevice;

    public CameraSystem(World world, GraphicsDevice graphicsDevice) :
        base(world.GetEntities().With<CameraComponent>().AsSet())
    {
        _graphicsDevice = graphicsDevice;
    }


    protected override void Update(float deltaTime, in Entity entity)
    {
        var keyboardState = Keyboard.GetState();
        ref var camera = ref entity.Get<CameraComponent>();

        Vector2 movement = Vector2.Zero;
        if (keyboardState.IsKeyDown(Keys.W))
            movement.Y -= 1;
        if (keyboardState.IsKeyDown(Keys.S))
            movement.Y += 1;
        if (keyboardState.IsKeyDown(Keys.A))
            movement.X -= 1;
        if (keyboardState.IsKeyDown(Keys.D))
            movement.X += 1;

        if (keyboardState.IsKeyDown(Keys.Q))
            camera.Zoom *= 1.01f; // 放大
        if (keyboardState.IsKeyDown(Keys.E))
            camera.Zoom *= 0.99f; // 缩小

        if (movement != Vector2.Zero)
        {
            movement.Normalize();
            camera.Position += movement * 100f * deltaTime / camera.Zoom; // 移动速度为 100单位/秒
        }

        camera.ViewportWidth = _graphicsDevice.Viewport.Width;
        camera.ViewportHeight = _graphicsDevice.Viewport.Height;
    }
}