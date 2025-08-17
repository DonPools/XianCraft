using System;
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using XianCraft.Components;

namespace XianCraft.Systems;

public class CollisionSystem : AEntitySetSystem<GameTime>
{
    private readonly World _world;
    private readonly EntitySet _chunkSet;

    public CollisionSystem(World world) : base(
        world.GetEntities().With<Position>().With<Movement>().AsSet())
    {
        _world = world;
    }

    protected override void Update(GameTime gameTime, in Entity entity)
    {
        // 这里可以实现碰撞检测逻辑
        // 例如，检测实体是否与环境或其他实体发生碰撞
    }

}