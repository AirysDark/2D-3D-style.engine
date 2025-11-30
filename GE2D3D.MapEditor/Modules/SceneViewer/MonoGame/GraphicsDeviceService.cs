using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GE2D3D.MapEditor.Modules.SceneViewer.MonoGame
{
    /// <summary>
    /// Used to initialize and control the presentation of the graphics device.
    /// Prism-friendly version (no MEF / Caliburn).
    /// </summary>
    public partial class GraphicsDeviceService : GraphicsDeviceServiceSingleton, IDisposable
    {
        // Static singleton instance managed manually (no IoC / MEF).
        private static GraphicsDeviceService? _instance;

        // Keep track of how many controls are sharing the singletonInstance.
        private static int _referenceCount;

        /// <summary>
        /// Gets a reference to the singleton instance.
        /// </summary>
        public static GraphicsDeviceService AddRef(int width, int height)
        {
            // Lazy-create singleton on first use.
            var singletonInstance = _instance ??= new GraphicsDeviceService();

            // Increment the "how many controls sharing the device" reference count.
            if (Interlocked.Increment(ref _referenceCount) == 1)
            {
                // First control to start using the device:
                // create / apply settings.
                singletonInstance.ApplyChanges();
                // If you later want to resize based on width/height, hook that here.
                // singletonInstance.EnsureGraphicsDevice(width, height);
            }

            return singletonInstance;
        }

        /// <summary>
        /// Releases a reference to the singleton instance.
        /// </summary>
        public override void Release(bool disposing)
        {
            // Decrement the "how many controls sharing the device" reference count.
            if (Interlocked.Decrement(ref _referenceCount) == 0)
            {
                // Last control finished using the device: dispose graphics device.
                if (disposing && GraphicsDevice != null)
                {
                    DeviceDisposing?.Invoke(this, EventArgs.Empty);
                    GraphicsDevice.Dispose();
                }

                GraphicsDevice = null;
            }
        }

        //private readonly Game _game;
        private bool _initialized;

        private int _preferredBackBufferHeight;
        private int _preferredBackBufferWidth;
        private SurfaceFormat _preferredBackBufferFormat;
        private DepthFormat _preferredDepthStencilFormat;
        private bool _preferMultiSampling;
        private bool _synchronizedWithVerticalRetrace = true;
        private bool _disposed;
        private bool _hardwareModeSwitch = true;
        private bool _wantFullScreen;
        private GraphicsProfile _graphicsProfile;
        // dirty flag for ApplyChanges
        private bool _shouldApplyChanges;

        /// <summary>
        /// The default back buffer width.
        /// </summary>
        public static readonly int DefaultBackBufferWidth = 800;

        /// <summary>
        /// The default back buffer height.
        /// </summary>
        public static readonly int DefaultBackBufferHeight = 480;

        /// <summary>
        /// Optional override for platform specific defaults.
        /// </summary>
        partial void PlatformConstruct();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public GraphicsDeviceService()
        {
            _preferredBackBufferFormat = SurfaceFormat.Color;
            _preferredDepthStencilFormat = DepthFormat.Depth24;
            _synchronizedWithVerticalRetrace = true;

            // For now just use a tiny dummy rect; actual size comes from presentation parameters.
            var clientBounds = new Rectangle(0, 0, 1, 1);

            if (clientBounds.Width >= clientBounds.Height)
            {
                _preferredBackBufferWidth = clientBounds.Width;
                _preferredBackBufferHeight = clientBounds.Height;
            }
            else
            {
                _preferredBackBufferWidth = clientBounds.Height;
                _preferredBackBufferHeight = clientBounds.Width;
            }

            // Default to windowed mode.
            _wantFullScreen = false;

            GraphicsProfile = GraphicsProfile.HiDef;

            // Allow platform-specific initialization.
            PlatformConstruct();
        }

        ~GraphicsDeviceService()
        {
            Dispose(false);
        }

        private void CreateDevice()
        {
            if (GraphicsDevice != null)
                return;

            try
            {
                if (!_initialized)
                    Initialize();

                var gdi = DoPreparingDeviceSettings();
                CreateDevice(gdi);
            }
            catch (NoSuitableGraphicsDeviceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new NoSuitableGraphicsDeviceException("Failed to create graphics device!", ex);
            }
        }

        private void CreateDevice(GraphicsDeviceInformation gdi)
        {
            if (GraphicsDevice != null)
                return;

            GraphicsDevice = new GraphicsDevice(gdi.Adapter, gdi.GraphicsProfile, gdi.PresentationParameters);
            _shouldApplyChanges = false;

            // hook up reset events
            GraphicsDevice.DeviceReset += (sender, args) => OnDeviceReset(args);
            GraphicsDevice.DeviceResetting += (sender, args) => OnDeviceResetting(args);

            OnDeviceCreated(EventArgs.Empty);
        }

        #region IGraphicsDeviceService Members

        public override event EventHandler<EventArgs>? DeviceCreated;
        public override event EventHandler<EventArgs>? DeviceDisposing;
        public override event EventHandler<EventArgs>? DeviceReset;
        public override event EventHandler<EventArgs>? DeviceResetting;
        public event EventHandler<PreparingDeviceSettingsEventArgs>? PreparingDeviceSettings;
        public event EventHandler<EventArgs>? Disposed;

        protected void OnDeviceDisposing(EventArgs e) => DeviceDisposing?.Invoke(this, e);

        protected void OnDeviceResetting(EventArgs e) => DeviceResetting?.Invoke(this, e);

        internal void OnDeviceReset(EventArgs e) => DeviceReset?.Invoke(this, e);

        internal void OnDeviceCreated(EventArgs e) => DeviceCreated?.Invoke(this, e);

        /// <summary>
        /// This populates a GraphicsDeviceInformation instance and invokes PreparingDeviceSettings to
        /// allow users to change the settings. Then returns that GraphicsDeviceInformation.
        /// Throws NullReferenceException if users set GraphicsDeviceInformation.PresentationParameters to null.
        /// </summary>
        private GraphicsDeviceInformation DoPreparingDeviceSettings()
        {
            var gdi = new GraphicsDeviceInformation();
            PrepareGraphicsDeviceInformation(gdi);
            var preparingDeviceSettingsHandler = PreparingDeviceSettings;

            if (preparingDeviceSettingsHandler != null)
            {
                // this allows users to overwrite settings through the argument
                var args = new PreparingDeviceSettingsEventArgs(gdi);
                preparingDeviceSettingsHandler(this, args);

                if (gdi.PresentationParameters == null || gdi.Adapter == null)
                    throw new NullReferenceException("Members should not be set to null in PreparingDeviceSettingsEventArgs");
            }

            return gdi;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (GraphicsDevice != null)
                    {
                        GraphicsDevice.Dispose();
                        GraphicsDevice = null;
                    }
                }
                _disposed = true;
                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        partial void PlatformApplyChanges();

        partial void PlatformPreparePresentationParameters(PresentationParameters presentationParameters);

        private void PreparePresentationParameters(PresentationParameters presentationParameters)
        {
            presentationParameters.BackBufferFormat = _preferredBackBufferFormat;
            presentationParameters.BackBufferWidth = _preferredBackBufferWidth;
            presentationParameters.BackBufferHeight = _preferredBackBufferHeight;
            presentationParameters.DepthStencilFormat = _preferredDepthStencilFormat;
            presentationParameters.IsFullScreen = _wantFullScreen;
            //presentationParameters.HardwareModeSwitch = _hardwareModeSwitch;
            presentationParameters.PresentationInterval = _synchronizedWithVerticalRetrace
                ? PresentInterval.One
                : PresentInterval.Immediate;
            presentationParameters.DisplayOrientation = DisplayOrientation.Default;
            presentationParameters.DeviceWindowHandle =
                new WindowInteropHelper(Application.Current.MainWindow).Handle;

            if (_preferMultiSampling)
            {
                // If you want to enable MSAA, compute a valid MultiSampleCount here.
                //presentationParameters.MultiSampleCount = GraphicsDevice != null
                //    ? GraphicsDevice.GraphicsCapabilities.MaxMultiSampleCount
                //    : 32;
            }
            else
            {
                presentationParameters.MultiSampleCount = 0;
            }

            PlatformPreparePresentationParameters(presentationParameters);
        }

        private void PrepareGraphicsDeviceInformation(GraphicsDeviceInformation gdi)
        {
            gdi.Adapter = GraphicsAdapter.DefaultAdapter;
            gdi.GraphicsProfile = GraphicsProfile;
            var pp = new PresentationParameters();
            PreparePresentationParameters(pp);
            gdi.PresentationParameters = pp;
        }

        /// <summary>
        /// Applies any pending property changes to the graphics device.
        /// </summary>
        public void ApplyChanges()
        {
            // If the device hasn't been created then create it now.
            if (GraphicsDevice == null)
                CreateDevice();

            if (!_shouldApplyChanges)
                return;

            _shouldApplyChanges = false;

            // Allow for optional platform specific behavior.
            PlatformApplyChanges();

            // Get updated settings.
            var gdi = DoPreparingDeviceSettings();

            if (gdi.GraphicsProfile != GraphicsDevice.GraphicsProfile)
            {
                // if the GraphicsProfile changed we need to create a new GraphicsDevice
                DisposeGraphicsDevice();
                CreateDevice(gdi);
                return;
            }

            GraphicsDevice.Reset(gdi.PresentationParameters);
        }

        private void DisposeGraphicsDevice()
        {
            GraphicsDevice.Dispose();
            DeviceDisposing?.Invoke(this, EventArgs.Empty);
            GraphicsDevice = null;
        }

        partial void PlatformInitialize(PresentationParameters presentationParameters);

        private void Initialize()
        {
            var presentationParameters = new PresentationParameters();
            PreparePresentationParameters(presentationParameters);

            // Allow for any per-platform changes to the presentation.
            PlatformInitialize(presentationParameters);

            _initialized = true;
        }

        /// <summary>
        /// Toggles between windowed and fullscreen modes.
        /// </summary>
        /// <remarks>
        /// Note that on platforms that do not support windowed modes this has no effect.
        /// </remarks>
        public void ToggleFullScreen()
        {
            IsFullScreen = !IsFullScreen;
            ApplyChanges();
        }

        /// <summary>
        /// The profile which determines the graphics feature level.
        /// </summary>
        public GraphicsProfile GraphicsProfile
        {
            get => _graphicsProfile;
            set
            {
                _shouldApplyChanges = true;
                _graphicsProfile = value;
            }
        }

        public bool IsFullScreen
        {
            get => _wantFullScreen;
            set
            {
                _shouldApplyChanges = true;
                _wantFullScreen = value;
            }
        }

        public bool HardwareModeSwitch
        {
            get => _hardwareModeSwitch;
            set
            {
                _shouldApplyChanges = true;
                _hardwareModeSwitch = value;
            }
        }

        public bool PreferMultiSampling
        {
            get => _preferMultiSampling;
            set
            {
                _shouldApplyChanges = true;
                _preferMultiSampling = value;
            }
        }

        public SurfaceFormat PreferredBackBufferFormat
        {
            get => _preferredBackBufferFormat;
            set
            {
                _shouldApplyChanges = true;
                _preferredBackBufferFormat = value;
            }
        }

        public int PreferredBackBufferHeight
        {
            get => _preferredBackBufferHeight;
            set
            {
                _shouldApplyChanges = true;
                _preferredBackBufferHeight = value;
            }
        }

        public int PreferredBackBufferWidth
        {
            get => _preferredBackBufferWidth;
            set
            {
                _shouldApplyChanges = true;
                _preferredBackBufferWidth = value;
            }
        }

        public DepthFormat PreferredDepthStencilFormat
        {
            get => _preferredDepthStencilFormat;
            set
            {
                _shouldApplyChanges = true;
                _preferredDepthStencilFormat = value;
            }
        }

        public bool SynchronizeWithVerticalRetrace
        {
            get => _synchronizedWithVerticalRetrace;
            set
            {
                _shouldApplyChanges = true;
                _synchronizedWithVerticalRetrace = value;
            }
        }
    }
}