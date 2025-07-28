
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Tiled;
using XianCraft.Components;


namespace XianCraft.Systems;

public class MouseInputSystem : ISystem<float>
{
    private readonly World _world;
    private readonly Entity _mouseEntity;
    private readonly Entity _cameraEntity;
    private readonly TiledMap _metaMap;
    private bool _isEnabled = true;

    public MouseInputSystem(World world, TiledMap metaMap)
    {
        _world = world;
        _metaMap = metaMap;
        _mouseEntity = _world.GetEntities().With<MouseInput>().AsSet().GetEntities()[0];
        _cameraEntity = _world.GetEntities().With<CameraComponent>().AsSet().GetEntities()[0];
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public void Update(float deltaTime)
    {

        var mouseState = Mouse.GetState();
        ref var mouseInput = ref _mouseEntity.Get<MouseInput>();
        var camera = _cameraEntity.Get<CameraComponent>();
        var offsetX = (mouseState.X - camera.ViewportWidth / 2f) / camera.Zoom + camera.Position.X;
        var offsetY = (mouseState.Y - camera.ViewportHeight / 2f) / camera.Zoom + camera.Position.Y;

        mouseInput.Position = new Vector2(mouseState.X, mouseState.Y);
        mouseInput.WorldPosition = Helper.ScreenToTileCoords(
            offsetX, offsetY, _metaMap.TileWidth, _metaMap.TileHeight
        );
        mouseInput.LeftButton = mouseState.LeftButton == ButtonState.Pressed;
        mouseInput.RightButton = mouseState.RightButton == ButtonState.Pressed;
    }

    public void Dispose()
    {
    }
}