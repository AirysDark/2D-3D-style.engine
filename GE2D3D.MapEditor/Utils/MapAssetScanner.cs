using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GE2D3D.MapEditor.Utils
{
    public sealed class MapAssetScannerReport
    {
        public string ProjectRoot { get; set; } = string.Empty;
        public string MapsRoot { get; set; } = string.Empty;

        public List<string> MapsScanned { get; } = new();
        public List<string> MissingMapFiles { get; } = new();

        public List<string> TextureFilesUsed { get; } = new();
        public List<string> MissingTextureFiles { get; } = new();

        public List<string> NpcTexturesUsed { get; } = new();
        public List<string> MissingNpcTextures { get; } = new();

        public List<string> MusicFilesUsed { get; } = new();
        public List<string> MissingMusicFiles { get; } = new();

        public List<string> ScriptPathsUsed { get; } = new();
        public List<string> MissingScriptPaths { get; } = new();

        // Path where SaveReport wrote the JSON
        public string ReportPath { get; set; } = string.Empty;
    }

    public static class MapAssetScanner
    {
        // Regex helpers for P3D-style .dat tags
        private static readonly Regex TexturePathRegex =
            new Regex("TexturePath\"\\{str\\[(?<id>[^\\]]+)\\]", RegexOptions.Compiled);

        private static readonly Regex TextureIdRegex =
            new Regex("TextureID\"\\{str\\[(?<id>[^\\]]+)\\]", RegexOptions.Compiled);

        private static readonly Regex MusicLoopRegex =
            new Regex("MusicLoop\"\\{str\\[(?<id>[^\\]]+)\\]", RegexOptions.Compiled);

        private static readonly Regex AdditionalValueRegex =
            new Regex("AdditionalValue\"\\{str\\[(?<val>[^\\]]*)\\]", RegexOptions.Compiled);

        /// <summary>
        /// Root folder where JSON reports are written:
        /// ProjectRoot/Content/EditorReports/
        /// </summary>
        private static string GetReportRoot()
        {
            var contentRoot = EditorPaths.ContentRoot;
            var root = Path.Combine(contentRoot, "EditorReports");

            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            return root;
        }

        /// <summary>
        /// Scans all .dat maps under EditorPaths.GetMapsFolder()
        /// and returns a report with used + missing assets.
        /// </summary>
        public static MapAssetScannerReport ScanAllMaps()
        {
            var report = new MapAssetScannerReport();

            var projectRoot = EditorPaths.GetProjectRoot();
            var mapsRoot = EditorPaths.GetMapsFolder();
            var contentRoot = EditorPaths.ContentRoot;
            var texturesRoot = EditorPaths.GetTexturesFolder();
            var npcRoot = Path.Combine(texturesRoot, "NPC");
            var songsRoot = EditorPaths.GetSongsFolder();
            var scriptsRoot = Path.Combine(contentRoot, "Data", "scripts");

            report.ProjectRoot = projectRoot;
            report.MapsRoot = mapsRoot;

            if (!Directory.Exists(mapsRoot))
            {
                Debug.WriteLine($"[MapAssetScanner] Maps root does not exist: {mapsRoot}");
                return report;
            }

            var datFiles = Directory.GetFiles(mapsRoot, "*.dat", SearchOption.AllDirectories);

            foreach (var mapPath in datFiles)
            {
                ProcessMapFile(report, mapPath, projectRoot, mapsRoot, texturesRoot, npcRoot, songsRoot, scriptsRoot);
            }

            return report;
        }

        /// <summary>
        /// Scan a single map file instead of all maps.
        /// Useful for "scan on map load".
        /// </summary>
        public static MapAssetScannerReport ScanSingleMap(string mapPath)
        {
            if (string.IsNullOrWhiteSpace(mapPath) || !File.Exists(mapPath))
                throw new FileNotFoundException("Map file not found for asset scan.", mapPath);

            var report = new MapAssetScannerReport();

            var projectRoot = EditorPaths.GetProjectRoot();
            var mapsRoot = EditorPaths.GetMapsFolder();
            var contentRoot = EditorPaths.ContentRoot;
            var texturesRoot = EditorPaths.GetTexturesFolder();
            var npcRoot = Path.Combine(texturesRoot, "NPC");
            var songsRoot = EditorPaths.GetSongsFolder();
            var scriptsRoot = Path.Combine(contentRoot, "Data", "scripts");

            report.ProjectRoot = projectRoot;
            report.MapsRoot = mapsRoot;

            ProcessMapFile(report, mapPath, projectRoot, mapsRoot, texturesRoot, npcRoot, songsRoot, scriptsRoot);

            return report;
        }

        /// <summary>
        /// Scan a single map file and save a report named "map_asset_&lt;mapname&gt;.json"
        /// in ProjectRoot/Content/EditorReports/. Returns the report.
        /// </summary>
        public static MapAssetScannerReport ScanSingleMapAndSave(string mapPath)
        {
            var report = ScanSingleMap(mapPath);

            var root = GetReportRoot();
            var mapName = Path.GetFileNameWithoutExtension(mapPath);
            var fileName = $"map_asset_{mapName}.json";
            var outputPath = Path.Combine(root, fileName);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(report, options);
            File.WriteAllText(outputPath, json);

            report.ReportPath = outputPath;
            Debug.WriteLine($"[MapAssetScanner] Single-map report saved to {outputPath}");

            return report;
        }

        /// <summary>
        /// Saves the report as JSON into ProjectRoot/Content/EditorReports/map_asset_report.json.
        /// Sets report.ReportPath.
        /// </summary>
        public static void SaveReport(MapAssetScannerReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));

            var root = GetReportRoot();
            var outputPath = Path.Combine(root, "map_asset_report.json");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(report, options);
            File.WriteAllText(outputPath, json);

            report.ReportPath = outputPath;
            Debug.WriteLine($"[MapAssetScanner] Report saved to {outputPath}");
        }

        /// <summary>
        /// Convenience: scans all maps and immediately saves, returns the report.
        /// </summary>
        public static MapAssetScannerReport ScanAndSave()
        {
            var report = ScanAllMaps();
            SaveReport(report);
            return report;
        }

        /// <summary>
        /// Internal helper to process a single .dat map and update the report.
        /// </summary>
        private static void ProcessMapFile(
            MapAssetScannerReport report,
            string mapPath,
            string projectRoot,
            string mapsRoot,
            string texturesRoot,
            string npcRoot,
            string songsRoot,
            string scriptsRoot)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            if (string.IsNullOrWhiteSpace(mapPath)) return;

            report.MapsScanned.Add(Relative(projectRoot, mapPath));

            string text;
            try
            {
                text = File.ReadAllText(mapPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapAssetScanner] Failed to read map '{mapPath}': {ex.Message}");
                return;
            }

            // --- 1) TexturePath[...] ? Content/Textures/<id>.png
            foreach (Match m in TexturePathRegex.Matches(text))
            {
                var id = m.Groups["id"].Value.Trim();
                if (string.IsNullOrEmpty(id))
                    continue;

                var texFile = Path.Combine(texturesRoot, id + ".png");
                var relTexFile = Relative(projectRoot, texFile);

                if (!report.TextureFilesUsed.Contains(relTexFile, StringComparer.OrdinalIgnoreCase))
                    report.TextureFilesUsed.Add(relTexFile);

                if (!File.Exists(texFile) &&
                    !report.MissingTextureFiles.Contains(relTexFile, StringComparer.OrdinalIgnoreCase))
                {
                    report.MissingTextureFiles.Add(relTexFile);
                }
            }

            // --- 2) TextureID[...] ? Content/Textures/NPC/<id>.png
            foreach (Match m in TextureIdRegex.Matches(text))
            {
                var id = m.Groups["id"].Value.Trim();
                if (string.IsNullOrEmpty(id))
                    continue;

                var npcFile = Path.Combine(npcRoot, id + ".png");
                var relNpcFile = Relative(projectRoot, npcFile);

                if (!report.NpcTexturesUsed.Contains(relNpcFile, StringComparer.OrdinalIgnoreCase))
                    report.NpcTexturesUsed.Add(relNpcFile);

                if (!File.Exists(npcFile) &&
                    !report.MissingNpcTextures.Contains(relNpcFile, StringComparer.OrdinalIgnoreCase))
                {
                    report.MissingNpcTextures.Add(relNpcFile);
                }
            }

            // --- 3) MusicLoop[...] ? Content/Songs/<id>.(ogg|wav|mp3)
            foreach (Match m in MusicLoopRegex.Matches(text))
            {
                var id = m.Groups["id"].Value.Trim();
                if (string.IsNullOrEmpty(id))
                    continue;

                var candidates = new[]
                {
                    Path.Combine(songsRoot, id + ".ogg"),
                    Path.Combine(songsRoot, id + ".wav"),
                    Path.Combine(songsRoot, id + ".mp3"),
                };

                bool exists = false;
                foreach (var cand in candidates)
                {
                    var relCand = Relative(projectRoot, cand);
                    if (!report.MusicFilesUsed.Contains(relCand, StringComparer.OrdinalIgnoreCase))
                        report.MusicFilesUsed.Add(relCand);

                    if (File.Exists(cand))
                    {
                        exists = true;
                    }
                }

                if (!exists)
                {
                    var relPreferred = Relative(projectRoot, candidates[0]);
                    if (!report.MissingMusicFiles.Contains(relPreferred, StringComparer.OrdinalIgnoreCase))
                        report.MissingMusicFiles.Add(relPreferred);
                }
            }

            // --- 4) AdditionalValue[...] ? maps or scripts
            // Warp AdditionalValues often look like "routes\route34.dat,13,0.1,15,0"
            // Script AdditionalValues often look like "furniture\barktown\yourroom\townmap"
            foreach (Match m in AdditionalValueRegex.Matches(text))
            {
                var raw = m.Groups["val"].Value.Trim();
                if (string.IsNullOrEmpty(raw))
                    continue;

                // Split by comma to isolate first token
                var parts = raw.Split(',');
                var first = parts[0].Trim();

                if (string.IsNullOrEmpty(first))
                    continue;

                // Map reference if ends with ".dat"
                if (first.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                {
                    var relativeMap = first.Replace('\\', Path.DirectorySeparatorChar)
                                           .Replace('/', Path.DirectorySeparatorChar);

                    var targetMap = Path.Combine(mapsRoot, relativeMap);
                    var relTargetMap = Relative(projectRoot, targetMap);

                    if (!File.Exists(targetMap) &&
                        !report.MissingMapFiles.Contains(relTargetMap, StringComparer.OrdinalIgnoreCase))
                    {
                        report.MissingMapFiles.Add(relTargetMap);
                    }
                }
                else
                {
                    // Heuristic: treat as script path if looks like path-ish
                    if (first.Contains("\\") || first.Contains("/") ||
                        first.Contains("furniture", StringComparison.OrdinalIgnoreCase) ||
                        first.Contains("route", StringComparison.OrdinalIgnoreCase))
                    {
                        var relativeScript = first.Replace('\\', Path.DirectorySeparatorChar)
                                                  .Replace('/', Path.DirectorySeparatorChar);

                        // We don't know exact extension -> we'll just report the base path
                        var scriptBase = Path.Combine(scriptsRoot, relativeScript);
                        var relScriptBase = Relative(projectRoot, scriptBase);

                        if (!report.ScriptPathsUsed.Contains(relScriptBase, StringComparer.OrdinalIgnoreCase))
                            report.ScriptPathsUsed.Add(relScriptBase);

                        // Check for common extensions
                        var candidates = new[]
                        {
                            scriptBase + ".txt",
                            scriptBase + ".dat",
                            scriptBase + ".lua",
                            scriptBase + ".p3ds",
                        };

                        bool anyExists = false;
                        foreach (var cand in candidates)
                        {
                            if (File.Exists(cand))
                            {
                                anyExists = true;
                                break;
                            }
                        }

                        if (!anyExists &&
                            !report.MissingScriptPaths.Contains(relScriptBase, StringComparer.OrdinalIgnoreCase))
                        {
                            report.MissingScriptPaths.Add(relScriptBase);
                        }
                    }
                }
            }
        }

        private static string Relative(string root, string path)
        {
            try
            {
                var fullRoot = Path.GetFullPath(root);
                var fullPath = Path.GetFullPath(path);

                if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                    return fullPath;

                return fullPath.Substring(fullRoot.Length).TrimStart('\\', '/');
            }
            catch
            {
                return path;
            }
        }
    }
}