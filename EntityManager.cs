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

    public Entity CreateMouseInputEntity()
    {
        var entity = _world.CreateEntity();
        entity.Set(new MouseInput(
            Vector2.Zero,
            Vector2.Zero,
            false,
            false
        ));
        return entity;
    }

    public Entity CreateWorldConfigEntity(int tileWidth, int tileHeight)
    {
        var entity = _world.CreateEntity();
        entity.Set(new WorldConfig());
        return entity;
    }

    public void Initialize()
    {
        CreateCameraEntity();
        CreateMouseInputEntity();
    }
}