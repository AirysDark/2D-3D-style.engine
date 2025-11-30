using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Prism.Modules.D3D.Cameras;

namespace Prism.Modules.D3D.Rendering
{
    /// <summary>
    /// A single render layer in the editor's rendering pipeline.
    /// </summary>
    public interface IRenderLayer
    {
        /// <summary>
        /// Whether this layer should currently be drawn.
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Render order. Lower values draw first.
        /// </summary>
        int Order { get; set; }

        /// <summary>
        /// Draws this layer using the provided graphics device and camera.
        /// </summary>
        void Draw(GraphicsDevice device, GameTime gameTime, ICamera camera);
    }
}
