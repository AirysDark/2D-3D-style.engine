using System;
using System.Reflection;
using System.Windows;

using Microsoft.Xna.Framework.Graphics;
using System.Windows.Interop;

using Prism.Modules.D3D.Controls;

namespace GE2D3D.MapEditor.Modules.SceneViewer.MonoGame
{
    /// <summary>
    /// WPF ? D3D9 bridge hosting a MonoGame RenderTarget2D.
    /// Uses Prism.Modules.D3D.Controls.BaseD3D9DrawingSurface as the base.
    /// </summary>
    public class DrawingSurfaceD3D9 : BaseD3D9DrawingSurface
    {
        public int RenderWidth => Math.Max(1, (int)ActualWidth);
        public int RenderHeight => Math.Max(1, (int)ActualHeight);

        /// <summary>
        /// Fired once the GraphicsDevice has been created and is ready.
        /// </summary>
        public event EventHandler<GraphicsDeviceEventArgs>? LoadContent;

        private GraphicsDeviceServiceSingleton? _graphicsDeviceService;
        private RenderTarget2D? _renderTarget;

        public GraphicsDevice GraphicsDevice =>
            _graphicsDeviceService?.GraphicsDevice
            ?? throw new InvalidOperationException("GraphicsDevice is not initialized yet.");

        public DrawingSurfaceD3D9()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        // ---------------------------------------------------------
        // WPF lifecycle
        // ---------------------------------------------------------

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (_graphicsDeviceService != null)
                return;

            var width = RenderWidth;
            var height = RenderHeight;

            // Backbuffer size isn't critical because we render to our own RT,
            // but the service still needs some initial dimensions.
            _graphicsDeviceService = GraphicsDeviceServiceDX.AddRef(width, height);
            _graphicsDeviceService.DeviceResetting += OnGraphicsDeviceServiceDeviceResetting;

            SetViewport();
            EnsureRenderTarget();

            RaiseLoadContent(new GraphicsDeviceEventArgs(_graphicsDeviceService.GraphicsDevice));
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            if (_graphicsDeviceService == null)
                return;

            _graphicsDeviceService.DeviceResetting -= OnGraphicsDeviceServiceDeviceResetting;
            _graphicsDeviceService.Release(true);
            _graphicsDeviceService = null;

            RemoveBackBufferReference();
        }

        private void OnGraphicsDeviceServiceDeviceResetting(object? sender, EventArgs e)
        {
            // Our render target becomes invalid
            RemoveBackBufferReference();
            ContentNeedsRefresh = true;
        }

        // ---------------------------------------------------------
        // BaseD3D9DrawingSurface overrides
        // ---------------------------------------------------------

        protected override void RemoveBackBufferReference()
        {
            if (_renderTarget != null)
            {
                _renderTarget.Dispose();
                _renderTarget = null;
            }

            base.RemoveBackBufferReference();
        }

        protected override void EnsureRenderTarget()
        {
            if (_renderTarget == null)
            {
                _renderTarget = new RenderTarget2D(
                    GraphicsDevice,
                    RenderWidth,
                    RenderHeight,
                    false,
                    SurfaceFormat.Bgra32,
                    DepthFormat.Depth24Stencil8,
                    1,
                    RenderTargetUsage.PlatformContents,
                    shared: true);

                RenderTargetPtr = GetRenderTargetPtr();
                RenderTargetRectangle = new Int32Rect(0, 0, _renderTarget.Width, _renderTarget.Height);
            }

            base.EnsureRenderTarget();
        }

        protected virtual void RaiseLoadContent(GraphicsDeviceEventArgs args)
            => LoadContent?.Invoke(this, args);

        protected override void RaiseViewportChanged(SizeChangedInfo args)
        {
            SetViewport();
            base.RaiseViewportChanged(args);
        }

        protected override void RaiseDraw(DrawEventArgs args)
        {
            if (_renderTarget == null)
            {
                EnsureRenderTarget();
            }

            GraphicsDevice.SetRenderTarget(_renderTarget);
            SetViewport();

            // Let subscribers draw into the RT
            base.RaiseDraw(args);

            GraphicsDevice.Flush();
            GraphicsDevice.SetRenderTarget(null);
        }

        // ---------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------

        private void SetViewport()
        {
            if (_graphicsDeviceService == null)
                return;

            _graphicsDeviceService.GraphicsDevice.Viewport = new Viewport(
                0, 0, RenderWidth, RenderHeight);
        }

        private IntPtr GetRenderTargetPtr()
        {
            if (_renderTarget == null)
                throw new InvalidOperationException("Render target has not been created yet.");

            // Try MonoGame's OpenGL field (glTexture)
            var glInfo = typeof(RenderTarget2D).GetField("glTexture", BindingFlags.Instance | BindingFlags.NonPublic);
            var glTexture = glInfo != null ? (int)(glInfo.GetValue(_renderTarget) ?? 0) : 0;
            var glPtr = glTexture != 0 ? new IntPtr(glTexture) : IntPtr.Zero;

            // Try DirectX shared handle
            var dxInfo = typeof(RenderTarget2D).GetMethod("GetSharedHandle", BindingFlags.Instance | BindingFlags.Public);
            var dxPtr = dxInfo != null ? (IntPtr)(dxInfo.Invoke(_renderTarget, null) ?? IntPtr.Zero) : IntPtr.Zero;

            if (glPtr != IntPtr.Zero)
                return glPtr;
            if (dxPtr != IntPtr.Zero)
                return dxPtr;

            throw new InvalidOperationException("Unable to obtain render target handle.");
        }

        // ---------------------------------------------------------
        // IDisposable
        // ---------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _renderTarget?.Dispose();
                _renderTarget = null;

                _graphicsDeviceService?.Release(true);
                _graphicsDeviceService = null;
            }

            base.Dispose(disposing);
        }

        ~DrawingSurfaceD3D9()
        {
            Dispose(false);
        }
    }
}