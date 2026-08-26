using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using GE2D3D.MapEditor.Components.Render;
using GE2D3D.MapEditor.Data;
using GE2D3D.MapEditor.Modules.SceneViewer.ViewModels;
using GE2D3D.MapEditor.Renders;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

namespace GE2D3D.MapEditor.Modules.SceneViewer.Views
{
    public partial class SceneView : UserControl
    {
        private bool _isRightMouseDown;
        private Point _lastMousePos;
        private RenderBootstrap? _bootstrap;
        private LevelInfo? _pendingLevel;

        public RenderBootstrap? Bootstrap => _bootstrap;

        public SceneView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            if (DataContext is SceneViewModel vm)
                vm.AttachView(this);

            if (GameHost != null)
            {
                GameHost.MouseMove += ViewHost_OnMouseMove;
                GameHost.MouseWheel += ViewHost_OnMouseWheel;
                GameHost.MouseRightButtonDown += ViewHost_OnMouseRightButtonDown;
                GameHost.MouseRightButtonUp += ViewHost_OnMouseRightButtonUp;
                GameHost.KeyDown += ViewHost_OnKeyDown;
                GameHost.KeyUp += ViewHost_OnKeyUp;
                GameHost.Focusable = true;
                GameHost.Focus();
            }
        }

        public void AttachBootstrap(RenderBootstrap bootstrap)
        {
            _bootstrap = bootstrap ?? throw new ArgumentNullException(nameof(bootstrap));
            SyncAaComboFromSettings();

            if (_pendingLevel != null)
            {
                _bootstrap.ReloadLevel(_pendingLevel);
                AutoFrameLoadedLevel();
                _pendingLevel = null;
            }
        }

        public void LoadLevel(LevelInfo levelInfo)
        {
            if (levelInfo == null)
                throw new ArgumentNullException(nameof(levelInfo));

            if (_bootstrap == null)
            {
                _pendingLevel = levelInfo;
                return;
            }

            _bootstrap.ReloadLevel(levelInfo);
            AutoFrameLoadedLevel();
            SyncAaComboFromSettings();
        }

        private void AutoFrameLoadedLevel()
        {
            var level = _bootstrap?.Level;
            var camera = _bootstrap?.Camera;
            if (level == null || camera == null || level.AllEntities.Count == 0)
                return;

            var min = new XnaVector3(float.MaxValue);
            var max = new XnaVector3(float.MinValue);
            var found = false;

            foreach (var entity in level.AllEntities)
            {
                if (entity == null)
                    continue;

                var size = entity.Size;
                if (size == XnaVector3.Zero)
                    size = XnaVector3.One;

                var scale = entity.Scale;
                if (scale == XnaVector3.Zero)
                    scale = XnaVector3.One;

                var half = XnaVector3.Abs(size * scale) * 0.5f;
                var position = entity.Position;

                min = XnaVector3.Min(min, position - half);
                max = XnaVector3.Max(max, position + half);
                found = true;
            }

            if (!found)
                return;

            var center = (min + max) * 0.5f;
            var extents = max - min;
            var radius = extents.Length() * 0.5f;

            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius < 8f)
                radius = 8f;

            var distance = radius * 2.25f;
            camera.Position = center + new XnaVector3(distance, distance * 0.75f, distance);
            camera.Target = center;
        }

        public void RefreshFromLevel(LevelInfo? levelInfo)
        {
            if (levelInfo != null)
                LoadLevel(levelInfo);
        }

        public void SetAntiAliasingFromMenu(AntiAliasing mode)
        {
            _bootstrap?.SetAntiAliasing(mode);
        }

        private void SyncAaComboFromSettings()
        {
        }

        private void OnAaChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_bootstrap == null || sender is not ComboBox combo || combo.SelectedItem is not ComboBoxItem selected)
                return;

            var tag = (selected.Tag as string) ?? selected.Content?.ToString();
            if (!string.IsNullOrWhiteSpace(tag) && Enum.TryParse<AntiAliasing>(tag, true, out var mode))
                _bootstrap.SetAntiAliasing(mode);
        }

        private void OnCamPresetTop(object sender, RoutedEventArgs e) => _bootstrap?.CameraController?.SetPresetTop();
        private void OnCamPresetFront(object sender, RoutedEventArgs e) => _bootstrap?.CameraController?.SetPresetFront();
        private void OnCamPresetSide(object sender, RoutedEventArgs e) => _bootstrap?.CameraController?.SetPresetSide();
        private void OnFocusSelection(object sender, RoutedEventArgs e) => _bootstrap?.FocusOnSelection();

        private void OnReloadLevel(object sender, RoutedEventArgs e)
        {
            _bootstrap?.ReloadLevel();
            AutoFrameLoadedLevel();
        }

        private void ViewHost_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isRightMouseDown || _bootstrap?.CameraController == null || GameHost == null)
                return;

            var pos = e.GetPosition(GameHost);
            var delta = pos - _lastMousePos;
            _bootstrap.CameraController.OnMouseDrag((float)delta.X, (float)delta.Y, true);
            _lastMousePos = pos;
        }

        private void ViewHost_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _bootstrap?.CameraController?.OnMouseWheel(e.Delta);
        }

        private void ViewHost_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isRightMouseDown = true;
            if (GameHost == null)
                return;

            _lastMousePos = e.GetPosition(GameHost);
            Mouse.Capture(GameHost);
            if (!GameHost.IsKeyboardFocusWithin)
                GameHost.Focus();
        }

        private void ViewHost_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isRightMouseDown = false;
            Mouse.Capture(null);
            _bootstrap?.CameraController?.EndDrag();
        }

        private void ViewHost_OnKeyDown(object sender, KeyEventArgs e) => _bootstrap?.CameraController?.OnKeyDown(e.Key);
        private void ViewHost_OnKeyUp(object sender, KeyEventArgs e) => _bootstrap?.CameraController?.OnKeyUp(e.Key);

        private void OnToggleCollisionLayer(object sender, RoutedEventArgs e)
        {
            if (_bootstrap != null)
                _bootstrap.SetLayerVisible(SceneLayer.Collision, (sender as CheckBox)?.IsChecked == true);
        }

        private void OnTogglePropsLayer(object sender, RoutedEventArgs e)
        {
            if (_bootstrap != null)
                _bootstrap.SetLayerVisible(SceneLayer.Props, (sender as CheckBox)?.IsChecked == true);
        }

        private void OnToggleLightsLayer(object sender, RoutedEventArgs e)
        {
            if (_bootstrap != null)
                _bootstrap.SetLayerVisible(SceneLayer.Lights, (sender as CheckBox)?.IsChecked == true);
        }

        private void OnToggleTriggersLayer(object sender, RoutedEventArgs e)
        {
            if (_bootstrap != null)
                _bootstrap.SetLayerVisible(SceneLayer.Triggers, (sender as CheckBox)?.IsChecked == true);
        }
    }
}
