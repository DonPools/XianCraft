using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Aseprite;

namespace XianCraft.Components;

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

    public Terrain(TerrainType type)
    {
        Type = type;
    }
}

// 区块组件 - 表示一个16x16的地形区块
public struct Chunk
{
    public Point Position;
    public TerrainType[,] TerrainData;

    public Chunk(Point position, TerrainType[,] terrainData)
    {
        Position = position;
        TerrainData = terrainData;
    }
}

// 鼠标输入组件
public struct MouseInput
{
    public Vector2 Position;
    public Vector2 WorldPosition;
    public bool LeftButton;
    public bool RightButton;

    public MouseInput(Vector2 position, Vector2 worldPosition, bool leftButton, bool rightButton)
    {
        Position = position;
        WorldPosition = worldPosition;
        LeftButton = leftButton;
        RightButton = rightButton;
    }
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

public enum MovementType { Idle, Walk, Run }
public enum FacingDirection { Up, Down, Left, Right }

enum CharacterState
{
    Idle,
    Run,
}

enum CharacterDirection
{
    Up,
    Down,
    Left,
    Right,
}

public class CharacterAnimateState
{
    public Dictionary<string, AnimationData> Animations;

    public string CurrentAnimationName;
    public AnimatedSprite CurrentAnimation;
    public Rectangle SourceRectangle;
    public GameTime AnimationTime;
}

public class Movement
{
    public Vector2 Velocity { get; set; } = Vector2.Zero;  // 当前速度向量
    public Vector2 TargetDirection { get; set; } = Vector2.Zero; // 目标方向

    public float Acceleration = 500f; // 加速度
    public float Deceleration = 800f; // 减速度
    public float MaxSpeed = 200f;     // 最大速度    
}

public class Player { }

public class Position
{
    public Vector2 Value { get; set; }
}