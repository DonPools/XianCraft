using System;
using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;

namespace XianCraft.Systems;

public class MovementSystem : AEntitySetSystem<GameTime>
{
    private readonly World _world;
    private readonly EntitySet _chunkSet;

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

        // 没有碰撞组件的实体，只检查地形碰撞
        var finalPos = FindFinalPosByTerrain(srcPos, dstPos, out var shouldStop);
        if (shouldStop)
            movement.Velocity = Vector2.Zero;

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
    private Vector2 FindFinalPosByTerrain(Vector2 srcPos, Vector2 dstPos, out bool shouldStop)
    {
        const float epsilon = 0.001f;
        float dx = dstPos.X - srcPos.X, dy = dstPos.Y - srcPos.Y;
        shouldStop = false;
        if (Math.Abs(dx) < 1e-6f && Math.Abs(dy) < 1e-6f)
            return dstPos;

        int startTileX = (int)Math.Floor(srcPos.X), startTileY = (int)Math.Floor(srcPos.Y);
        var curChunkCoord = Helper.WorldPosToChunk(new Vector2(startTileX, startTileY));
        Chunk? curChunk = GetChunk(curChunkCoord);
        if (!IsTileWalkable(curChunk, startTileX, startTileY))
        {
            shouldStop = true;
            return srcPos;
        }

        int stepX = Math.Sign(dx), stepY = Math.Sign(dy);
        float stepSizeX = dx != 0 ? 1f / Math.Abs(dx) : float.MaxValue;
        float stepSizeY = dy != 0 ? 1f / Math.Abs(dy) : float.MaxValue;
        float tMaxX = stepX != 0 ? ((stepX > 0 ? startTileX + 1 - srcPos.X : srcPos.X - startTileX) * stepSizeX) : float.MaxValue;
        float tMaxY = stepY != 0 ? ((stepY > 0 ? startTileY + 1 - srcPos.Y : srcPos.Y - startTileY) * stepSizeY) : float.MaxValue;
        int curX = startTileX, curY = startTileY;

        while (true)
        {
            if (tMaxX > 1f && tMaxY > 1f)
                return dstPos;

            bool moveX = tMaxX < tMaxY;
            int nextX = curX + (moveX ? stepX : 0);
            int nextY = curY + (moveX ? 0 : stepY);

            // 只在跨chunk时重新获取chunk
            var nextChunkCoord = Helper.WorldPosToChunk(new Vector2(nextX, nextY));
            if (nextChunkCoord != curChunkCoord)
            {
                curChunk = GetChunk(nextChunkCoord);
                curChunkCoord = nextChunkCoord;
            }

            if (!IsTileWalkable(curChunk, nextX, nextY))
            {
                float t = Math.Max(0, (moveX ? tMaxX : tMaxY) - epsilon);
                shouldStop = true;
                return new Vector2(srcPos.X + t * dx, srcPos.Y + t * dy);
            }
            if (moveX)
            {
                curX = nextX;
                tMaxX += stepSizeX;
            }
            else
            {
                curY = nextY;
                tMaxY += stepSizeY;
            }
        }
    }

    // 辅助函数：获取指定网格的区块
    private Chunk? GetChunkForTile(int tileX, int tileY)
    {
        var worldPos = new Vector2(tileX, tileY);
        var chunkCoord = Helper.WorldPosToChunk(worldPos);
        return GetChunk(chunkCoord);
    }

    // 你需要实现这个方法，根据你的地图数据判断格子是否可通行
    private bool IsTileWalkable(Chunk? chunk, int x, int y)
    {
        if (chunk is null)
            return false;

        int tileX = ((x % Const.ChunkSize) + Const.ChunkSize) % Const.ChunkSize;
        int tileY = ((y % Const.ChunkSize) + Const.ChunkSize) % Const.ChunkSize;

        var terrain = chunk.Value.TerrainData[tileX, tileY];

        return terrain.Type != TerrainType.Water && !terrain.HasTree;
    }
}