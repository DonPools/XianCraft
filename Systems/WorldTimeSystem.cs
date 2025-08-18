using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using XianCraft.Components;

namespace XianCraft.Systems;

public class WorldTimeSystem : AEntitySetSystem<GameTime>
{
    public WorldTimeSystem(World world) : base(
        world.GetEntities().With<GlobalState>().AsSet())
    { }
    
    protected override void Update(GameTime gameTime, in Entity entity)
    {
        ref var globalState = ref entity.Get<GlobalState>();
        globalState.Clock.Advance((float)gameTime.ElapsedGameTime.TotalSeconds);
    }
}