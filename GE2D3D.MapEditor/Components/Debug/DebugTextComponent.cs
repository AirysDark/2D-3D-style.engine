using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GE2D3D.MapEditor.Components.Debug
{
    /// <summary>
    /// Minimal debug text component stub so the project compiles.
    /// You can extend this later to actually render debug text.
    /// </summary>
    public class DebugTextComponent : IGameComponent, IUpdateable, IDrawable
    {
        // Keep references if you want to extend later.
        private readonly GraphicsDevice _graphicsDevice;
        private readonly GameComponentCollection _components;

        public DebugTextComponent(GraphicsDevice graphicsDevice, GameComponentCollection components)
        {
            _graphicsDevice = graphicsDevice;
            _components = components;

            Enabled = true;
            Visible = true;
        }

        // IGameComponent
        public void Initialize()
        {
            // No-op for now ? add init logic if you want.
        }

        // IUpdateable
        public bool Enabled { get; set; }
        public int UpdateOrder { get; set; }

        public event EventHandler<EventArgs> EnabledChanged;
        public event EventHandler<EventArgs> UpdateOrderChanged;

        public void Update(GameTime gameTime)
        {
            if (!Enabled)
                return;

            // No-op ? add debug state updates here if needed.
        }

        // IDrawable
        public bool Visible { get; set; }
        public int DrawOrder { get; set; }

        public event EventHandler<EventArgs> VisibleChanged;
        public event EventHandler<EventArgs> DrawOrderChanged;

        public void Draw(GameTime gameTime)
        {
            if (!Visible)
                return;

            // No-op ? you can later add SpriteBatch + font drawing here.
        }
    }
}