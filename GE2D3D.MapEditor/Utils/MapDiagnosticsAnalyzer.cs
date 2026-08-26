using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GE2D3D.MapEditor.Utils
{
    public sealed class MapDiagnosticsReport
    {
        public Dictionary<string, int> RecordCounts { get; private set; }
        public Dictionary<string, int> EntityIdCounts { get; private set; }
        public Dictionary<string, int> FieldCounts { get; private set; }
        public HashSet<string> TexturePaths { get; private set; }
        public HashSet<string> ModelPaths { get; private set; }
        public List<string> ReferencedMaps { get; private set; }
        public List<string> UnknownRecordTypes { get; private set; }
        public List<string> Warnings { get; private set; }
        public long FileSizeBytes { get; set; }
        public int TextLength { get; set; }
        public int LineCount { get; set; }
        public int TagCount { get; set; }
        public int MalformedTagCount { get; set; }

        public MapDiagnosticsReport()
        {
            RecordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            EntityIdCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            FieldCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            TexturePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ModelPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ReferencedMaps = new List<string>();
            UnknownRecordTypes = new List<string>();
            Warnings = new List<string>();
        }
    }

    /// <summary>
    /// Raw P3D map inspector. It scans the actual quoted tag syntax used by maps:
    /// {"Floor"{...}}, {"Position"{...}}, etc. No regular expressions are used.
    /// </summary>
    public static class MapDiagnosticsAnalyzer
    {
        private static readonly HashSet<string> KnownTopLevelRecords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Level", "Actions", "Structure", "Floor", "Entity", "EntityField",
            "NPC", "Shader", "OffsetMap", "Backdrop", "Map"
        };

        public static MapDiagnosticsReport Analyze(string mapPath)
        {
            var report = new MapDiagnosticsReport();
            if (string.IsNullOrWhiteSpace(mapPath))
            {
                report.Warnings.Add("No map path supplied.");
                return report;
            }
            if (!File.Exists(mapPath))
            {
                report.Warnings.Add("Map file does not exist: " + mapPath);
                return report;
            }

            var fileInfo = new FileInfo(mapPath);
            report.FileSizeBytes = fileInfo.Length;
            var text = File.ReadAllText(mapPath);
            report.TextLength = text.Length;
            report.LineCount = CountLines(text);

            var names = ExtractQuotedTagNames(text, report);
            report.TagCount = names.Count;

            for (var i = 0; i < names.Count; i++)
            {
                var name = names[i];
                Increment(report.FieldCounts, name);
            }

            ClassifyTopLevelRecords(text, names, report);
            ExtractStringFieldValues(text, "EntityID", report.EntityIdCounts);
            ExtractStringSet(text, "TexturePath", report.TexturePaths);
            ExtractStringSet(text, "ModelPath", report.ModelPaths);
            ExtractAdditionalModelPaths(text, report.ModelPaths);
            ExtractMapReferences(text, report.ReferencedMaps);

            if (report.TagCount == 0)
                report.Warnings.Add("No quoted P3D tags were detected. Expected syntax such as {\"Floor\"{...}}.");

            return report;
        }

        private static List<string> ExtractQuotedTagNames(string text, MapDiagnosticsReport report)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text)) return result;

            for (var i = 0; i < text.Length - 2; i++)
            {
                if (text[i] != '{' || text[i + 1] != '"') continue;
                var end = text.IndexOf('"', i + 2);
                if (end < 0)
                {
                    report.MalformedTagCount++;
                    report.Warnings.Add("Unterminated quoted tag near character " + i + ".");
                    break;
                }

                var name = text.Substring(i + 2, end - (i + 2));
                if (IsValidTagName(name))
                    result.Add(name);
                else
                {
                    report.MalformedTagCount++;
                    report.Warnings.Add("Invalid tag name '" + name + "' near character " + i + ".");
                }

                i = end;
            }

            return result;
        }

        private static void ClassifyTopLevelRecords(string text, List<string> names, MapDiagnosticsReport report)
        {
            var topLevel = new HashSet<string>(KnownTopLevelRecords, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < names.Count; i++)
            {
                var name = names[i];
                if (!topLevel.Contains(name)) continue;
                Increment(report.RecordCounts, name);
            }

            // If a map contains a record-like tag with ENT/LEV/NPC/etc directly after
            // its opening brace, preserve it as unknown instead of silently dropping it.
            for (var i = 0; i < text.Length - 6; i++)
            {
                if (text[i] != '{' || text[i + 1] != '"') continue;
                var quoteEnd = text.IndexOf('"', i + 2);
                if (quoteEnd < 0) break;
                var name = text.Substring(i + 2, quoteEnd - (i + 2));
                var valueStart = quoteEnd + 1;
                if (valueStart + 3 >= text.Length || text[valueStart] != '{') continue;
                var bracket = text.IndexOf('[', valueStart + 1);
                if (bracket < 0 || bracket - valueStart > 16) continue;
                var token = text.Substring(valueStart + 1, bracket - valueStart - 1);
                if (!LooksLikeRecordContainer(token)) continue;
                if (!KnownTopLevelRecords.Contains(name))
                {
                    Increment(report.RecordCounts, name);
                    if (!ContainsIgnoreCase(report.UnknownRecordTypes, name))
                        report.UnknownRecordTypes.Add(name);
                }
            }
        }

        private static void ExtractStringFieldValues(string text, string fieldName, Dictionary<string, int> destination)
        {
            foreach (var value in ExtractFieldValues(text, fieldName))
            {
                if (!string.IsNullOrWhiteSpace(value)) Increment(destination, value);
            }
        }

        private static void ExtractStringSet(string text, string fieldName, HashSet<string> destination)
        {
            foreach (var value in ExtractFieldValues(text, fieldName))
            {
                if (!string.IsNullOrWhiteSpace(value)) destination.Add(value);
            }
        }

        private static void ExtractAdditionalModelPaths(string text, HashSet<string> destination)
        {
            foreach (var value in ExtractFieldValues(text, "AdditionalValue"))
            {
                if (value.IndexOf("model", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    value.IndexOf("models/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    value.IndexOf("models\\", StringComparison.OrdinalIgnoreCase) >= 0)
                    destination.Add(value);
            }
        }

        private static void ExtractMapReferences(string text, List<string> destination)
        {
            string[] names = { "Map", "FilePath", "MapPath" };
            foreach (var name in names)
            {
                foreach (var value in ExtractFieldValues(text, name))
                {
                    if (value.IndexOf(".dat", StringComparison.OrdinalIgnoreCase) >= 0 && !ContainsIgnoreCase(destination, value))
                        destination.Add(value);
                }
            }
        }

        private static IEnumerable<string> ExtractFieldValues(string text, string fieldName)
        {
            var marker = "{\"" + fieldName + "\"{str[";
            var index = 0;
            while (index < text.Length)
            {
                index = text.IndexOf(marker, index, StringComparison.OrdinalIgnoreCase);
                if (index < 0) yield break;
                var start = index + marker.Length;
                var end = text.IndexOf("]", start, StringComparison.Ordinal);
                if (end > start) yield return text.Substring(start, end - start).Trim();
                index = end > start ? end + 1 : start;
            }
        }

        private static bool LooksLikeRecordContainer(string token)
        {
            return token.Equals("ENT", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("LEV", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("ACT", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("NPC", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("STR", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("OFF", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("BCK", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("SHD", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidTagName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            foreach (var character in name)
            {
                if (!char.IsLetterOrDigit(character) && character != '_') return false;
            }
            return true;
        }

        private static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var count = 1;
            for (var i = 0; i < text.Length; i++) if (text[i] == '\n') count++;
            return count;
        }

        private static void Increment(Dictionary<string, int> dictionary, string key)
        {
            int count;
            dictionary.TryGetValue(key, out count);
            dictionary[key] = count + 1;
        }

        private static bool ContainsIgnoreCase(IEnumerable<string> values, string value)
        {
            foreach (var item in values)
                if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
