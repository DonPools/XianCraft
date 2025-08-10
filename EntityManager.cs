using DefaultEcs;
using XianCraft.Components;
using Microsoft.Xna.Framework;

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
    
    public Entity CreateTreeEntity(Vector2 position)
    {
        var entity = _world.CreateEntity();
        var animateState = new AnimateState
        {
            Animations = _assetManager.GetCharacterAnimations("tree"),
        };
        animateState.SetAnimation("Default");
        entity.Set(new Position { Value = position });        
        entity.Set(animateState);
        return entity;
    }

    public Entity CreatePlayerEntity()
    {
        var entity = _world.CreateEntity();
        entity.Set(new Player());
        entity.Set(new Position { Value = new Vector2(1, 1) });
        entity.Set(new Movement());
        entity.Set(new AnimateState
        {
            Animations = _assetManager.GetCharacterAnimations("wolf")
        });
        return entity;
    }

    public void Initialize()
    {
        CreateCameraEntity();
        CreateMouseInputEntity();
        CreatePlayerEntity();
    }
}