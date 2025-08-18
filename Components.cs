using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Aseprite;
using XianCraft.Utils;

namespace XianCraft.Components;

public class GlobalState
{
    public GameClock Clock;
    public GameTime GameTime;
}

/// <summary>
/// 地形方块类型枚举 - 简化版，只有陆地和水
/// </summary>
public enum TerrainType
{
    Sand,              // 沙子 (沙漠)
    Dirt,               // 泥土 (陆地)
    Water,              // 水面
    WetDirt,            // 湿泥土 (湿润的陆地)
    ShortGrass,         // 短草 (草地)
    TallGrass,          // 高草 (茂密的草地)
    Stone,              // 石头 (岩石)
}

// 地形方块组件
public struct Terrain
{
    public TerrainType Type;
    public bool HasTree; // 是否有树
}

public class TerrainMap
{
    private int minX, minY, maxX, maxY;
    private Terrain[,] data;

    public TerrainMap(int minX, int minY, int maxX, int maxY)
    {
        this.minX = minX;
        this.minY = minY;
        this.maxX = maxX;
        this.maxY = maxY;
        data = new Terrain[maxX - minX + 1, maxY - minY + 1];
    }

    // 获取或设置任意位置的值
    public Terrain this[int x, int y]
    {
        get
        {
            return data[x - minX, y - minY];
        }
        set
        {
            data[x - minX, y - minY] = value;
        }
    }

    public int MinX => minX;
    public int MinY => minY;
    public int MaxX => maxX;
    public int MaxY => maxY;

    public int Width => maxX - minX + 1;
    public int Height => maxY - minY + 1;

    public string GetHash()
    {
        return $"{minX},{minY},{maxX},{maxY}";
    }

    public bool IsWalkable(int x, int y)
    {
        if (x < minX || x > maxX || y < minY || y > maxY)
            return false;

        var terrain = this[x, y];
        return terrain.Type != TerrainType.Water && !terrain.HasTree;
    }
}



// 鼠标输入组件
public struct MouseInput
{
    public Vector2 Position;
    public Vector2 WorldPosition;
    public bool LeftButton;
    public bool RightButton;
    public bool PreviousLeftButton;
    public bool PreviousRightButton;
}

public struct Camera
{
    public Vector2 Position;
    public float Zoom;
    public int ViewportWidth;
    public int ViewportHeight;

    public Camera(Vector2 position, float zoom)
    {
        Position = position;
        Zoom = zoom;
    }
}

public class AnimateState
{
    public Dictionary<string, AnimationData> Animations;

    public string Direction = "Down"; // 当前方向

    public string EntityName = "";
    public Vector2 Origin = new Vector2(0, 0); // 原点偏移
    public string CurrentAnimationName;
    public AnimatedSprite CurrentAnimation;
    public Rectangle SourceRectangle;
    public GameTime AnimationTime;

    public void SetAnimation(string animationName)
    {
        if (Animations.TryGetValue(animationName, out var animationData))
        {
            CurrentAnimationName = animationName;
            CurrentAnimation = animationData.AnimatedSprite;
            SourceRectangle = animationData.SourceRect;
            CurrentAnimation.Reset();
            CurrentAnimation.Play();
        }
        else
        {
            CurrentAnimation = null; // 或者设置为一个默认动画
            SourceRectangle = Rectangle.Empty; // 或者设置为一个默认矩形
        }
    }
}

// 输入控制
struct MoveCommand
{
    public Vector2 TargetPosition;   // 鼠标点击的目标位置
}

// 寻路数据
struct PathData
{
    public List<Vector2> Path;
}

// 移动属性
struct Movement
{
    public float CurrentSpeed; // 当前速度
    public float MoveSpeed;     // 移动速度 (units/sec)
    public float StopDistance;  // 到达目标的判定距离

    override public string ToString()
    {
        return $"Speed: {MoveSpeed}, StopDistance: {StopDistance}";
    }
}

// 方向状态 (用于动画控制)
public enum Direction : byte
{
    Up, Down, Left, Right,
}

struct Facing
{
    public Direction Value;
    public float Angle; // 方向角度（弧度制）
}

// 碰撞数据
struct CircleCollider
{
    public float Radius;      // 圆形碰撞半径
}

public class Player { }

public class Position
{
    public Vector2 Value { get; set; }
}

public class DebugInfo
{
    public string SystemInfo { get; set; } = string.Empty;
}

public class LightSource
{
    public Color Color; // 光源颜色
    public float Intensity; // 光照强度 (0.0 - 1.0)
    public float Range;    // 光照半径
    public bool IsFlickering; // 是否闪烁
}