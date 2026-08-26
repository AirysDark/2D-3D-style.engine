using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using GE2D3D.MapEditor.Modules.SceneViewer.ViewModels;
using GE2D3D.MapEditor.Utils;

namespace GE2D3D.MapEditor
{
    public partial class MapDiagnosticsWindow : Window
    {
        private readonly SceneViewModel _viewModel;
        private MapDiagnosticsReport _lastReport;
        private string _overviewText = string.Empty;
        private string _traceText = string.Empty;
        private string _rawText = string.Empty;
        private string _comparisonText = string.Empty;
        private string _assetsText = string.Empty;
        private string _warningsText = string.Empty;

        public MapDiagnosticsWindow(SceneViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            MapDiagnosticsTrace.TraceChanged += OnTraceChanged;
            Closed += OnWindowClosed;
            RefreshDiagnostics();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            MapDiagnosticsTrace.TraceChanged -= OnTraceChanged;
        }

        private void OnTraceChanged(object sender, EventArgs e)
        {
            if (Dispatcher == null) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (AutoRefreshCheckBox.IsChecked == true)
                    RefreshTraceOnly();
            }));
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshDiagnostics();

        private void OnClearTraceClick(object sender, RoutedEventArgs e)
        {
            MapDiagnosticsTrace.Clear();
            RefreshTraceOnly();
            StatusTextBlock.Text = "Live trace cleared.";
        }

        private void OnCopyAllClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(BuildCombinedReport());
                StatusTextBlock.Text = "Full diagnostic report copied to clipboard.";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Copy failed: " + ex.Message;
            }
        }

        private void OnSaveReportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Save Map Diagnostics Report",
                    FileName = string.IsNullOrWhiteSpace(_viewModel.FileName) ? "MapDiagnostics.txt" : Path.GetFileNameWithoutExtension(_viewModel.FileName) + "_Diagnostics.txt",
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    DefaultExt = ".txt",
                    AddExtension = true
                };
                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, BuildCombinedReport());
                    StatusTextBlock.Text = "Report saved: " + dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Save failed: " + ex.Message;
            }
        }

        private void RefreshDiagnostics()
        {
            var mapPath = _viewModel.FilePath;
            _lastReport = !string.IsNullOrWhiteSpace(mapPath) && File.Exists(mapPath)
                ? MapDiagnosticsAnalyzer.Analyze(mapPath)
                : new MapDiagnosticsReport();

            _overviewText = BuildOverview(mapPath, _lastReport);
            _rawText = BuildRawMapReport(_lastReport);
            _comparisonText = BuildComparisonReport(_lastReport);
            _assetsText = BuildAssetsReport(mapPath, _lastReport);
            _warningsText = BuildWarningsReport(_lastReport);

            OverviewTextBox.Text = _overviewText;
            RawMapTextBox.Text = _rawText;
            ComparisonTextBox.Text = _comparisonText;
            AssetsTextBox.Text = _assetsText;
            WarningsTextBox.Text = _warningsText;
            RefreshTraceOnly();

            StatusTextBlock.Text = "Analysis refreshed at " + DateTime.Now.ToString("HH:mm:ss.fff");
        }

        private void RefreshTraceOnly()
        {
            _traceText = MapDiagnosticsTrace.BuildTextReport();
            TraceTextBox.Text = _traceText;
            TraceTextBox.ScrollToEnd();
        }

        private string BuildOverview(string mapPath, MapDiagnosticsReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("GE2D3D MAP DIAGNOSTICS - OVERVIEW");
            sb.AppendLine(new string('=', 88));
            sb.AppendLine("Generated:       " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            sb.AppendLine("Map file:        " + (string.IsNullOrWhiteSpace(mapPath) ? "<none loaded>" : mapPath));
            sb.AppendLine("Map exists:      " + (!string.IsNullOrWhiteSpace(mapPath) && File.Exists(mapPath) ? "YES" : "NO"));
            sb.AppendLine("Map name:        " + (_viewModel.FileName ?? "<none>"));
            sb.AppendLine("Display name:    " + (_viewModel.DisplayName ?? "<none>"));
            sb.AppendLine("File size:       " + report.FileSizeBytes + " bytes");
            sb.AppendLine("Text length:     " + report.TextLength + " characters");
            sb.AppendLine("Lines:           " + report.LineCount);
            sb.AppendLine("Quoted tags:     " + report.TagCount);
            sb.AppendLine("Malformed tags:  " + report.MalformedTagCount);
            sb.AppendLine("Editor status:   " + (_viewModel.StatusMessage ?? "<none>"));
            sb.AppendLine();
            sb.AppendLine("EDITOR STATE");
            sb.AppendLine(new string('-', 88));
            sb.AppendLine("Parsed entities in LevelInfo: " + (_viewModel.LevelInfo == null ? 0 : _viewModel.LevelInfo.Entities.Count));
            sb.AppendLine("Parsed structures:            " + (_viewModel.LevelInfo == null ? 0 : _viewModel.LevelInfo.Structures.Count));
            sb.AppendLine("Parsed offset maps:           " + (_viewModel.LevelInfo == null ? 0 : _viewModel.LevelInfo.OffsetMaps.Count));
            sb.AppendLine("Entities currently in UI:      " + _viewModel.Entities.Count);
            sb.AppendLine();
            sb.AppendLine("TRACE SESSION");
            sb.AppendLine(new string('-', 88));
            sb.AppendLine("Trace map:       " + (string.IsNullOrWhiteSpace(MapDiagnosticsTrace.CurrentMapPath) ? "<none>" : MapDiagnosticsTrace.CurrentMapPath));
            sb.AppendLine("Trace elapsed:   " + MapDiagnosticsTrace.Elapsed.TotalMilliseconds.ToString("F1") + " ms");
            sb.AppendLine("Trace entries:   " + MapDiagnosticsTrace.GetSnapshot().Count);
            return sb.ToString();
        }

        private static string BuildRawMapReport(MapDiagnosticsReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("RAW MAP INVENTORY");
            sb.AppendLine(new string('=', 88));
            sb.AppendLine("RECORD TYPES");
            sb.AppendLine(new string('-', 88));
            if (report.RecordCounts.Count == 0) sb.AppendLine("<none detected>");
            foreach (var item in report.RecordCounts.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)) sb.AppendLine(item.Key + ": " + item.Value);

            sb.AppendLine();
            sb.AppendLine("ALL QUOTED FIELDS / TAGS");
            sb.AppendLine(new string('-', 88));
            if (report.FieldCounts.Count == 0) sb.AppendLine("<none detected>");
            foreach (var item in report.FieldCounts.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)) sb.AppendLine(item.Key + ": " + item.Value);

            sb.AppendLine();
            sb.AppendLine("ENTITY IDs");
            sb.AppendLine(new string('-', 88));
            if (report.EntityIdCounts.Count == 0) sb.AppendLine("<none detected>");
            foreach (var item in report.EntityIdCounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)) sb.AppendLine(item.Key + ": " + item.Value);
            return sb.ToString();
        }

        private string BuildComparisonReport(MapDiagnosticsReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("RAW vs PARSED vs EDITOR");
            sb.AppendLine(new string('=', 88));
            sb.AppendLine("TYPE".PadRight(18) + "RAW".PadRight(10) + "PARSED".PadRight(10) + "EDITOR".PadRight(10) + "STATUS");
            sb.AppendLine(new string('-', 88));
            AddComparisonRow(sb, report, "Level", _viewModel.LevelInfo == null ? 0 : 1, 0);
            AddComparisonRow(sb, report, "Actions", _viewModel.LevelInfo == null ? 0 : 1, 0);
            AddComparisonRow(sb, report, "Entity", _viewModel.LevelInfo == null ? 0 : _viewModel.LevelInfo.Entities.Count, _viewModel.Entities.Count);
            AddComparisonRow(sb, report, "Floor", _viewModel.LevelInfo == null ? 0 : _viewModel.LevelInfo.Entities.Count, _viewModel.Entities.Count);
            AddComparisonRow(sb, report, "EntityField", _viewModel.LevelInfo == null ? 0 : _viewModel.LevelInfo.Entities.Count, _viewModel.Entities.Count);
            AddComparisonRow(sb, report, "Structure", _viewModel.LevelInfo == null ? 0 : _viewModel.LevelInfo.Structures.Count, 0);
            AddComparisonRow(sb, report, "OffsetMap", _viewModel.LevelInfo == null ? 0 : _viewModel.LevelInfo.OffsetMaps.Count, 0);
            AddComparisonRow(sb, report, "NPC", 0, 0);
            AddComparisonRow(sb, report, "Backdrop", _viewModel.LevelInfo == null ? 0 : 1, 0);
            AddComparisonRow(sb, report, "Shader", _viewModel.LevelInfo == null ? 0 : 1, 0);
            sb.AppendLine();
            sb.AppendLine("NOTE: Entity/Floor/EntityField currently feed the same parsed entity collection, so their individual parsed totals cannot be separated without adding per-record provenance to LevelInfo.");
            sb.AppendLine("NPC is intentionally shown as not parsed because LevelLoader currently detects NPC records but leaves NPC loading disabled.");
            return sb.ToString();
        }

        private static void AddComparisonRow(StringBuilder sb, MapDiagnosticsReport report, string name, int parsed, int editor)
        {
            int raw;
            report.RecordCounts.TryGetValue(name, out raw);
            string status;
            if (raw == 0) status = "NOT IN RAW MAP";
            else if (name.Equals("NPC", StringComparison.OrdinalIgnoreCase)) status = "NOT IMPLEMENTED";
            else if (parsed == 0) status = "RAW FOUND / NOT PARSED";
            else status = "PARSED";
            sb.AppendLine(name.PadRight(18) + raw.ToString().PadRight(10) + parsed.ToString().PadRight(10) + editor.ToString().PadRight(10) + status);
        }

        private static string BuildAssetsReport(string mapPath, MapDiagnosticsReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ASSET AND REFERENCE DIAGNOSTICS");
            sb.AppendLine(new string('=', 88));
            var contentRoot = FindContentRoot(mapPath);
            sb.AppendLine("Content root: " + (contentRoot ?? "NOT FOUND"));
            sb.AppendLine();
            sb.AppendLine("TEXTURE SHEETS");
            sb.AppendLine(new string('-', 88));
            if (report.TexturePaths.Count == 0) sb.AppendLine("<none referenced>");
            foreach (var texture in report.TexturePaths.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var resolved = ResolveTexture(contentRoot, texture);
                sb.AppendLine("[" + (resolved == null ? "MISSING/UNKNOWN" : "FOUND") + "] " + texture + (resolved == null ? string.Empty : " -> " + resolved));
            }

            sb.AppendLine();
            sb.AppendLine("MODEL REFERENCES");
            sb.AppendLine(new string('-', 88));
            if (report.ModelPaths.Count == 0) sb.AppendLine("<none referenced>");
            foreach (var model in report.ModelPaths.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) sb.AppendLine(model);

            sb.AppendLine();
            sb.AppendLine("MAP / STRUCTURE REFERENCES");
            sb.AppendLine(new string('-', 88));
            if (report.ReferencedMaps.Count == 0) sb.AppendLine("<none referenced>");
            foreach (var value in report.ReferencedMaps.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) sb.AppendLine(value);
            return sb.ToString();
        }

        private static string BuildWarningsReport(MapDiagnosticsReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("WARNINGS, ERRORS AND UNSUPPORTED CONTENT");
            sb.AppendLine(new string('=', 88));
            foreach (var warning in report.Warnings) sb.AppendLine("WARNING: " + warning);
            foreach (var unknown in report.UnknownRecordTypes) sb.AppendLine("UNSUPPORTED RECORD: " + unknown);

            var trace = MapDiagnosticsTrace.GetSnapshot();
            foreach (var entry in trace.Where(x => x.Level == MapDiagnosticLevel.Warning || x.Level == MapDiagnosticLevel.Error)) sb.AppendLine(entry.ToString());
            if (report.Warnings.Count == 0 && report.UnknownRecordTypes.Count == 0 && !trace.Any(x => x.Level == MapDiagnosticLevel.Warning || x.Level == MapDiagnosticLevel.Error)) sb.AppendLine("No warnings or errors currently recorded.");
            return sb.ToString();
        }

        private string BuildCombinedReport()
        {
            return _overviewText + Environment.NewLine + _traceText + Environment.NewLine + _rawText + Environment.NewLine + _comparisonText + Environment.NewLine + _assetsText + Environment.NewLine + _warningsText;
        }

        private static string FindContentRoot(string mapPath)
        {
            if (string.IsNullOrWhiteSpace(mapPath)) return null;
            var directory = new DirectoryInfo(Path.GetDirectoryName(mapPath) ?? string.Empty);
            while (directory != null)
            {
                if (string.Equals(directory.Name, "Content", StringComparison.OrdinalIgnoreCase)) return directory.FullName;
                directory = directory.Parent;
            }
            return null;
        }

        private static string ResolveTexture(string contentRoot, string texture)
        {
            if (string.IsNullOrWhiteSpace(contentRoot) || string.IsNullOrWhiteSpace(texture)) return null;
            string[] extensions = { ".png", ".jpg", ".jpeg", ".bmp", ".dds", ".xnb" };
            string[] roots = { Path.Combine(contentRoot, "Textures"), contentRoot };
            foreach (var root in roots)
            {
                foreach (var extension in extensions)
                {
                    var candidate = Path.Combine(root, texture + extension);
                    if (File.Exists(candidate)) return candidate;
                }
            }
            return null;
        }
    }
}
