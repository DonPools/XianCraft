
using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;

namespace XianCraft.Systems;

public class WorldGenerationSystem : ISystem<float>
{
    private readonly World _world;
    private bool _isEnabled = true;
    private Entity _cameraEntity;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public WorldGenerationSystem(World world)
    {
        _world = world;
        _cameraEntity = _world.GetEntities().With<CameraComponent>().AsSet().GetEntities()[0];

        _world.CreateEntity()
            .Set(new ChunkComponent(new Vector2(0, 0), new TerrainType[16, 16]));
    }

    public void Update(float deltaTime)
    {
        var camera = _cameraEntity.Get<CameraComponent>();        
    }

    public void Dispose()
    {
    }
}