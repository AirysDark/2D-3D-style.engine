using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using GE2D3D.MapEditor.Modules.SceneViewer.ViewModels;
using GE2D3D.MapEditor.Utils;

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

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshDiagnostics();
        private void OnClearClick(object sender, RoutedEventArgs e) => DiagnosticsTextBox.Clear();

        private void RefreshDiagnostics()
        {
            var sb = new StringBuilder();
            sb.AppendLine("GE2D3D MAP DIAGNOSTICS / RAW MAP INSPECTOR");
            sb.AppendLine(new string('=', 72));
            sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();

            var mapPath = _viewModel.FilePath;
            sb.AppendLine("Map file: " + (string.IsNullOrWhiteSpace(mapPath) ? "<none loaded>" : mapPath));
            sb.AppendLine("Map exists: " + (!string.IsNullOrWhiteSpace(mapPath) && File.Exists(mapPath) ? "YES" : "NO"));
            sb.AppendLine("Map name: " + (_viewModel.FileName ?? "<none>"));
            sb.AppendLine("Display name: " + (_viewModel.DisplayName ?? "<none>"));
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(mapPath) && File.Exists(mapPath))
            {
                var contentRoot = FindContentRoot(mapPath);
                sb.AppendLine("Content root: " + (contentRoot ?? "NOT FOUND"));
                if (contentRoot != null)
                {
                    sb.AppendLine("Maps root: " + Path.Combine(contentRoot, "Data", "maps"));
                    sb.AppendLine("Textures root: " + Path.Combine(contentRoot, "Textures"));
                }

                var report = MapDiagnosticsAnalyzer.Analyze(mapPath);
                sb.AppendLine();
                sb.AppendLine("RECORD TYPES FOUND");
                sb.AppendLine(new string('-', 72));
                foreach (var item in report.RecordCounts.OrderBy(x => x.Key))
                    sb.AppendLine(item.Key + ": " + item.Value);

                sb.AppendLine();
                sb.AppendLine("FIELDS FOUND (dynamic - not hard-coded)");
                sb.AppendLine(new string('-', 72));
                foreach (var item in report.FieldCounts.OrderBy(x => x.Key))
                    sb.AppendLine(item.Key + ": " + item.Value);

                sb.AppendLine();
                sb.AppendLine("ENTITY IDs FOUND");
                sb.AppendLine(new string('-', 72));
                foreach (var item in report.EntityIdCounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
                    sb.AppendLine(item.Key + ": " + item.Value);

                sb.AppendLine();
                sb.AppendLine("TEXTURE SHEETS REFERENCED");
                sb.AppendLine(new string('-', 72));
                foreach (var texture in report.TexturePaths.OrderBy(x => x))
                {
                    var found = contentRoot != null && File.Exists(Path.Combine(contentRoot, "Textures", texture + ".png"));
                    sb.AppendLine(texture + " [" + (found ? "FOUND" : "MISSING/UNKNOWN") + "]");
                }

                if (report.ModelPaths.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("MODEL REFERENCES FOUND");
                    sb.AppendLine(new string('-', 72));
                    foreach (var model in report.ModelPaths.OrderBy(x => x)) sb.AppendLine(model);
                }

                if (report.ReferencedMaps.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("REFERENCED MAPS / STRUCTURES");
                    sb.AppendLine(new string('-', 72));
                    foreach (var referencedMap in report.ReferencedMaps.Distinct(StringComparer.OrdinalIgnoreCase)) sb.AppendLine(referencedMap);
                }

                if (report.UnknownRecordTypes.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("UNKNOWN RECORD TYPES - NOT SILENTLY IGNORED");
                    sb.AppendLine(new string('-', 72));
                    foreach (var type in report.UnknownRecordTypes) sb.AppendLine(type);
                }
            }

            sb.AppendLine();
            sb.AppendLine("LOADED EDITOR STATE");
            sb.AppendLine(new string('-', 72));
            sb.AppendLine("Editor status: " + (_viewModel.StatusMessage ?? "<none>"));
            sb.AppendLine("Entities currently loaded into editor: " + _viewModel.Entities.Count);
            sb.AppendLine("Note: diagnostics inspects the raw file and reports every record/field it can see, including records the current renderer does not yet render.");

            DiagnosticsTextBox.Text = sb.ToString();
            DiagnosticsTextBox.ScrollToHome();
        }

        private static string? FindContentRoot(string mapPath)
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(mapPath) ?? string.Empty);
            while (directory != null)
            {
                if (string.Equals(directory.Name, "Content", StringComparison.OrdinalIgnoreCase)) return directory.FullName;
                directory = directory.Parent;
            }
            return null;
        }
    }
}
