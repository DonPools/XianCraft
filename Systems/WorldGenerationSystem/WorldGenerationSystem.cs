
using System;
using DefaultEcs;
using DefaultEcs.System;
using XianCraft.Components;
using Microsoft.Xna.Framework;

namespace XianCraft.Systems;

public class WorldGenerationSystem : ISystem<float>
{
    private readonly World _world;
    private bool _isEnabled = true;
    private Entity _cameraEntity;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public WorldGenerationSystem(World world)
    {
        _world = world;
        _cameraEntity = _world.GetEntities().With<CameraComponent>().AsSet().GetEntities()[0];

        buildDebugChunk();
    }

    private void buildDebugChunk()
    {
        // 以 0,0 为中心，内圈 dirt，外圈 water
        for (var x = -2; x < 3; x++)
        {
            for (var y = -2; y < 3; y++)
            {
                var position = new Vector2(x, y);
                var terrainData = new TerrainType[Const.ChunkSize, Const.ChunkSize];
                for (int i = 0; i < Const.ChunkSize; i++)
                {
                    for (int j = 0; j < Const.ChunkSize; j++)
                    {
                        var posX = x * Const.ChunkSize + i;
                        var posY = y * Const.ChunkSize + j;
                        // 计算到中心的曼哈顿距离
                        int dist = Math.Max(Math.Abs(posX - 8), Math.Abs(posY - 8));
                        terrainData[i, j] = dist % 2 == 0 ? TerrainType.Dirt : terrainData[i, j] = TerrainType.Water;
                    }
                }
                _world.CreateEntity().Set(new ChunkComponent(position, terrainData));
            }
        }
    }

    public void Update(float deltaTime)
    {
        var camera = _cameraEntity.Get<CameraComponent>();
    }

    public void Dispose()
    {
    }
}