using System;
using Microsoft.Xna.Framework.Graphics;

namespace Prism.Modules.D3D.Rendering
{
    /// <summary>
    /// Default implementation of IRenderingContext.
    /// </summary>
    public class RenderingContext : IRenderingContext, IDisposable
    {
        public GraphicsDevice Device { get; }
        public SpriteBatch SpriteBatch { get; }

        public RenderingContext(GraphicsDevice device)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
            SpriteBatch = new SpriteBatch(device);
        }

        public void Dispose()
        {
            SpriteBatch?.Dispose();
        }
    }
}
