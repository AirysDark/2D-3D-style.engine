using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Prism.Modules.D3D.Cameras;

namespace Prism.Modules.D3D.Overlays
{
    /// <summary>
    /// Simple debug overlay that can show text (FPS, camera, etc.).
    /// Does nothing unless Font and TextProvider are set.
    /// </summary>
    public class DebugOverlayLayer : Rendering.IRenderLayer
    {
        private SpriteBatch _spriteBatch;

        public bool Enabled { get; set; } = true;
        public int Order { get; set; } = int.MaxValue; // draw last by default

        /// <summary>
        /// Font used to draw debug text. If null, nothing will be drawn.
        /// </summary>
        public SpriteFont Font { get; set; }

        /// <summary>
        /// Function that returns the text to draw.
        /// </summary>
        public Func<GameTime, ICamera, string> TextProvider { get; set; }

        /// <summary>
        /// Screen-space position for the text.
        /// </summary>
        public Vector2 Position { get; set; } = new Vector2(10, 10);

        public Color Color { get; set; } = Color.White;

        public void Draw(GraphicsDevice device, GameTime gameTime, ICamera camera)
        {
            if (!Enabled || Font == null || TextProvider == null)
                return;

            if (_spriteBatch == null || _spriteBatch.GraphicsDevice != device)
            {
                _spriteBatch?.Dispose();
                _spriteBatch = new SpriteBatch(device);
            }

            string text = TextProvider(gameTime, camera);
            if (string.IsNullOrEmpty(text))
                return;

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _spriteBatch.DrawString(Font, text, Position, Color);
            _spriteBatch.End();
        }
    }
}
