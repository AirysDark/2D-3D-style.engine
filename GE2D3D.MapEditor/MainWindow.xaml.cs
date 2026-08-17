using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GE2D3D.MapEditor.Components.Render;
using GE2D3D.MapEditor.Modules.SceneViewer.ViewModels;
using GE2D3D.MapEditor.Modules.SceneViewer.Views;
using GE2D3D.MapEditor.Utils;

namespace GE2D3D.MapEditor
{
    public partial class MainWindow : Window
    {
        public SceneViewModel ViewModel { get; }

        private SelectionWindow? _selectionWindow;
        private EntitiesWindow? _entitiesWindow;
        private CameraPathWindow? _cameraPathWindow;
        private AssetsWindow? _assetsWindow;

        public MainWindow()
        {
            InitializeComponent();

            // Create VM once
            ViewModel = new SceneViewModel();

            // Set DataContext AFTER InitializeComponent so it overrides the XAML one at runtime
            DataContext = ViewModel;

            // Hook the MonoGame view to the VM once the window is loaded
            this.Loaded += OnMainWindowLoaded;
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Now SceneViewControl is guaranteed to be constructed
            ViewModel.AttachView(SceneViewControl);
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnCameraPathMenuClick(object sender, RoutedEventArgs e)
        {
            if (_cameraPathWindow == null || !_cameraPathWindow.IsLoaded)
            {
                _cameraPathWindow = new CameraPathWindow
                {
                    Owner = this,
                    DataContext = this.DataContext
                };
                _cameraPathWindow.Show();
            }
            else
            {
                _cameraPathWindow.Activate();
            }
        }

        private void OnSelectionMenuClick(object sender, RoutedEventArgs e)
        {
            if (_selectionWindow == null || !_selectionWindow.IsLoaded)
            {
                _selectionWindow = new SelectionWindow
                {
                    Owner = this,
                    DataContext = this.DataContext
                };
                _selectionWindow.Show();
            }
            else
            {
                _selectionWindow.Activate();
            }
        }

        private void OnEntitiesMenuClick(object sender, RoutedEventArgs e)
        {
            if (_entitiesWindow == null || !_entitiesWindow.IsLoaded)
            {
                _entitiesWindow = new EntitiesWindow
                {
                    Owner = this,
                    DataContext = this.DataContext
                };
                _entitiesWindow.Show();
            }
            else
            {
                _entitiesWindow.Activate();
            }
        }

        private void OnAssetsMenuClick(object sender, RoutedEventArgs e)
        {
            if (_assetsWindow == null || !_assetsWindow.IsLoaded)
            {
                _assetsWindow = new AssetsWindow
                {
                    Owner = this,
                    DataContext = this.DataContext
                };
                _assetsWindow.Show();
            }
            else
            {
                _assetsWindow.Activate();
            }
        }

        private void OnAAModeMenuClick(object sender, RoutedEventArgs e)
        {
            if (SceneViewControl == null)
                return;

            if (sender is not MenuItem clicked)
                return;

            var tag = clicked.Tag as string;
            if (string.IsNullOrWhiteSpace(tag))
                return;

            if (!Enum.TryParse<AntiAliasing>(tag, ignoreCase: true, out var mode))
                return;

            // 1) Apply AA mode to the renderer via SceneView
            SceneViewControl.SetAntiAliasingFromMenu(mode);

            // 2) Ensure only this menu item is checked
            if (clicked.Parent is ItemsControl parent)
            {
                foreach (var item in parent.Items)
                {
                    if (item is MenuItem mi)
                    {
                        mi.IsChecked = ReferenceEquals(mi, clicked);
                    }
                }
            }
        }

        // ----------------------------------------------------------
        // RenderTest helpers + handlers
        // ----------------------------------------------------------

        private static string GetLastMapPathFile()
        {
            var root = EditorPaths.GetProjectRoot();
            return Path.Combine(root, "last_map.txt");
        }

        private static void SaveLastMapPath(string path)
        {
            try
            {
                var file = GetLastMapPathFile();
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, path ?? string.Empty);
                Debug.WriteLine($"[Editor] Saved last map path: {path}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Editor] Failed to save last_map.txt: {ex.Message}");
            }
        }

        private static void LaunchRenderTest()
        {
            try
            {
                var exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RenderTest.exe");
                if (!File.Exists(exe))
                {
                    MessageBox.Show(
                        "RenderTest.exe not found next to the editor executable.",
                        "Render Test",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to launch RenderTest.exe:\n{ex.Message}",
                    "Render Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnRenderTestRunLastMapClick(object sender, RoutedEventArgs e)
        {
            var lastFile = GetLastMapPathFile();
            if (!File.Exists(lastFile))
            {
                MessageBox.Show(
                    "No last map found. Open a map in the editor first, or use 'Open Map and Run...'.",
                    "Render Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var mapPath = File.ReadAllText(lastFile).Trim();
            if (string.IsNullOrWhiteSpace(mapPath) || !File.Exists(mapPath))
            {
                MessageBox.Show(
                    "The last map path is invalid. Use 'Open Map and Run...' to pick a new map.",
                    "Render Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            LaunchRenderTest();
        }

        private void OnRenderTestOpenAndRunClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                InitialDirectory = EditorPaths.GetMapsFolder(),
                Filter = "Map files (*.dat)|*.dat|All files (*.*)|*.*",
                Title = "Select map to test in RenderTest"
            };

            if (dlg.ShowDialog() == true)
            {
                SaveLastMapPath(dlg.FileName);
                LaunchRenderTest();
            }
        }
    }
}
