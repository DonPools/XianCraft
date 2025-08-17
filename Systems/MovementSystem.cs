using System;
using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;

namespace XianCraft.Systems;

public class MovementSystem : AEntitySetSystem<GameTime>
{
    private readonly World _world;

    public MovementSystem(World world) : base(
        world.GetEntities().With<Movement>().With<Position>().With<PathData>().AsSet())
    {
        _world = world;
    }

    protected override void Update(GameTime gameTime, in Entity entity)
    {
        ref var position = ref entity.Get<Position>();
        ref var movement = ref entity.Get<Movement>();
        ref var pathData = ref entity.Get<PathData>();

        if (pathData.Path.Count == 0)
            return;

        // 获取当前目标节点
        var targetNode = pathData.Path[0];

        // 计算移动向量
        Vector2 direction = new Vector2(targetNode.X - position.Value.X, targetNode.Y - position.Value.Y);
        float distance = direction.Length();

        if (distance < movement.MoveSpeed * gameTime.ElapsedGameTime.TotalSeconds)
        {
            // 到达目标节点，移除它
            pathData.Path.RemoveAt(0);
            if (pathData.Path.Count == 0)
            {
                movement.CurrentSpeed = 0; // 停止移动
                return; // 如果路径为空，直接返回
            }

            targetNode = pathData.Path[0];
            direction = new Vector2(targetNode.X - position.Value.X, targetNode.Y - position.Value.Y);
        }

        // 归一化方向并应用速度
        direction.Normalize();
        movement.CurrentSpeed = movement.MoveSpeed;
        position.Value += direction * movement.MoveSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

        // face direction
        if (entity.Has<Facing>())
        {
            ref var facing = ref entity.Get<Facing>();
            facing.Angle = (float)Math.Atan2(direction.Y, direction.X); // 弧度

            // 计算主方向
            float angleDeg = MathHelper.ToDegrees(facing.Angle);
            angleDeg = (angleDeg + 360) % 360; // 保证为正角度

            if (angleDeg >= 45 && angleDeg < 135)
                facing.Value = Direction.Down;
            else if (angleDeg >= 135 && angleDeg < 225)
                facing.Value = Direction.Left;
            else if (angleDeg >= 225 && angleDeg < 315)
                facing.Value = Direction.Up;
            else
                facing.Value = Direction.Right;
        }
    }

    

}