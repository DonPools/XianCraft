using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;
using System;

public class CharacterAnimationSystem : AEntitySetSystem<GameTime>
{
    private readonly World _world;

    public CharacterAnimationSystem(World world) : base(world.GetEntities().With<CharacterAnimateState>().AsSet())
    {
        _world = world;
    }

    protected override void Update(GameTime gameTime, in Entity entity)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        ref var animState = ref entity.Get<CharacterAnimateState>();
        if (entity.Has<Movement>())
        {
            ref var movement = ref entity.Get<Movement>();

            // 根据速度获得运动方向
            string direction = GetDirectionFromVelocity(movement.Velocity);
            var animationName = movement.TargetDirection != Vector2.Zero
                ? $"Walk_{direction}"
                : $"Idle_{direction}";

            if (animState.CurrentAnimationName != animationName)
            {
                animState.CurrentAnimationName = animationName;

                // 如果有对应的动画，更新当前动画
                if (animState.Animations.TryGetValue(animationName, out var animationData))
                {
                    animState.CurrentAnimation = animationData.AnimatedSprite;
                    animState.SourceRectangle = animationData.SourceRect;
                    animState.CurrentAnimation.Reset();
                    animState.CurrentAnimation.Play();
                }
                else
                {
                    // 如果没有找到对应的动画，使用默认动画
                    animState.CurrentAnimation = null; // 或者设置为一个默认动画
                    animState.SourceRectangle = Rectangle.Empty; // 或者设置为一个默认矩形
                }
            }

            if (animState.CurrentAnimation != null)
                animState.CurrentAnimation.Update(gameTime);
        }

    }

    private string GetDirectionFromVelocity(Vector2 velocity)
    {
        if (velocity == Vector2.Zero)
            return "Down"; // 默认朝向

        // 计算角度
        float angle = (float)Math.Atan2(velocity.Y, velocity.X);

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
