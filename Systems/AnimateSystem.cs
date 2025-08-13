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
        if (entity.Has<Movement>())
        {
            var movement = entity.Get<Movement>();
            // 根据速度获得运动方向, TODO: 把方向从动画状态中移除
            string direction = GetDirectionFromVelocity(movement.TargetDirection, animState.Direction);
            animState.Direction = direction; // 记录当前方向
            var animationName = movement.TargetDirection != Vector2.Zero
                ? $"Run_{direction}"
                : $"Idle_{direction}";

            if (animState.CurrentAnimationName != animationName)
                animState.SetAnimation(animationName);
        }

        if (animState.CurrentAnimation != null)
            animState.CurrentAnimation.Update(gameTime);

    }

    private string GetDirectionFromVelocity(Vector2 targetDirection, string currentDirection)
    {
        if (targetDirection == Vector2.Zero)
            return currentDirection; // 保持原先的方向

        // 计算角度
        float angle = (float)Math.Atan2(targetDirection.Y, targetDirection.X);

        // 将角度转换为度数并标准化到 0-360 范围
        float degrees = MathHelper.ToDegrees(angle);
        if (degrees < 0)
            degrees += 360;

        // 根据角度确定方向（8方向或4方向）
        if (degrees >= 315 || degrees < 45)
            return "Right";
        else if (degrees >= 45 && degrees < 135)
            return "Down";
        else if (degrees >= 135 && degrees < 225)
            return "Left";
        else // 225 <= degrees < 315
            return "Up";
    }
}
