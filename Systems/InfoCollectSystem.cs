using System;
using System.Linq;
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;

using XianCraft.Components;

namespace XianCraft.Systems;

public class InfoCollectSystem : AEntitySetSystem<GameTime>
{
    private readonly World _world;

    private EntitySet _cameraSet;
    private EntitySet _mouseInputSet;
    private EntitySet _chunkSet;
    private EntitySet _playerSet;
    private EntitySet _entitySet;

    // FPS 相关字段
    private int _frameCount = 0;
    private double _elapsedTime = 0;
    private int _fps = 0;

    private Entity _cameraEntity => _cameraSet.GetEntities().ToArray().FirstOrDefault();
    private Entity _mouseEntity => _mouseInputSet.GetEntities().ToArray().FirstOrDefault();
    private Entity _playerEntity => _playerSet.GetEntities().ToArray().FirstOrDefault();


    public InfoCollectSystem(World world) : base(world.GetEntities().With<DebugInfo>().AsSet())
    {
        _world = world;

        _cameraSet = _world.GetEntities().With<Camera>().AsSet();
        _mouseInputSet = _world.GetEntities().With<MouseInput>().AsSet();
        _playerSet = _world.GetEntities().With<Player>().With<Position>().AsSet();
        _chunkSet = _world.GetEntities().With<Chunk>().AsSet();
        _entitySet = _world.GetEntities().AsSet();
    }

    protected override void Update(GameTime gameTime, in Entity entity)
    {
        // FPS 统计
        _frameCount++;
        _elapsedTime += gameTime.ElapsedGameTime.TotalSeconds;
        if (_elapsedTime >= 1.0)
        {
            _fps = _frameCount;
            _frameCount = 0;
            _elapsedTime = 0;
        }

        ref var debugInfo = ref entity.Get<DebugInfo>();
        var camera = _cameraEntity.Get<Camera>();
        var mouseInput = _mouseEntity.Get<MouseInput>();

        // 内存和GC信息
        long totalMemory = GC.GetTotalMemory(false);
        var playerPos = _playerEntity.Get<Position>();

        // 进程信息
        var process = System.Diagnostics.Process.GetCurrentProcess();
        long workingSet = process.WorkingSet64;
        int handleCount = process.HandleCount;

        var systemInfo = $"[FPS] {_fps}\n" + // 新增 FPS 显示
                        $"[内存/GC]" +
                        $"\n托管堆内存: {totalMemory / 1024 / 1024:F2} MB" +
                        $"\n工作集: {workingSet / 1024 / 1024:F2} MB" +
                        $"\n句柄数: {handleCount}";

        systemInfo += $"\n\n[统计]\n" +
                    $"实体数量: {_entitySet.Count}\n" +
                    $"相机位置: {camera.Position.X:F1}, {camera.Position.Y:F1}\n" +
                    $"相机缩放: {camera.Zoom}\n" +
                    $"Viewport: {camera.ViewportWidth} x {camera.ViewportHeight}\n" +
                    $"鼠标位置: {mouseInput.Position.X:F1}, {mouseInput.Position.Y:F1}\n" +
                    $"鼠标世界位置: {mouseInput.WorldPosition.X:F1}, {mouseInput.WorldPosition.Y:F1}\n";

        var worldX = (int)Math.Floor(mouseInput.WorldPosition.X);
        var worldY = (int)Math.Floor(mouseInput.WorldPosition.Y);

        int FloorDiv(int a, int b) => (a >= 0) ? (a / b) : ((a - b + 1) / b);
        var chunkX = FloorDiv(worldX, Const.ChunkSize);
        var chunkY = FloorDiv(worldY, Const.ChunkSize);
        var x = ((worldX % Const.ChunkSize) + Const.ChunkSize) % Const.ChunkSize;
        var y = ((worldY % Const.ChunkSize) + Const.ChunkSize) % Const.ChunkSize;

        systemInfo += $"鼠标所在区块: {chunkX}, {chunkY} ({x}, {y})\n";
        var chunkEntities = _chunkSet.GetEntities();        

        foreach (var chunkEntity in chunkEntities)
        {
            var chunk = chunkEntity.Get<Chunk>();
            //Console.WriteLine($"Chunk: {chunk.Position.X}, {chunk.Position.Y} ({x}, {y})");
            if (chunk.Position.X == chunkX && chunk.Position.Y == chunkY)
            {
                systemInfo += $"地形类型: {chunk.TerrainData[x, y].Type}\n";
                break;
            }
        }

        systemInfo += $"\n[玩家信息]\n";
        systemInfo += $"玩家位置: {playerPos.Value.X:F1}, {playerPos.Value.Y:F1}\n";

        debugInfo.SystemInfo = systemInfo;
    }
}