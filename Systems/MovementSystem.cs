using System;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using XianCraft.Components;
using Microsoft.Xna.Framework;

public class MovementSystem : AEntitySetSystem<GameTime>
{
    private readonly World _world;
    public MovementSystem(World world) : base(world.GetEntities().With<Movement>().With<Position>().AsSet())
    {
        _world = world;
    }

    protected override void Update(GameTime gameTime, in Entity entity)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        ref var movement = ref entity.Get<Movement>();
        ref var position = ref entity.Get<Position>();

        Vector2 targetVelocity = movement.TargetDirection * movement.MaxSpeed;

        // 有目标方向时加速
        if (movement.TargetDirection != Vector2.Zero)
        {
            movement.Velocity = Vector2.Lerp(
                movement.Velocity,
                targetVelocity,
                movement.Acceleration * deltaTime / movement.MaxSpeed
            );
        }
        // 无目标方向时减速
        else if (movement.Velocity != Vector2.Zero)
        {
            Vector2 deceleration = Vector2.Normalize(movement.Velocity) *
                                   movement.Deceleration * deltaTime;

            if (deceleration.Length() >= movement.Velocity.Length())
                movement.Velocity = Vector2.Zero;
            else
                movement.Velocity -= deceleration;
        }

        // 硬性速度上限
        if (movement.Velocity.Length() > movement.MaxSpeed)
            movement.Velocity = Vector2.Normalize(movement.Velocity) * movement.MaxSpeed;

        position.Value += movement.Velocity * deltaTime;
    }
}