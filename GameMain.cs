using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Tiled;
using DefaultEcs;
using DefaultEcs.System;

using XianCraft.Systems;
using XianCraft.Utils;
using System.IO;

namespace XianCraft;

public class GameMain : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // ECS
    private World _world;
    private SequentialSystem<float> _updateSystems;
    private SequentialSystem<SpriteBatch> _renderSystems;

    private EnitityManager _entityManager;
    private SpriteFont _spriteFont;
    private TiledMap _metaMap;

    private Effect _effect;

    public GameMain()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1200;
        _graphics.PreferredBackBufferHeight = 800;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _world = new World();
        _entityManager = new EnitityManager(_world);
        _entityManager.Initialize();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteFont = Content.Load<SpriteFont>("simsun");
        _metaMap = Content.Load<TiledMap>("Tilemap/meta");
        var shaderExtention = UtilsHelper.GetShaderExtension();
        var bytecode = File.ReadAllBytes(Path.Combine(Content.RootDirectory, "Effects", $"PixelPerfectEffect.{shaderExtention}.mgfxo"));
        _effect = new Effect(GraphicsDevice, bytecode);

        _spriteBatch = new SpriteBatch(GraphicsDevice);        

        _updateSystems = new SequentialSystem<float>(
            new CameraSystem(_world, GraphicsDevice),
            new MouseInputSystem(_world, _metaMap),
            new WorldGenerationSystem(_world, _metaMap)
        );

        _renderSystems = new SequentialSystem<SpriteBatch>(
            new WorldRendererSystem(_world, GraphicsDevice, _metaMap, _effect),
            new UISystem(_world, GraphicsDevice, _spriteFont)
        );
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        _updateSystems.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _renderSystems.Update(_spriteBatch);

        base.Draw(gameTime);
    }
}
