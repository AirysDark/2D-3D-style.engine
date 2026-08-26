using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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

        public MapDiagnosticsReport()
        {
            RecordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            EntityIdCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            FieldCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            TexturePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ModelPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ReferencedMaps = new List<string>();
            UnknownRecordTypes = new List<string>();
        }
    }

    public static class MapDiagnosticsAnalyzer
    {
        private static readonly Regex RecordRegex = new Regex(
            "\\{\\\"(?<type>[^\\\"]+)\\\"\\{[A-Za-z]+\\[",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex FieldRegex = new Regex(
            "\\{\\\"(?<field>[^\\\"]+)\\\"\\{(?<kind>[A-Za-z]+)\\[",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex EntityIdRegex = new Regex(
            "\\{\\\"EntityID\\\"\\{str\\[(?<value>[^\\]]*)\\]\\}\\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex TexturePathRegex = new Regex(
            "\\{\\\"TexturePath\\\"\\{str\\[(?<value>[^\\]]*)\\]\\}\\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ModelPathRegex = new Regex(
            "\\{\\\"(?:ModelPath|AdditionalValue)\\\"\\{str\\[(?<value>[^\\]]*models[^\\]]*)\\]\\}\\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex MapRegex = new Regex(
            "\\{\\\"(?:Map|FilePath|MapPath)\\\"\\{str\\[(?<value>[^\\]]+\\.dat[^\\]]*)\\]\\}\\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static MapDiagnosticsReport Analyze(string mapPath)
        {
            MapDiagnosticsReport report = new MapDiagnosticsReport();

            if (String.IsNullOrWhiteSpace(mapPath) || !File.Exists(mapPath))
                return report;

            string text = File.ReadAllText(mapPath);

            HashSet<string> known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Level", "Actions", "Structure", "Floor", "Entity", "EntityField",
                "NPC", "Shader", "OffsetMap", "Backdrop"
            };

            foreach (Match match in RecordRegex.Matches(text))
            {
                string type = match.Groups["type"].Value;
                Increment(report.RecordCounts, type);

                if (!known.Contains(type) && !ContainsIgnoreCase(report.UnknownRecordTypes, type))
                    report.UnknownRecordTypes.Add(type);
            }

            foreach (Match match in FieldRegex.Matches(text))
            {
                string fieldName = match.Groups["field"].Value;
                Increment(report.FieldCounts, fieldName);
            }

            foreach (Match match in EntityIdRegex.Matches(text))
            {
                string entityId = match.Groups["value"].Value;
                if (!String.IsNullOrWhiteSpace(entityId))
                    Increment(report.EntityIdCounts, entityId);
            }

            foreach (Match match in TexturePathRegex.Matches(text))
            {
                string texturePath = match.Groups["value"].Value;
                if (!String.IsNullOrWhiteSpace(texturePath))
                    report.TexturePaths.Add(texturePath);
            }

            foreach (Match match in ModelPathRegex.Matches(text))
            {
                string modelPath = match.Groups["value"].Value;
                if (!String.IsNullOrWhiteSpace(modelPath))
                    report.ModelPaths.Add(modelPath);
            }

            foreach (Match match in MapRegex.Matches(text))
            {
                string referencedMap = match.Groups["value"].Value;
                if (!String.IsNullOrWhiteSpace(referencedMap))
                    report.ReferencedMaps.Add(referencedMap);
            }

            return report;
        }

        private static void Increment(Dictionary<string, int> dictionary, string key)
        {
            int count;
            if (dictionary.TryGetValue(key, out count))
                dictionary[key] = count + 1;
            else
                dictionary[key] = 1;
        }

        private static bool ContainsIgnoreCase(List<string> values, string value)
        {
            foreach (string item in values)
            {
                if (String.Equals(item, value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
