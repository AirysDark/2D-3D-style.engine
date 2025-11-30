using Microsoft.Xna.Framework.Graphics;

namespace Prism.Modules.D3D.Rendering
{
    /// <summary>
    /// Simple wrapper for editor rendering context.
    /// </summary>
    public interface IRenderingContext
    {
        GraphicsDevice Device { get; }
        SpriteBatch SpriteBatch { get; }
    }
}
