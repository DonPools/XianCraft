
using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;

namespace XianCraft.Systems;

public class CameraSystem : AComponentSystem<GameTime, Camera>
{
    private readonly GraphicsDevice _graphicsDevice;
    private EntitySet _targetSet;
    private TiledMap _metaMap;

    public CameraSystem(World world, GraphicsDevice graphicsDevice, TiledMap metaMap) : base(world)
    {
        _graphicsDevice = graphicsDevice;
        _metaMap = metaMap;
        _targetSet = world.GetEntities().With<Player>().With<Position>().AsSet();
    }

    protected override void Update(GameTime gameTime, ref Camera camera)
    {
        var keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(Keys.Q))
            camera.Zoom *= 1.01f; // 放大
        if (keyboardState.IsKeyDown(Keys.E))
            camera.Zoom *= 0.99f; // 缩小

        var entity = _targetSet.GetEntities()[0];
        if (entity.IsAlive)
        {
            ref var position = ref entity.Get<Position>();
            camera.Position = Helper.WorldToAbsScreenCoords(
                position.Value.X, position.Value.Y,
                _metaMap.TileWidth, _metaMap.TileHeight
            );
        }
        
        camera.ViewportWidth = _graphicsDevice.Viewport.Width;
        camera.ViewportHeight = _graphicsDevice.Viewport.Height;
    }
}