using System;

namespace XianCraft.Utils;

/// <summary>
/// 柏林噪音生成器 - 基于 Ken Perlin 的经典算法
/// </summary>
public class PerlinNoise
{
    private readonly int[] _permutation;
    private readonly Random _random;

    // 标准梯度向量
    private static readonly int[,] Gradients = new int[,]
    {
        {1,1,0}, {-1,1,0}, {1,-1,0}, {-1,-1,0},
        {1,0,1}, {-1,0,1}, {1,0,-1}, {-1,0,-1},
        {0,1,1}, {0,-1,1}, {0,1,-1}, {0,-1,-1}
    };

    public PerlinNoise(int seed = 0)
    {
        _random = new Random(seed);
        _permutation = new int[512];
        
        // 初始化排列表
        var p = new int[256];
        for (int i = 0; i < 256; i++)
        {
            p[i] = i;
        }

        // Fisher-Yates 洗牌算法
        for (int i = 255; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (p[i], p[j]) = (p[j], p[i]);
        }

        // 复制排列表以避免边界问题
        for (int i = 0; i < 512; i++)
        {
            _permutation[i] = p[i % 256];
        }
    }

    /// <summary>
    /// 生成2D柏林噪音
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>噪音值 (-1 到 1)</returns>
    public double Noise2D(double x, double y)
    {
        // 找到单位立方体中的位置
        int xi = (int)Math.Floor(x) & 255;
        int yi = (int)Math.Floor(y) & 255;

        // 找到相对位置
        double xf = x - Math.Floor(x);
        double yf = y - Math.Floor(y);

        // 计算淡化曲线
        double u = Fade(xf);
        double v = Fade(yf);

        // 哈希坐标
        int aa = _permutation[_permutation[xi] + yi];
        int ab = _permutation[_permutation[xi] + yi + 1];
        int ba = _permutation[_permutation[xi + 1] + yi];
        int bb = _permutation[_permutation[xi + 1] + yi + 1];

        // 计算梯度
        double x1 = Lerp(u, Grad2D(aa, xf, yf), Grad2D(ba, xf - 1, yf));
        double x2 = Lerp(u, Grad2D(ab, xf, yf - 1), Grad2D(bb, xf - 1, yf - 1));

        return Lerp(v, x1, x2);
    }

    /// <summary>
    /// 分层噪音 (Octave Noise) - 生成更复杂的地形
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="octaves">倍频数</param>
    /// <param name="persistence">持续性 (每层的振幅衰减)</param>
    /// <param name="scale">缩放比例</param>
    /// <returns>分层噪音值</returns>
    public double OctaveNoise2D(double x, double y, int octaves, double persistence = 0.5, double scale = 1.0)
    {
        double value = 0.0;
        double amplitude = 1.0;
        double frequency = scale;
        double maxValue = 0.0;

        for (int i = 0; i < octaves; i++)
        {
            value += Noise2D(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= 2.0;
        }

        return value / maxValue;
    }

    /// <summary>
    /// 生成地形高度 - 专门用于地形生成的便利方法
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="z">Z坐标 (在2D游戏中通常是固定值)</param>
    /// <param name="baseHeight">基础高度</param>
    /// <param name="amplitude">振幅</param>
    /// <param name="octaves">倍频数</param>
    /// <param name="persistence">持续性</param>
    /// <param name="scale">缩放比例</param>
    /// <returns>地形高度</returns>
    public int GenerateTerrainHeight(double x, double z = 0, int baseHeight = 50, int amplitude = 20, 
        int octaves = 4, double persistence = 0.5, double scale = 0.01)
    {
        double noise = OctaveNoise2D(x, z, octaves, persistence, scale);
        return baseHeight + (int)(noise * amplitude);
    }

    /// <summary>
    /// 生成洞穴噪音 - 用于生成地下洞穴
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="threshold">洞穴阈值</param>
    /// <returns>是否为洞穴</returns>
    public bool GenerateCave(double x, double y, double threshold = 0.6)
    {
        double caveNoise1 = Math.Abs(OctaveNoise2D(x, y, 3, 0.5, 0.02));
        double caveNoise2 = Math.Abs(OctaveNoise2D(x + 1000, y + 1000, 3, 0.5, 0.025));
        
        return caveNoise1 > threshold && caveNoise2 > threshold;
    }

    /// <summary>
    /// 生成矿物分布 - 用于矿物生成
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="oreType">矿物类型偏移</param>
    /// <param name="threshold">生成阈值</param>
    /// <returns>是否生成矿物</returns>
    public bool GenerateOre(double x, double y, int oreType, double threshold = 0.8)
    {
        double oreNoise = Math.Abs(OctaveNoise2D(x + oreType * 500, y + oreType * 300, 2, 0.6, 0.05));
        return oreNoise > threshold;
    }

    // 淡化函数 - 创建平滑的过渡
    private static double Fade(double t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    // 线性插值
    private static double Lerp(double t, double a, double b)
    {
        return a + t * (b - a);
    }

    // 2D梯度函数
    private static double Grad2D(int hash, double x, double y)
    {
        int h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : (h == 12 || h == 14 ? x : 0);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
