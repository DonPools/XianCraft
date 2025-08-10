using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;


public class PlayerControlSystem : AEntitySetSystem<GameTime>
{
    private readonly World _world;
    public PlayerControlSystem(World world) : base(
        world.GetEntities().With<Player>().With<Movement>().AsSet())
    {
        _world = world;
    }

    protected override void Update(GameTime gameTime, in Entity entity)
    {
        ref var movement = ref entity.Get<Movement>();
        movement.TargetDirection = GetInputDirection();
    }


    private static Vector2 GetInputDirection()
    {
        Vector2 direction = Vector2.Zero;
        KeyboardState keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(Keys.W)) direction.Y -= 1;
        if (keyboardState.IsKeyDown(Keys.S)) direction.Y += 1;
        if (keyboardState.IsKeyDown(Keys.A)) direction.X -= 1;
        if (keyboardState.IsKeyDown(Keys.D)) direction.X += 1;

        return direction != Vector2.Zero ?
            Vector2.Normalize(direction) : Vector2.Zero;
    }
}