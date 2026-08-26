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
        private MapDiagnosticsWindow? _mapDiagnosticsWindow;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new SceneViewModel();
            DataContext = ViewModel;
            Loaded += OnMainWindowLoaded;
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.AttachView(SceneViewControl);
        }

        private void OnExitClick(object sender, RoutedEventArgs e) => Close();

        private void OnMapDiagnosticsMenuClick(object sender, RoutedEventArgs e)
        {
            if (_mapDiagnosticsWindow == null || !_mapDiagnosticsWindow.IsLoaded)
            {
                _mapDiagnosticsWindow = new MapDiagnosticsWindow(ViewModel) { Owner = this };
                _mapDiagnosticsWindow.Show();
            }
            else
            {
                _mapDiagnosticsWindow.Activate();
            }
        }

        private void OnCameraPathMenuClick(object sender, RoutedEventArgs e)
        {
            if (_cameraPathWindow == null || !_cameraPathWindow.IsLoaded)
            {
                _cameraPathWindow = new CameraPathWindow { Owner = this, DataContext = DataContext };
                _cameraPathWindow.Show();
            }
            else _cameraPathWindow.Activate();
        }

        private void OnSelectionMenuClick(object sender, RoutedEventArgs e)
        {
            if (_selectionWindow == null || !_selectionWindow.IsLoaded)
            {
                _selectionWindow = new SelectionWindow { Owner = this, DataContext = DataContext };
                _selectionWindow.Show();
            }
            else _selectionWindow.Activate();
        }

        private void OnEntitiesMenuClick(object sender, RoutedEventArgs e)
        {
            if (_entitiesWindow == null || !_entitiesWindow.IsLoaded)
            {
                _entitiesWindow = new EntitiesWindow { Owner = this, DataContext = DataContext };
                _entitiesWindow.Show();
            }
            else _entitiesWindow.Activate();
        }

        private void OnAssetsMenuClick(object sender, RoutedEventArgs e)
        {
            if (_assetsWindow == null || !_assetsWindow.IsLoaded)
            {
                _assetsWindow = new AssetsWindow { Owner = this, DataContext = DataContext };
                _assetsWindow.Show();
            }
            else _assetsWindow.Activate();
        }

        private void OnAAModeMenuClick(object sender, RoutedEventArgs e)
        {
            if (SceneViewControl == null || sender is not MenuItem clicked || clicked.Tag is not string tag ||
                !Enum.TryParse<AntiAliasing>(tag, true, out var mode)) return;

            SceneViewControl.SetAntiAliasingFromMenu(mode);
            if (clicked.Parent is ItemsControl parent)
                foreach (var item in parent.Items)
                    if (item is MenuItem mi) mi.IsChecked = ReferenceEquals(mi, clicked);
        }

        private static string GetLastMapPathFile() => Path.Combine(EditorPaths.GetProjectRoot(), "last_map.txt");

        private static void SaveLastMapPath(string path)
        {
            try
            {
                var file = GetLastMapPathFile();
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, path ?? string.Empty);
                Debug.WriteLine($"[Editor] Saved last map path: {path}");
            }
            catch (Exception ex) { Debug.WriteLine($"[Editor] Failed to save last_map.txt: {ex.Message}"); }
        }

        private static void LaunchRenderTest()
        {
            try
            {
                var exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RenderTest.exe");
                if (!File.Exists(exe))
                {
                    MessageBox.Show("RenderTest.exe not found next to the editor executable.", "Render Test", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Process.Start(new ProcessStartInfo { FileName = exe, UseShellExecute = true });
            }
            catch (Exception ex) { MessageBox.Show($"Failed to launch RenderTest.exe:\n{ex.Message}", "Render Test", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void OnRenderTestRunLastMapClick(object sender, RoutedEventArgs e)
        {
            var lastFile = GetLastMapPathFile();
            if (!File.Exists(lastFile))
            {
                MessageBox.Show("No last map found. Open a map in the editor first, or use 'Open Map and Run...'.", "Render Test", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var mapPath = File.ReadAllText(lastFile).Trim();
            if (string.IsNullOrWhiteSpace(mapPath) || !File.Exists(mapPath))
            {
                MessageBox.Show("The last map path is invalid. Use 'Open Map and Run...' to pick a new map.", "Render Test", MessageBoxButton.OK, MessageBoxImage.Warning);
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
