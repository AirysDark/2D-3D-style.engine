using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GE2D3D.MapEditor.Utils
{
    /// <summary>
    /// Builds a C# 10/.NET 6 compatible summary of every record/field name
    /// encountered in a P3D-style .dat map file. No regular expressions are used.
    /// </summary>
    internal static class MapLoadDebugSummary
    {
        public static string Build(string filePath, string text)
        {
            Dictionary<string, int> counts = CountTags(text);
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Loaded map: " + Path.GetFileName(filePath));
            builder.AppendLine();
            builder.AppendLine("MAP CONTENT COUNTS");
            builder.AppendLine(new string('-', 40));

            foreach (KeyValuePair<string, int> item in counts.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine(item.Key + ": " + item.Value);
            }

            return builder.ToString();
        }

        private static Dictionary<string, int> CountTags(string text)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(text))
                return counts;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '{')
                    continue;

                int start = i + 1;
                int end = start;

                while (end < text.Length && IsTagCharacter(text[end]))
                    end++;

                if (end == start || end >= text.Length || text[end] != '"')
                    continue;

                string name = text.Substring(start, end - start);
                int current;
                counts.TryGetValue(name, out current);
                counts[name] = current + 1;
            }

            return counts;
        }

        private static bool IsTagCharacter(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }
    }
}
