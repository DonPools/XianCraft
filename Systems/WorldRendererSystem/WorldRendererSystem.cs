
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace XianCraft.Systems;

public class WorldRendererSystem : ISystem<SpriteBatch>
{
    private readonly World _world;
    private readonly GraphicsDevice _graphicsDevice;
    private bool _isEnabled = true;

    public WorldRendererSystem(World world, GraphicsDevice graphicsDevice)
    {
        _world = world;
        _graphicsDevice = graphicsDevice;
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public void Update(SpriteBatch spriteBatch)
    {
        if (!_isEnabled) return;

        spriteBatch.Begin();
        
        // Render logic goes here, e.g., drawing entities with specific components
        
        spriteBatch.End();
    }

    public void Dispose()
    {
    }
}