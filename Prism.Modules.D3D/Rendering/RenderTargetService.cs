using System;
using Microsoft.Xna.Framework.Graphics;

namespace Prism.Modules.D3D.Rendering
{
    /// <summary>
    /// Default implementation of IRenderTargetService.
    /// </summary>
    public class RenderTargetService : IRenderTargetService
    {
        public RenderTarget2D CreateRenderTarget(
            GraphicsDevice device,
            int width,
            int height,
            bool mipMap = false,
            SurfaceFormat surfaceFormat = SurfaceFormat.Color,
            DepthFormat depthFormat = DepthFormat.None,
            int multiSampleCount = 0,
            RenderTargetUsage usage = RenderTargetUsage.DiscardContents)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            return new RenderTarget2D(
                device,
                width,
                height,
                mipMap,
                surfaceFormat,
                depthFormat,
                multiSampleCount,
                usage);
        }
    }
}
