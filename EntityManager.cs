using DefaultEcs;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using XianCraft.Utils;

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
        var gameClock = new GameClock();
        gameClock.SetDayAndHour(1, 6); // 初始化为第1天，6点钟
        gameClock.TimeScale = 3600;
        
        entity.Set(new DebugInfo());
        entity.Set(new GlobalState
        { 
            Clock = gameClock
        });
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
            MoveSpeed = 5.0f,
        });
        entity.Set(new Facing{
            Value = Direction.Down,
            Angle = MathHelper.PiOver2, // 90度，向下
        });
        entity.Set(new LightSource
        {
            Color = new Color(1f, 0.74f, 0.32f), // 火炬颜色
            Intensity = 0.9f,
            Range = 8.0f,
            IsFlickering = true // 可选：是否闪烁
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