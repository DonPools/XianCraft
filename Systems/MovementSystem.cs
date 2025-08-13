using System;
using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;

namespace XianCraft.Systems;

public class MovementSystem : AEntitySetSystem<GameTime>
{
    private readonly World _world;
    private EntitySet _chunkSet;

    public MovementSystem(World world) : base(world.GetEntities().With<Movement>().With<Position>().AsSet())
    {
        _world = world;
        _chunkSet = _world.GetEntities().With<Chunk>().AsSet();
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

        // 检查是否有碰撞
        var srcPos = position.Value;
        var dstPos = position.Value + movement.Velocity * deltaTime;
        if (srcPos == dstPos)
            return; // 没有移动        

        // 获取最后可达点
        var finalPos = FindLastAccessiblePoint(srcPos, dstPos, out bool shouldStop);
        if (shouldStop)
        {
            movement.Velocity = Vector2.Zero;
            return;
        }

        position.Value = finalPos;
    }

    private Chunk? GetChunk(Point pos)
    {
        foreach (var chunkEntity in _chunkSet.GetEntities())
        {
            var chunk = chunkEntity.Get<Chunk>();
            if (chunk.Position.X == pos.X && chunk.Position.Y == pos.Y)
                return chunk;
        }

        return null;
    }

    // 返回最后一个可达点，如果全程可达则返回dstPos
    private Vector2 FindLastAccessiblePoint(Vector2 srcPos, Vector2 dstPos, out bool shouldStop)
    {
        Point srcTile = new Point((int)Math.Floor(srcPos.X), (int)Math.Floor(srcPos.Y));
        Point dstTile = new Point((int)Math.Floor(dstPos.X), (int)Math.Floor(dstPos.Y));

        int x0 = srcTile.X, y0 = srcTile.Y;
        int x1 = dstTile.X, y1 = dstTile.Y;
        int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        int lastX = x0, lastY = y0;

        Chunk? curChunk = null;
        while (true)
        {
            if (Helper.WorldPosToChunk(new Vector2(x0, y0)) != Helper.WorldPosToChunk(new Vector2(lastX, lastY)) || curChunk == null)
                curChunk = GetChunk(Helper.WorldPosToChunk(new Vector2(x0, y0)));

            if (!IsTileWalkable(curChunk, x0, y0))
            {
                shouldStop = true;
                return new Vector2(lastX + 0.5f, lastY + 0.5f);
            }

            if (x0 == x1 && y0 == y1)
                break;

            lastX = x0;
            lastY = y0;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
        // 全程可达，返回目标点
        shouldStop = false;
        return dstPos;
    }

    // 你需要实现这个方法，根据你的地图数据判断格子是否可通行
    private bool IsTileWalkable(Chunk? chunk, int x, int y)
    {
        if (chunk is null)
            return false;

        int tileX = ((x % Const.ChunkSize) + Const.ChunkSize) % Const.ChunkSize;
        int tileY = ((y % Const.ChunkSize) + Const.ChunkSize) % Const.ChunkSize;

        var terrain = chunk?.TerrainData[tileX, tileY];
        if (terrain is null)
            return false;

        return terrain?.Type != TerrainType.Water;
    }
}