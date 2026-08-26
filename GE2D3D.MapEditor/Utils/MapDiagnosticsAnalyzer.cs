using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GE2D3D.MapEditor.Utils
{
    /// <summary>
    /// Inspects the raw P3D-style map grammar without assuming a fixed list of
    /// records. Used by the editor to report what a map actually contains.
    /// </summary>
    public sealed class MapDiagnosticsReport
    {
        public Dictionary<string, int> RecordCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> EntityIdCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> FieldCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> TexturePaths { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ModelPaths { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> ReferencedMaps { get; } = new();
        public List<string> UnknownRecordTypes { get; } = new();
    }

    public static class MapDiagnosticsAnalyzer
    {
        private static readonly Regex RecordRegex = new Regex(
            @"\{\"(?<type>[^\"]+)\"\{[A-Za-z]+\[",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex FieldRegex = new Regex(
            @"\{\"(?<field>[^\"]+)\"\{(?<kind>[A-Za-z]+)\[",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex EntityIdRegex = new Regex(
            @"\{\"EntityID\"\{str\[(?<value>[^\]]*)\]\}\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex TexturePathRegex = new Regex(
            @"\{\"TexturePath\"\{str\[(?<value>[^\]]*)\]\}\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ModelPathRegex = new Regex(
            @"\{\"(?:ModelPath|AdditionalValue)\"\{str\[(?<value>[^\]]*models[^\]]*)\]\}\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex MapRegex = new Regex(
            @"\{\"Map\"\{str\[(?<value>[^\]]+\.dat[^\]]*)\]\}\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static MapDiagnosticsReport Analyze(string mapPath)
        {
            var report = new MapDiagnosticsReport();
            if (string.IsNullOrWhiteSpace(mapPath) || !File.Exists(mapPath))
                return report;

            var text = File.ReadAllText(mapPath);
            var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Level", "Actions", "Structure", "Floor", "Entity", "EntityField",
                "NPC", "Shader", "OffsetMap", "Backdrop"
            };

            foreach (Match match in RecordRegex.Matches(text))
            {
                var type = match.Groups["type"].Value;
                report.RecordCounts[type] = report.RecordCounts.TryGetValue(type, out var count) ? count + 1 : 1;
                if (!known.Contains(type) && !report.UnknownRecordTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
                    report.UnknownRecordTypes.Add(type);
            }

            foreach (Match match in FieldRegex.Matches(text))
            {
                var field = match.Groups["field"].Value;
                report.FieldCounts[field] = report.FieldCounts.TryGetValue(field, out var count) ? count + 1 : 1;
            }

            foreach (Match match in EntityIdRegex.Matches(text))
            {
                var id = match.Groups["value"].Value;
                if (!string.IsNullOrWhiteSpace(id))
                    report.EntityIdCounts[id] = report.EntityIdCounts.TryGetValue(id, out var count) ? count + 1 : 1;
            }

            foreach (Match match in TexturePathRegex.Matches(text))
                if (!string.IsNullOrWhiteSpace(match.Groups["value"].Value)) report.TexturePaths.Add(match.Groups["value"].Value);

            foreach (Match match in ModelPathRegex.Matches(text))
                if (!string.IsNullOrWhiteSpace(match.Groups["value"].Value)) report.ModelPaths.Add(match.Groups["value"].Value);

            foreach (Match match in MapRegex.Matches(text))
                if (!string.IsNullOrWhiteSpace(match.Groups["value"].Value)) report.ReferencedMaps.Add(match.Groups["value"].Value);

            return report;
        }
    }
}
