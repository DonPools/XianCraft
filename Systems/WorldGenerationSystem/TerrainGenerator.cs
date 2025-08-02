using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using XianCraft.Components;
using XianCraft.Utils;

namespace XianCraft.Systems;

/// <summary>
/// 专门负责地形生成的类，与加载策略分离
/// </summary>
public class TerrainGenerator
{
    private readonly PerlinNoise _terrainNoise;
    
    // 地形生成参数
    private const double MainTerrainScale = 0.003;    // 主要地形规模
    private const double MountainScale = 0.002;       // 山脉规模
    private const double RiverScale = 0.008;          // 河流规模

    public TerrainGenerator(int seed = 0)
    {
        // 使用不同种子生成各种地形特征
        _terrainNoise = new PerlinNoise(seed);
    }

    /// <summary>
    /// 生成区块地形数据
    /// </summary>
    public TerrainType GenerateChunkTerrain(Point chunkPos, int chunkSize, int x, int y)
    {
        int startX = chunkPos.X * chunkSize;
        int startY = chunkPos.Y * chunkSize;

        int worldX = startX + x;
        int worldY = startY + y;
         
        double elevation = GenerateElevation(worldX, worldY);

        return elevation < 0.3 ? TerrainType.Water : TerrainType.Dirt;        
    }
    
    /// <summary>
    /// 生成2D地形高度值（0-1范围）
    /// </summary>
    public double GenerateElevation(int x, int y)
    {
        // 主要地形
        double mainTerrain = _terrainNoise.OctaveNoise2D(x, y, 6, 0.6, MainTerrainScale);
        
        // 山脉系统
        double mountains = _terrainNoise.OctaveNoise2D(x + 5000, y + 5000, 4, 0.7, MountainScale);
        mountains = Math.Max(0, mountains - 0.3); // 只保留较高的部分作为山脉
        
        // 河流谷地
        double riverX = Math.Abs(_terrainNoise.OctaveNoise2D(x + 10000, y * 0.1, 2, 0.8, RiverScale));
        double riverY = Math.Abs(_terrainNoise.OctaveNoise2D(x * 0.1, y + 10000, 2, 0.8, RiverScale));
        double riverCut = Math.Min(riverX, riverY);
        
        if (riverCut < 0.15) // 河流阈值
        {
            mainTerrain -= 0.4; // 切割出河谷
        }
        
        // 组合所有地形特征
        double elevation = (mainTerrain + mountains * 0.6) * 0.5 + 0.5;
        return Math.Max(0, Math.Min(1, elevation));
    }
}

