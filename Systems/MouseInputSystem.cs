
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Tiled;
using XianCraft.Components;
using System.Linq;
using MonoGame.Extended;


namespace XianCraft.Systems;

public class MouseInputSystem : AComponentSystem<GameTime, Camera>
{
    private readonly World _world;

    private EntitySet _mouseInputSet;
    private Entity _mouseEntity => _mouseInputSet.GetEntities().ToArray().FirstOrDefault();
    private readonly TiledMap _metaMap;

    public MouseInputSystem(World world, TiledMap metaMap): base(world)
    {
        _world = world;
        _metaMap = metaMap;
        _mouseInputSet = _world.GetEntities().With<MouseInput>().AsSet();
    }

    protected override void Update(GameTime gameTime, ref Camera camera)
    {
        ref var mouseInput = ref _mouseEntity.Get<MouseInput>();
     
        var mouseState = Mouse.GetState();                
        var offsetX = (mouseState.X - camera.ViewportWidth / 2f) / camera.Zoom + camera.Position.X;
        var offsetY = (mouseState.Y - camera.ViewportHeight / 2f) / camera.Zoom + camera.Position.Y;

        mouseInput.Position = new Vector2(mouseState.X, mouseState.Y);
        mouseInput.WorldPosition = Helper.ScreenToTileCoords(
            offsetX, offsetY, _metaMap.TileWidth, _metaMap.TileHeight
        );
        mouseInput.LeftButton = mouseState.LeftButton == ButtonState.Pressed;
        mouseInput.RightButton = mouseState.RightButton == ButtonState.Pressed;
    }
}