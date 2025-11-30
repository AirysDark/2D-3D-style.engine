using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using GE2D3D.MapEditor.Components.Render;
using GE2D3D.MapEditor.Data;
using GE2D3D.MapEditor.Modules.SceneViewer.ViewModels;
using GE2D3D.MapEditor.Renders;

namespace GE2D3D.MapEditor.Modules.SceneViewer.Views
{
    public partial class SceneView : UserControl
    {
        // Mouse tracking for camera controls
        private bool _isRightMouseDown;
        private Point _lastMousePos;

        // Render bootstrap coming from outside (shell / module)
        private RenderBootstrap? _bootstrap;

        // If a level is requested before AttachBootstrap, we cache it here
        private LevelInfo? _pendingLevel;

        /// <summary>
        /// Expose bootstrap if the ViewModel or host needs it.
        /// </summary>
        public RenderBootstrap? Bootstrap => _bootstrap;

        public SceneView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            // Hook this view into the SceneViewModel so it can call LoadLevel/RefreshFromLevel
            if (DataContext is SceneViewModel vm)
            {
                vm.AttachView(this);
            }

            // GameHost is defined in XAML (the element hosting the MonoGame control)
            if (GameHost != null)
            {
                // Mouse
                GameHost.MouseMove += ViewHost_OnMouseMove;
                GameHost.MouseWheel += ViewHost_OnMouseWheel;
                GameHost.MouseRightButtonDown += ViewHost_OnMouseRightButtonDown;
                GameHost.MouseRightButtonUp += ViewHost_OnMouseRightButtonUp;

                // Keyboard
                GameHost.KeyDown += ViewHost_OnKeyDown;
                GameHost.KeyUp += ViewHost_OnKeyUp;

                GameHost.Focusable = true;
                GameHost.Focus();
            }
        }

        /// <summary>
        /// Called by the hosting module to connect this view to the engine.
        /// NOTE: We do NOT override DataContext here so Prism can keep the VM.
        /// </summary>
        public void AttachBootstrap(RenderBootstrap bootstrap)
        {
            _bootstrap = bootstrap ?? throw new ArgumentNullException(nameof(bootstrap));

            // AA combo was removed from the toolbar, this is now a no-op.
            SyncAaComboFromSettings();

            // If a level was requested before bootstrap was ready, load it now.
            if (_pendingLevel != null)
            {
                _bootstrap.ReloadLevel(_pendingLevel);
                _pendingLevel = null;
            }
        }

        /// <summary>
        /// Direct API for the host/viewmodel to load a level into the viewer.
        /// </summary>
        public void LoadLevel(LevelInfo levelInfo)
        {
            if (levelInfo == null)
                throw new ArgumentNullException(nameof(levelInfo));

            // If bootstrap isn't ready yet, remember this level and
            // load it once AttachBootstrap is called.
            if (_bootstrap == null)
            {
                _pendingLevel = levelInfo;
                return;
            }

            _bootstrap.ReloadLevel(levelInfo);
            SyncAaComboFromSettings();
        }

        /// <summary>
        /// Called by the ViewModel whenever LevelInfo changes.
        /// </summary>
        public void RefreshFromLevel(LevelInfo? levelInfo)
        {
            if (levelInfo == null)
                return;

            LoadLevel(levelInfo);
        }

        /// <summary>
        /// AA combo has been removed from the toolbar; keep this as a no-op so
        /// existing calls remain valid.
        /// </summary>
        private void SyncAaComboFromSettings()
        {
            // AA dropdown was removed; nothing to sync anymore.
        }

        // ---------------------------------------------------------
        // Toolbar handlers (wired from XAML)
        // ---------------------------------------------------------

        public void SetAntiAliasingFromMenu(Components.Render.AntiAliasing mode)
        {
            if (_bootstrap == null)
                return;

            _bootstrap.SetAntiAliasing(mode);
            // No AA combo anymore, so nothing additional to sync.
        }

        private void OnAaChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_bootstrap == null)
                return;

            if (sender is not ComboBox combo)
                return;

            if (combo.SelectedItem is not ComboBoxItem selected)
                return;

            var tag = (selected.Tag as string) ?? selected.Content?.ToString();
            if (string.IsNullOrWhiteSpace(tag))
                return;

            if (Enum.TryParse<AntiAliasing>(tag, ignoreCase: true, out var mode))
            {
                _bootstrap.SetAntiAliasing(mode);
            }
        }

        private void OnCamPresetTop(object sender, RoutedEventArgs e)
        {
            _bootstrap?.CameraController?.SetPresetTop();
        }

        private void OnCamPresetFront(object sender, RoutedEventArgs e)
        {
            _bootstrap?.CameraController?.SetPresetFront();
        }

        private void OnCamPresetSide(object sender, RoutedEventArgs e)
        {
            _bootstrap?.CameraController?.SetPresetSide();
        }

        private void OnFocusSelection(object sender, RoutedEventArgs e)
        {
            _bootstrap?.FocusOnSelection();
        }

        private void OnReloadLevel(object sender, RoutedEventArgs e)
        {
            _bootstrap?.ReloadLevel();
        }

        // ---------------------------------------------------------
        // Mouse hooks (wired to GameHost grid)
        // ---------------------------------------------------------

        private void ViewHost_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isRightMouseDown)
                return;

            if (_bootstrap?.CameraController == null)
                return;

            var host = GameHost;
            if (host == null)
                return;

            var pos = e.GetPosition(host);
            var delta = pos - _lastMousePos;

            _bootstrap.CameraController.OnMouseDrag(
                (float)delta.X,
                (float)delta.Y,
                rightButtonDown: true);

            _lastMousePos = pos;
        }

        private void ViewHost_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_bootstrap?.CameraController == null)
                return;

            _bootstrap.CameraController.OnMouseWheel(e.Delta);
        }

        private void ViewHost_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isRightMouseDown = true;

            var host = GameHost;
            if (host == null)
                return;

            _lastMousePos = e.GetPosition(host);

            Mouse.Capture(host);

            if (!host.IsKeyboardFocusWithin)
            {
                host.Focus();
            }
        }

        private void ViewHost_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isRightMouseDown = false;
            Mouse.Capture(null);

            _bootstrap?.CameraController?.EndDrag();
        }

        // ---------------------------------------------------------
        // Keyboard hooks (WASD / QE, etc.)
        // ---------------------------------------------------------

        private void ViewHost_OnKeyDown(object sender, KeyEventArgs e)
        {
            _bootstrap?.CameraController?.OnKeyDown(e.Key);
        }

        private void ViewHost_OnKeyUp(object sender, KeyEventArgs e)
        {
            _bootstrap?.CameraController?.OnKeyUp(e.Key);
        }

        // ---------------------------------------------------------
        // Layer toggles + volumes/triggers (wired from XAML)
        // ---------------------------------------------------------

        private void OnToggleCollisionLayer(object sender, RoutedEventArgs e)
        {
            if (_bootstrap == null)
                return;

            var isChecked = (sender as CheckBox)?.IsChecked == true;

            // Adjust enum / API name to match your RenderBootstrap
            _bootstrap.SetLayerVisible(SceneLayer.Collision, isChecked);
        }

        private void OnTogglePropsLayer(object sender, RoutedEventArgs e)
        {
            if (_bootstrap == null)
                return;

            var isChecked = (sender as CheckBox)?.IsChecked == true;
            _bootstrap.SetLayerVisible(SceneLayer.Props, isChecked);
        }

        private void OnToggleLightsLayer(object sender, RoutedEventArgs e)
        {
            if (_bootstrap == null)
                return;

            var isChecked = (sender as CheckBox)?.IsChecked == true;
            _bootstrap.SetLayerVisible(SceneLayer.Lights, isChecked);
        }

        private void OnToggleTriggersLayer(object sender, RoutedEventArgs e)
        {
            if (_bootstrap == null)
                return;

            var isChecked = (sender as CheckBox)?.IsChecked == true;
            _bootstrap.SetLayerVisible(SceneLayer.Triggers, isChecked);

            // If you have a separate "debug volumes" visualization, hook that here too:
            // _bootstrap.SetTriggerVolumesVisible(isChecked);
        }
    }
}