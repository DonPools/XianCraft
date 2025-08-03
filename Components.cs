using Microsoft.Xna.Framework;

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
public struct ChunkComponent
{
    public Point Position;
    public TerrainType[,] TerrainData;

    public ChunkComponent(Point position, TerrainType[,] terrainData)
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

public struct CameraComponent
{
    public Vector2 Position;
    public float Zoom;
    public int ViewportWidth;
    public int ViewportHeight;

    public CameraComponent(Vector2 position, float zoom)
    {
        Position = position;
        Zoom = zoom;
    }
}
