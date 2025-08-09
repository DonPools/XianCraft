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
        entity.Set(new Camera(new Vector2(0, 0), 4.0f));
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

    public Entity CreatePlayerEntity()
    {
        var entity = _world.CreateEntity();
        entity.Set(new Player());
        entity.Set(new Position { Value = Vector2.Zero });
        entity.Set(new Movement());
        return entity;
    }

    public void Initialize()
    {
        CreateCameraEntity();
        CreateMouseInputEntity();
        CreatePlayerEntity();
    }
}