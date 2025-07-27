using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DefaultEcs;
using DefaultEcs.System;

using XianCraft.Systems;

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

    public GameMain()
    {
        _graphics = new GraphicsDeviceManager(this);
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
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _updateSystems = new SequentialSystem<float>(
            new CameraSystem(_world, GraphicsDevice),
            new WorldGenerationSystem(_world)
        );

        _renderSystems = new SequentialSystem<SpriteBatch>(
            new WorldRendererSystem(_world, GraphicsDevice)
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
