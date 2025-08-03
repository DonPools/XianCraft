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
    private const double HumidityScale = 0.004;       // 湿度噪声规模
    private const double TemperatureScale = 0.003;    // 温度噪声规模

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
        double humidity = GenerateHumidity(worldX, worldY, elevation);
        double temperature = GenerateTemperature(worldX, worldY, elevation);

        return DetermineTerrainType(elevation, humidity, temperature);        
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
    
    /// <summary>
    /// 生成湿度值（0-1范围），基于地形高度和噪声
    /// </summary>
    private double GenerateHumidity(int x, int y, double elevation)
    {
        // 基础湿度噪声
        double baseHumidity = _terrainNoise.OctaveNoise2D(x + 20000, y + 20000, 4, 0.5, HumidityScale);
        baseHumidity = (baseHumidity + 1) * 0.5; // 转换到0-1范围
        
        // 高度对湿度的影响：海拔越高越干燥，低地和河谷更湿润
        double elevationEffect = 1.0 - elevation * 0.6; // 高海拔减少湿度
        
        // 检查是否靠近河流
        double riverX = Math.Abs(_terrainNoise.OctaveNoise2D(x + 10000, y * 0.1, 2, 0.8, RiverScale));
        double riverY = Math.Abs(_terrainNoise.OctaveNoise2D(x * 0.1, y + 10000, 2, 0.8, RiverScale));
        double riverProximity = Math.Min(riverX, riverY);
        
        // 河流附近增加湿度
        double riverEffect = 1.0;
        if (riverProximity < 0.25)
        {
            riverEffect = 1.0 + (0.25 - riverProximity) * 2.0; // 河流附近湿度增加
        }
        
        double humidity = baseHumidity * elevationEffect * riverEffect;
        return Math.Max(0, Math.Min(1, humidity));
    }
    
    /// <summary>
    /// 生成温度值（0-1范围），基于地形高度和纬度
    /// </summary>
    private double GenerateTemperature(int x, int y, double elevation)
    {
        // 基础温度噪声（气候变化）
        double baseTemperature = _terrainNoise.OctaveNoise2D(x + 30000, y + 30000, 3, 0.6, TemperatureScale);
        baseTemperature = (baseTemperature + 1) * 0.5; // 转换到0-1范围
        
        // 纬度效应：距离中心越远越冷（简化的纬度效应）
        double latitudeEffect = 1.0 - Math.Abs(y * 0.000005); // 纬度越高越冷
        latitudeEffect = Math.Max(0.2, latitudeEffect); // 保证最低温度
        
        // 海拔效应：海拔每升高，温度下降
        double altitudeEffect = 1.0 - elevation * 0.8; // 高海拔显著降温
        altitudeEffect = Math.Max(0.1, altitudeEffect); // 保证最低温度
        
        double temperature = baseTemperature * latitudeEffect * altitudeEffect;
        return Math.Max(0, Math.Min(1, temperature));
    }
    
    /// <summary>
    /// 根据高度、湿度和温度确定地形类型
    /// </summary>
    private TerrainType DetermineTerrainType(double elevation, double humidity, double temperature)
    {
        // 水面
        if (elevation < 0.3)
        {
            return TerrainType.Water;
        }

        // 检查是否靠近河流
        bool nearRiver = IsNearRiver(elevation);
        
        // 高山地区 (0.8+)
        if (elevation > 0.8)
        {
            if (temperature < 0.2) // 极寒高山
                return TerrainType.Stone; // 雪线以上
            else if (temperature < 0.4) // 寒冷高山
                return TerrainType.Dirt; // 高山荒地
            else if (nearRiver && humidity > 0.4)
                return TerrainType.TallGrass; // 高山河谷
            else if (humidity > 0.3)
                return TerrainType.ShortGrass; // 高山草甸
            else
                return TerrainType.Stone; // 干燥岩石
        }

        // 中高海拔地区 (0.5-0.8)
        if (elevation > 0.5)
        {
            if (temperature < 0.3) // 寒冷地区
            {
                return humidity > 0.4 ? TerrainType.ShortGrass : TerrainType.Dirt;
            }
            else if (nearRiver) // 河流附近
            {
                return humidity > 0.6 ? TerrainType.TallGrass : TerrainType.WetDirt;
            }
            else if (humidity > 0.7 && temperature > 0.4) // 非常湿润温暖
            {
                return TerrainType.TallGrass;
            }
            else if (humidity > 0.4) // 中等湿度
            {
                return TerrainType.ShortGrass;
            }
            else if (humidity > 0.2) // 轻微干燥
            {
                return TerrainType.Dirt; // 干燥土地
            }
            else // 非常干燥
            {
                return temperature > 0.7 ? TerrainType.Sand : TerrainType.Stone;
            }
        }

        // 中低海拔地区 (0.35-0.5)
        if (elevation > 0.35)
        {
            if (nearRiver) // 河流附近
            {
                if (humidity > 0.6)
                    return TerrainType.TallGrass; // 茂密河岸植被
                else
                    return TerrainType.WetDirt; // 湿润河岸
            }
            else if (humidity > 0.6) // 湿润地区
            {
                return TerrainType.TallGrass;
            }
            else if (humidity > 0.4) // 中等湿度
            {
                return TerrainType.ShortGrass;
            }
            else if (humidity > 0.2) // 轻微干燥
            {
                return TerrainType.Dirt;
            }
            else // 干燥
            {
                return temperature > 0.6 ? TerrainType.Sand : TerrainType.Dirt;
            }
        }

        // 低地 (0.3-0.35) - 靠近水体
        if (nearRiver) // 河流附近的低地
        {
            if (humidity > 0.5)
                return TerrainType.TallGrass; // 河岸茂密植被
            else
                return TerrainType.WetDirt; // 湿润河滩
        }
        else if (humidity > 0.6) // 湿润低地
        {
            return TerrainType.TallGrass;
        }
        else if (humidity > 0.4) // 中等湿度
        {
            return TerrainType.ShortGrass;
        }
        else if (humidity > 0.2) // 干燥低地
        {
            return TerrainType.Dirt;
        }
        else // 非常干燥的低地
        {
            return TerrainType.Sand;
        }
    }

    /// <summary>
    /// 检查是否靠近河流
    /// </summary>
    private bool IsNearRiver(double elevation)
    {
        // 如果海拔很低，很可能是河流切割形成的谷地
        return elevation < 0.4; // 低海拔地区更可能是河流附近
    }
}

