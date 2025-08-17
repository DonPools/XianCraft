using DefaultEcs;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace XianCraft;

public class EntityManager
{
    World _world;
    AssetManager _assetManager;

    public EntityManager(World world, AssetManager assetManager)
    {
        _world = world;
        _assetManager = assetManager;
    }
    
    public Entity CreateGlobalStateEntity()
    {
        var entity = _world.CreateEntity();
        entity.Set(new DebugInfo());
        entity.Set(new GlobalState());
        entity.Set(new TerrainMap(0, 0, 0, 0));
        return entity;
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
        entity.Set(new MouseInput{
            Position = Vector2.Zero,
            WorldPosition = Vector2.Zero,
            LeftButton = false,
            RightButton = false
        });
        return entity;
    }

    public Entity CreateTreeEntity(Vector2 position)
    {
        var entityAsset = _assetManager.GetEntityAsset("tree");
        var animateState = new AnimateState
        {
            EntityName = entityAsset.Name,
            Origin = entityAsset.Origin,
            Animations = entityAsset.Animations,
        };
        animateState.SetAnimation("Default");

        var entity = _world.CreateEntity();
        entity.Set(new Position { Value = position });
        entity.Set(animateState);
        return entity;
    }

    public Entity CreatePlayerEntity()
    {
        var entityAsset = _assetManager.GetEntityAsset("wolf");
        var animateState = new AnimateState
        {
            EntityName = entityAsset.Name,
            Origin = entityAsset.Origin,
            Animations = entityAsset.Animations,
        };
        animateState.SetAnimation("Idle_Down");

        var entity = _world.CreateEntity();
        entity.Set(new Player());
        entity.Set(new Position { Value = new Vector2(8.5f, 8.5f) });
        entity.Set(new Movement{
            MoveSpeed = 3.0f,
        });
        entity.Set(new Facing{
            Value = Direction.Down,
            Angle = MathHelper.PiOver2, // 90度，向下
        });
        entity.Set(animateState);
        return entity;
    }

    public void Initialize()
    {
        CreateGlobalStateEntity();
        CreateCameraEntity();
        CreateMouseInputEntity();
        CreatePlayerEntity();
    }
}