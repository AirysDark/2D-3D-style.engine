using Microsoft.Xna.Framework.Graphics;

namespace Prism.Modules.D3D.Rendering
{
    /// <summary>
    /// Provides helpers for creating and managing render targets for previews/thumbnails.
    /// </summary>
    public interface IRenderTargetService
    {
        RenderTarget2D CreateRenderTarget(
            GraphicsDevice device,
            int width,
            int height,
            bool mipMap = false,
            SurfaceFormat surfaceFormat = SurfaceFormat.Color,
            DepthFormat depthFormat = DepthFormat.None,
            int multiSampleCount = 0,
            RenderTargetUsage usage = RenderTargetUsage.DiscardContents);
    }
}
