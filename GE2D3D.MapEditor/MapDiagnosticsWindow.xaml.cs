using System;
using System.IO;
using System.Text;
using System.Windows;
using GE2D3D.MapEditor.Modules.SceneViewer.ViewModels;

namespace GE2D3D.MapEditor
{
    public partial class MapDiagnosticsWindow : Window
    {
        private readonly SceneViewModel _viewModel;

        public MapDiagnosticsWindow(SceneViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            RefreshDiagnostics();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            RefreshDiagnostics();
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            DiagnosticsTextBox.Clear();
        }

        private void RefreshDiagnostics()
        {
            var sb = new StringBuilder();
            sb.AppendLine("GE2D3D MAP DIAGNOSTICS");
            sb.AppendLine(new string('=', 72));
            sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();

            var mapPath = _viewModel.FilePath;
            sb.AppendLine("Map file: " + (string.IsNullOrWhiteSpace(mapPath) ? "<none loaded>" : mapPath));
            sb.AppendLine("Map exists: " + (!string.IsNullOrWhiteSpace(mapPath) && File.Exists(mapPath) ? "YES" : "NO"));
            sb.AppendLine("Map name: " + (_viewModel.FileName ?? "<none>"));
            sb.AppendLine("Display name: " + (_viewModel.DisplayName ?? "<none>"));
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(mapPath))
            {
                var mapDirectory = Path.GetDirectoryName(mapPath);
                sb.AppendLine("Map directory: " + (mapDirectory ?? "<unknown>"));

                var contentRoot = FindContentRoot(mapPath);
                sb.AppendLine("Content root: " + (contentRoot ?? "NOT FOUND"));

                if (contentRoot != null)
                {
                    var mapsRoot = Path.Combine(contentRoot, "Data", "maps");
                    var texturesRoot = Path.Combine(contentRoot, "Textures");
                    sb.AppendLine("Maps root: " + mapsRoot + " [" + (Directory.Exists(mapsRoot) ? "FOUND" : "MISSING") + "]");
                    sb.AppendLine("Textures root: " + texturesRoot + " [" + (Directory.Exists(texturesRoot) ? "FOUND" : "MISSING") + "]");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Editor status: " + (_viewModel.StatusMessage ?? "<no status message>"));
            sb.AppendLine("Camera status: " + (_viewModel.StatusCameraText ?? "<none>"));
            sb.AppendLine("Selection status: " + (_viewModel.StatusSelectionText ?? "<none>"));
            sb.AppendLine("Entities visible to editor: " + _viewModel.Entities.Count);
            sb.AppendLine();
            sb.AppendLine("Note: Refresh this window after opening a map to see the current resolved paths and editor state.");

            DiagnosticsTextBox.Text = sb.ToString();
            DiagnosticsTextBox.ScrollToHome();
        }

        private static string? FindContentRoot(string mapPath)
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(mapPath) ?? string.Empty);
            while (directory != null)
            {
                if (string.Equals(directory.Name, "Content", StringComparison.OrdinalIgnoreCase))
                    return directory.FullName;

                directory = directory.Parent;
            }

            return null;
        }
    }
}
