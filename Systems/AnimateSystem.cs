using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using System;

public class AnimationSystem : AEntitySetSystem<GameTime>
{
    private readonly World _world;

    public AnimationSystem(World world) : base(world.GetEntities().With<AnimateState>().AsSet())
    {
        _world = world;
    }

    protected override void Update(GameTime gameTime, in Entity entity)
    {
        ref var animState = ref entity.Get<AnimateState>();
        if (entity.Has<Facing>() && entity.Has<Movement>())
        {
            var facing = entity.Get<Facing>();
            var movement = entity.Get<Movement>();
            var moveAnimate = movement.CurrentSpeed > 0 ? "Run" : "Idle";
            var direction = facing.Value.ToString();
            var animationName = $"{moveAnimate}_{direction}";

            if (animState.CurrentAnimationName != animationName)
                animState.SetAnimation(animationName);
        }
        

        if (animState.CurrentAnimation != null)
            animState.CurrentAnimation.Update(gameTime);
    }
}
