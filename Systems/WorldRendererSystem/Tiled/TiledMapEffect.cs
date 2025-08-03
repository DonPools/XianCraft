using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics.Effects;

namespace XianCraft.Renderers.Tiled
{
    public interface ITiledMapEffect : IEffectMatrices, ITextureEffect
    {
        float Alpha { get; set; }
        Vector2 TextureSize { set; }
    }

    public class TiledMapEffect : DefaultEffect, ITiledMapEffect
    {
        EffectParameter _textureSizeParameter;

        public TiledMapEffect(GraphicsDevice graphicsDevice)
            : base(graphicsDevice)
        {
            Initialize();
        }

        public TiledMapEffect(GraphicsDevice graphicsDevice, byte[] byteCode)
            : base(graphicsDevice, byteCode)
        {
            Initialize();
        }

        public TiledMapEffect(Effect cloneSource)
            : base(cloneSource)
        {
            Initialize();
        }

        public Vector2 TextureSize
        {
            set
            {
                _textureSizeParameter?.SetValue(value);
            }
        }

        private void Initialize()
        {
            VertexColorEnabled = false;
            TextureEnabled = true;
            _textureSizeParameter = Parameters["TextureSize"];
        }
    }
}
