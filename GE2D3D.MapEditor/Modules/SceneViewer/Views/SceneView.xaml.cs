using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using GE2D3D.MapEditor.Components.Render;
using GE2D3D.MapEditor.Data;
using GE2D3D.MapEditor.Modules.SceneViewer.ViewModels;
using GE2D3D.MapEditor.Renders;
using GE2D3D.MapEditor.Utils;
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
            MapDiagnosticsTrace.Renderer("SceneView loaded", "Bootstrap=" + (_bootstrap == null ? "NULL" : "READY"));

            if (DataContext is SceneViewModel vm)
            {
                MapDiagnosticsTrace.Renderer("Attaching SceneView to SceneViewModel", "LevelInfo=" + (vm.LevelInfo == null ? "NULL" : "READY"));
                vm.AttachView(this);
            }
            else
            {
                MapDiagnosticsTrace.Warning("SceneView DataContext is not SceneViewModel", DataContext == null ? "DataContext=NULL" : DataContext.GetType().FullName ?? "unknown");
            }

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
                MapDiagnosticsTrace.Renderer("GameHost input attached");
            }
            else
            {
                MapDiagnosticsTrace.Error("GameHost is null during SceneView load");
            }
        }

        public void AttachBootstrap(RenderBootstrap bootstrap)
        {
            var sw = Stopwatch.StartNew();
            MapDiagnosticsTrace.Renderer("AttachBootstrap entered", "PendingLevel=" + (_pendingLevel == null ? "NO" : "YES"));

            try
            {
                _bootstrap = bootstrap ?? throw new ArgumentNullException(nameof(bootstrap));
                MapDiagnosticsTrace.Success("RenderBootstrap attached", "LevelEntities=" + (_bootstrap.LevelInfo?.Entities?.Count ?? 0));

                SyncAaComboFromSettings();

                if (_pendingLevel != null)
                {
                    var pendingCount = _pendingLevel.Entities?.Count ?? 0;
                    MapDiagnosticsTrace.Renderer("Reloading pending level into bootstrap", "InputEntities=" + pendingCount);

                    _bootstrap.ReloadLevel(_pendingLevel);

                    var worldCount = _bootstrap.GetAllEntities().Count;
                    MapDiagnosticsTrace.Renderer("Pending level ReloadLevel completed", "WorldEntities=" + worldCount);

                    AutoFrameLoadedLevel();
                    _pendingLevel = null;
                }

                // Important: maps can be loaded before the MonoGame bootstrap exists.
                // SceneViewModel.RefreshEntityList() then sees Bootstrap == null and clears
                // its UI collection. Re-attaching the view after bootstrap creation makes
                // the view model rebuild its entity list from the now-live world.
                if (DataContext is SceneViewModel vm)
                {
                    MapDiagnosticsTrace.Renderer("Refreshing SceneViewModel after bootstrap attach", "WorldEntities=" + _bootstrap.GetAllEntities().Count);
                    vm.AttachView(this);
                    MapDiagnosticsTrace.Success("SceneViewModel refreshed after bootstrap attach", "UIEntities=" + vm.Entities.Count);
                }
            }
            catch (Exception ex)
            {
                MapDiagnosticsTrace.Error("AttachBootstrap failed", ex.ToString());
                throw;
            }
            finally
            {
                sw.Stop();
                MapDiagnosticsTrace.Performance("AttachBootstrap completed", "Elapsed=" + sw.Elapsed.TotalMilliseconds.ToString("F1") + " ms");
            }
        }

        public void LoadLevel(LevelInfo levelInfo)
        {
            if (levelInfo == null)
                throw new ArgumentNullException(nameof(levelInfo));

            var inputCount = levelInfo.Entities?.Count ?? 0;
            MapDiagnosticsTrace.Renderer("SceneView.LoadLevel entered", "InputEntities=" + inputCount + " Bootstrap=" + (_bootstrap == null ? "NULL" : "READY"));

            if (_bootstrap == null)
            {
                _pendingLevel = levelInfo;
                MapDiagnosticsTrace.Warning("Bootstrap not ready; level queued", "PendingEntities=" + inputCount);
                return;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                _bootstrap.ReloadLevel(levelInfo);
                var worldCount = _bootstrap.GetAllEntities().Count;
                MapDiagnosticsTrace.Success("RenderBootstrap.ReloadLevel completed", "InputEntities=" + inputCount + " WorldEntities=" + worldCount);

                AutoFrameLoadedLevel();
                SyncAaComboFromSettings();

                if (DataContext is SceneViewModel vm)
                {
                    // Re-run AttachView so the entity browser gets the newly rebuilt
                    // RenderBootstrap world rather than stale/pre-bootstrap state.
                    vm.AttachView(this);
                    MapDiagnosticsTrace.Success("Editor entity list refreshed after ReloadLevel", "UIEntities=" + vm.Entities.Count);
                }
            }
            catch (Exception ex)
            {
                MapDiagnosticsTrace.Error("SceneView.LoadLevel failed", ex.ToString());
                throw;
            }
            finally
            {
                sw.Stop();
                MapDiagnosticsTrace.Performance("SceneView.LoadLevel completed", "Elapsed=" + sw.Elapsed.TotalMilliseconds.ToString("F1") + " ms");
            }
        }

        private void AutoFrameLoadedLevel()
        {
            var level = _bootstrap?.Level;
            var camera = _bootstrap?.Camera;

            if (level == null)
            {
                MapDiagnosticsTrace.Warning("AutoFrame skipped", "RenderBootstrap.Level is NULL");
                return;
            }

            if (camera == null)
            {
                MapDiagnosticsTrace.Warning("AutoFrame skipped", "Camera is NULL");
                return;
            }

            if (level.AllEntities.Count == 0)
            {
                MapDiagnosticsTrace.Warning("AutoFrame skipped", "World contains 0 entities");
                return;
            }

            MapDiagnosticsTrace.Renderer("AutoFrame scanning world bounds", "WorldEntities=" + level.AllEntities.Count);

            var min = new XnaVector3(float.MaxValue);
            var max = new XnaVector3(float.MinValue);
            var found = false;
            var skipped = 0;

            foreach (var entity in level.AllEntities)
            {
                if (entity == null)
                {
                    skipped++;
                    continue;
                }

                var size = entity.Size;
                if (size == XnaVector3.Zero)
                    size = XnaVector3.One;

                var scale = entity.Scale;
                if (scale == XnaVector3.Zero)
                    scale = XnaVector3.One;

                var scaledSize = size * scale;
                var half = new XnaVector3(
                    Math.Abs(scaledSize.X),
                    Math.Abs(scaledSize.Y),
                    Math.Abs(scaledSize.Z)) * 0.5f;
                var position = entity.Position;

                min = XnaVector3.Min(min, position - half);
                max = XnaVector3.Max(max, position + half);
                found = true;
            }

            if (!found)
            {
                MapDiagnosticsTrace.Warning("AutoFrame failed to find valid entity bounds", "Skipped=" + skipped);
                return;
            }

            var center = (min + max) * 0.5f;
            var extents = max - min;
            var radius = extents.Length() * 0.5f;
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius < 8f)
                radius = 8f;

            var distance = radius * 2.25f;
            camera.Position = center + new XnaVector3(distance, distance * 0.75f, distance);
            camera.Target = center;

            MapDiagnosticsTrace.Success(
                "Camera auto-framed loaded map",
                "Min=" + min + " Max=" + max + " Center=" + center + " Radius=" + radius.ToString("F2") +
                " Camera=" + camera.Position + " Target=" + camera.Target + " Skipped=" + skipped);
        }

        public void RefreshFromLevel(LevelInfo? levelInfo)
        {
            if (levelInfo == null)
            {
                MapDiagnosticsTrace.Warning("SceneView.RefreshFromLevel ignored null LevelInfo");
                return;
            }

            MapDiagnosticsTrace.Renderer("SceneView.RefreshFromLevel", "Entities=" + (levelInfo.Entities?.Count ?? 0));
            LoadLevel(levelInfo);
        }

        public void SetAntiAliasingFromMenu(AntiAliasing mode) => _bootstrap?.SetAntiAliasing(mode);
        private void SyncAaComboFromSettings() { }

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
            if (_bootstrap == null)
            {
                MapDiagnosticsTrace.Warning("Manual ReloadLevel ignored", "Bootstrap is NULL");
                return;
            }

            try
            {
                MapDiagnosticsTrace.Renderer("Manual ReloadLevel started", "CurrentWorldEntities=" + _bootstrap.GetAllEntities().Count);
                _bootstrap.ReloadLevel();
                AutoFrameLoadedLevel();
                MapDiagnosticsTrace.Success("Manual ReloadLevel completed", "WorldEntities=" + _bootstrap.GetAllEntities().Count);

                if (DataContext is SceneViewModel vm)
                    vm.AttachView(this);
            }
            catch (Exception ex)
            {
                MapDiagnosticsTrace.Error("Manual ReloadLevel failed", ex.ToString());
            }
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

        private void ViewHost_OnMouseWheel(object sender, MouseWheelEventArgs e) => _bootstrap?.CameraController?.OnMouseWheel(e.Delta);
        private void ViewHost_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isRightMouseDown = true;
            if (GameHost == null) return;
            _lastMousePos = e.GetPosition(GameHost);
            Mouse.Capture(GameHost);
            if (!GameHost.IsKeyboardFocusWithin) GameHost.Focus();
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
            if (_bootstrap != null) _bootstrap.SetLayerVisible(SceneLayer.Collision, (sender as CheckBox)?.IsChecked == true);
        }
        private void OnTogglePropsLayer(object sender, RoutedEventArgs e)
        {
            if (_bootstrap != null) _bootstrap.SetLayerVisible(SceneLayer.Props, (sender as CheckBox)?.IsChecked == true);
        }
        private void OnToggleLightsLayer(object sender, RoutedEventArgs e)
        {
            if (_bootstrap != null) _bootstrap.SetLayerVisible(SceneLayer.Lights, (sender as CheckBox)?.IsChecked == true);
        }
        private void OnToggleTriggersLayer(object sender, RoutedEventArgs e)
        {
            if (_bootstrap != null) _bootstrap.SetLayerVisible(SceneLayer.Triggers, (sender as CheckBox)?.IsChecked == true);
        }
    }
}
