using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using AsepriteDotNet.Aseprite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using MonoGame.Aseprite;
using MonoGame.Extended;
using Microsoft.Xna.Framework;

public class RectangleData
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }

    public Rectangle ToRectangle() => new Rectangle(X, Y, Width, Height);

    public override string ToString() => $"X:{X}, Y:{Y}, W:{Width}, H:{Height}";
}

public class OriginData
{
    public float X { get; init; }
    public float Y { get; init; }
    public Vector2 ToVector2() => new Vector2(X, Y);
}

public class AnimationData
{
    public AnimatedSprite AnimatedSprite { get; set; }
    public Rectangle SourceRect { get; set; }
}

/// <summary>
/// 单个动画定义，对应 Aseprite 文件中的 Tag。
/// </summary>
public class AnimationDefinition
{
    public string TagName { get; init; }
    public RectangleData SourceRect { get; init; }
}

/// <summary>
/// 定义，指向包含所有动画 Tag 的单个精灵文件。
/// </summary>
public class EntityConfig
{
    public string Name { get; init; }
    public string File { get; init; }
    public OriginData Origin { get; init; } // 改为 OriginData
    public Dictionary<string, AnimationDefinition> Animations { get; init; } = new();

    public override string ToString()
    {
        return $"Name: {Name}, File: {File}, Origin: {Origin}, Animations: {Animations.Count}";
    }
}


public class EntityAsset
{
    public string Name { get; set; }
    public Vector2 Origin { get; set; }
    public Dictionary<string, AnimationData> Animations { get; set; } = new();
}

/// <summary>
/// 数据驱动的资源管理器，用于加载和提供 Aseprite Tag 的实体动画。
/// </summary>
public class AssetManager
{
    /// <summary>
    /// 内部类：缓存已加载的纹理和对应的 Aseprite 文件数据。
    /// </summary>
    private class AnimationSource
    {
        public AsepriteFile AsepriteFile { get; }
        public SpriteSheet SpriteSheet { get; }

        public AnimationSource(AsepriteFile asepriteFile, GraphicsDevice graphicsDevice)
        {
            AsepriteFile = asepriteFile;
            SpriteSheet = asepriteFile.CreateSpriteSheet(
                graphicsDevice,
                onlyVisibleLayers: true,
                includeBackgroundLayer: false,
                includeTilemapLayers: false,
                mergeDuplicateFrames: true,
                borderPadding: 0,
                spacing: 0,
                innerPadding: 0
            );
        }
    }

    private readonly ContentManager _contentManager;
    private readonly GraphicsDevice _graphicsDevice;

    // 实体定义：实体名 -> 实体定义
    private Dictionary<string, EntityConfig> _entityConfigs = new();

    // 资源缓存：精灵文件名 -> AnimationSource
    private readonly Dictionary<string, AnimationSource> _sourceCache = new();

    public AssetManager(ContentManager contentManager, GraphicsDevice graphicsDevice)
    {
        _contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
    }

    public void Initialize()
    {
        // 在这里可以加载默认的实体定义或其他初始化逻辑
        LoadConfig("Animations/wolf.json");
        LoadConfig("Animations/tree.json");
    }

    /// <summary>
    /// 从指定 JSON 文件加载所有实体定义。
    /// </summary>
    /// <param name="jsonFilePath">相对于 Content 根目录的 JSON 文件路径。</param>
    public void LoadConfig(string jsonFilePath)
    {
        try
        {
            var fullPath = Path.Combine(_contentManager.RootDirectory, jsonFilePath);
            using var jsonText = File.OpenText(fullPath);
            var json = jsonText.ReadToEnd();

            var config = JsonSerializer.Deserialize<EntityConfig>(json);
            if (config == null || string.IsNullOrEmpty(config.Name) || string.IsNullOrEmpty(config.File))
            {
                throw new InvalidOperationException($"实体定义文件 '{jsonFilePath}' 格式不正确或缺少必要字段。");
            }
            _entityConfigs[config.Name] = config;
            Console.WriteLine($"已加载实体定义: {config})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载实体定义失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 获取指定动画当前帧的纹理和源矩形。
    /// </summary>
    /// <returns>找到帧返回 true，否则返回 false。</returns>
    private bool TryGetAnimateSprite(string entityName, string animationName, out AnimatedSprite animatedSprite, out Rectangle sourceRect)
    {
        sourceRect = Rectangle.Empty;
        animatedSprite = null;

        // 1. 查找实体和动画定义
        if (!_entityConfigs.TryGetValue(entityName, out var entityConfig) ||
            !entityConfig.Animations.TryGetValue(animationName, out var animDef))
        {
            Console.WriteLine($"未找到实体 '{entityName}' 或动画 '{animationName}' 的定义。");
            return false;
        }

        // 2. 获取或加载动画资源
        if (!_sourceCache.TryGetValue(entityConfig.File, out var source))
        {
            try
            {
                var aseFile = _contentManager.Load<AsepriteFile>(entityConfig.File);
                source = new AnimationSource(aseFile, _graphicsDevice);
                _sourceCache[entityConfig.File] = source;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载动画资源 '{entityConfig.File}' 失败: {ex.Message}");
                return false;
            }
        }

        animatedSprite = source.SpriteSheet.CreateAnimatedSprite(
            animDef.TagName
        );

        sourceRect = animDef.SourceRect?.ToRectangle() ?? Rectangle.Empty;

        return true;
    }

    public EntityAsset GetEntityAsset(string entityName)
    {
        if (!_entityConfigs.TryGetValue(entityName, out var entityConfig))
        {
            throw new KeyNotFoundException($"未找到实体 '{entityName}' 的定义。");
        }

        var animationData = new Dictionary<string, AnimationData>();

        foreach (var animDef in entityConfig.Animations)
        {
            if (TryGetAnimateSprite(entityName, animDef.Key, out var animatedSprite, out var sourceRect))
            {
                animationData[animDef.Key] = new AnimationData
                {
                    AnimatedSprite = animatedSprite,
                    SourceRect = sourceRect
                };
            }
            else
            {
                Console.WriteLine($"动画 '{animDef.Key}' 加载失败。");
            }
        }

        return new EntityAsset
        {
            Name = entityConfig.Name,
            Origin = entityConfig.Origin?.ToVector2() ?? Vector2.Zero,
            Animations = animationData
        };
    }
}
