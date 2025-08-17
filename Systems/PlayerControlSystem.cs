using DefaultEcs;
using DefaultEcs.System;
using System.Linq;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using System;


public class PlayerControlSystem : AEntitySetSystem<GameTime>
{
    private readonly World _world;
    private readonly EntitySet _mouseInputSet;

    private Entity _mouseEntity => _mouseInputSet.GetEntities().ToArray().FirstOrDefault();

    public PlayerControlSystem(World world) : base(
        world.GetEntities().With<Player>().With<Movement>().AsSet())
    {
        _world = world;
        _mouseInputSet = _world.GetEntities().With<MouseInput>().AsSet();
    }

    protected override void Update(GameTime gameTime, in Entity entity)
    {
        var mouseInput = _mouseEntity.Get<MouseInput>();
        if (mouseInput.RightButton == true && mouseInput.PreviousRightButton == false)        
        {
            entity.Set(new MoveCommand
            {
                TargetPosition = mouseInput.WorldPosition,
            }
            );
            Console.WriteLine($"Player command to move to {mouseInput.WorldPosition}");
        }
    }
}