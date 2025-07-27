using DefaultEcs;
using XianCraft.Components;
using Microsoft.Xna.Framework;

namespace XianCraft;

public class EnitityManager
{
    World _world;

    public EnitityManager(World world)
    {
        _world = world;
    }

    public Entity CreateCameraEntity()
    {
        var entity = _world.CreateEntity();
        entity.Set(new CameraComponent(new Vector2(0, 0), 1.0f));
        return entity;
    }

    public void Initialize()
    {
        CreateCameraEntity();
    }
}