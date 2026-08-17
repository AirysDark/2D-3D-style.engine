using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.Xna.Framework;

using GE2D3D.MapEditor.Data;

namespace GE2D3D.MapEditor.Utils
{
    public class LevelLoader
    {
        public static LevelInfo Load(string text, string path)
        {
            // Safety: if text is null/empty, return an "empty" level instead of NRE
            if (string.IsNullOrWhiteSpace(text))
            {
                Logger.Log(LogType.Error, $"LevelLoader.Load: empty text for path '{path}'");

                var emptyTags = new LevelTags();
                var emptyLevel = new LevelInfo(
                    emptyTags,
                    path,
                    new LevelTags(),
                    new List<EntityInfo>(),
                    new List<StructureInfo>(),
                    new List<OffsetMapInfo>(),
                    new ShaderInfo(),
                    new BackdropInfo()
                );

                return emptyLevel;
            }

            // Handle both \r\n and \n line endings safely
            var data = text
                .Replace("\r\n", "\n")
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .ToArray();

            var levelTags = new LevelTags();
            var actionTags = new LevelTags();
            var entities = new List<EntityInfo>();
            var structures = new List<StructureInfo>();
            var offsetMaps = new List<OffsetMapInfo>();
            var shader = new ShaderInfo();
            var backdrop = new BackdropInfo();

            var list = new List<LevelTag>(); // for debug if needed

            for (var i = 0; i < data.Length; i++)
            {
                var line = data[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Only lines with {} matter
                if (!line.Contains("{") || !line.Contains("}"))
                    continue;

                try
                {
                    // Match the VB logic:
                    // line = line.Remove(0, line.IndexOf("{") + 2)
                    var firstBraceIndex = line.IndexOf("{", StringComparison.Ordinal);
                    if (firstBraceIndex < 0 || firstBraceIndex + 2 >= line.Length)
                        continue;

                    var afterBrace = line.Substring(firstBraceIndex + 2);
                    var lower = afterBrace.ToLowerInvariant();

                    LevelTagType tagType = LevelTagType.None;

                    // ----------------- TYPE DETECTION (VB-style) -----------------
                    if (lower.StartsWith(@"structure"""))
                        tagType = LevelTagType.Structure;
                    else if (lower.StartsWith(@"entity"""))
                        tagType = LevelTagType.Entity;
                    else if (lower.StartsWith(@"floor"""))
                        tagType = LevelTagType.Floor;
                    else if (lower.StartsWith(@"entityfield"""))
                        tagType = LevelTagType.EntityField;
                    else if (lower.StartsWith(@"level"""))
                        tagType = LevelTagType.Level;
                    else if (lower.StartsWith(@"actions"""))
                        tagType = LevelTagType.LevelActions;
                    else if (lower.StartsWith(@"npc"""))
                        tagType = LevelTagType.NPC;
                    else if (lower.StartsWith(@"shader"""))
                        tagType = LevelTagType.Shader;
                    else if (lower.StartsWith(@"offsetmap"""))
                        tagType = LevelTagType.OffsetMap;
                    else if (lower.StartsWith(@"backdrop"""))
                        tagType = LevelTagType.Backdrop;

                    if (tagType == LevelTagType.None)
                        continue;

                    // ----------------- CUT OUT TAG BODY (VB-style) -----------------
                    // VB:
                    // line = line.Remove(0, line.IndexOf("[") + 1)
                    // line = line.Remove(line.Length - 3, 3)
                    var idxOpenBracket = afterBrace.IndexOf("[", StringComparison.Ordinal);
                    if (idxOpenBracket < 0 || idxOpenBracket + 1 >= afterBrace.Length)
                        continue;

                    var tagBody = afterBrace.Substring(idxOpenBracket + 1);
                    if (tagBody.Length < 3)
                        continue;

                    // strip trailing "]}}"
                    tagBody = tagBody.Substring(0, tagBody.Length - 3);

                    var tags = GetTags(tagBody);

                    // ----------------- HANDLE EACH TAG TYPE -----------------
                    switch (tagType)
                    {
                        // STRUCTURE: keep as metadata for now (editor can choose to expand later)
                        case LevelTagType.Structure:
                            {
                                var map = tags.TagExists("map") ? tags.GetTag<string>("map") : null;
                                var offsetArray = tags.TagExists("offset") ? tags.GetTag<float[]>("offset") : null;
                                var rotation = tags.TagExists("Rotation") ? tags.GetTag<int>("Rotation") : -1;
                                var addNPC = tags.TagExists("AddNPC") && tags.GetTag<bool>("AddNPC");

                                if (!string.IsNullOrEmpty(map) && offsetArray != null && offsetArray.Length >= 3)
                                {
                                    structures.Add(new StructureInfo
                                    {
                                        Map = map,
                                        Offset = new Vector3(offsetArray[0], offsetArray[1], offsetArray[2]),
                                        Rotation = rotation,
                                        AddNPC = addNPC
                                    });
                                }
                                else
                                {
                                    Logger.Log(LogType.Info,
                                        $"LevelLoader: invalid structure tag in '{path}' line {i + 1}");
                                }

                                break;
                            }

                        case LevelTagType.Entity:
                            try
                            {
                                var loaded = TagsLoader.LoadEntity(tags);
                                if (loaded != null)
                                    entities.AddRange(loaded);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LogType.Error,
                                    $"LevelLoader: exception in LoadEntity at '{path}' line {i + 1}: {ex.Message}");
                            }

                            break;

                        case LevelTagType.Floor:
                            try
                            {
                                var loaded = TagsLoader.LoadFloor(tags);
                                if (loaded != null)
                                    entities.AddRange(loaded);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LogType.Error,
                                    $"LevelLoader: exception in LoadFloor at '{path}' line {i + 1}: {ex.Message}");
                            }

                            break;

                        case LevelTagType.EntityField:
                            try
                            {
                                var loaded = TagsLoader.LoadEntityField(tags);
                                if (loaded != null)
                                    entities.AddRange(loaded);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LogType.Error,
                                    $"LevelLoader: exception in LoadEntityField at '{path}' line {i + 1}: {ex.Message}");
                            }

                            break;

                        case LevelTagType.Level:
                            levelTags = new LevelTags(tags);
                            break;

                        case LevelTagType.LevelActions:
                            actionTags = new LevelTags(tags);
                            break;

                        case LevelTagType.NPC:
                            // NPC loading is disabled in the original C# loader too
                            // (kept here for parity with the old behaviour)
                            break;

                        case LevelTagType.Shader:
                            try
                            {
                                shader = TagsLoader.LoadShader(tags) ?? new ShaderInfo();
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LogType.Error,
                                    $"LevelLoader: exception in LoadShader at '{path}' line {i + 1}: {ex.Message}");
                            }

                            break;

                        case LevelTagType.OffsetMap:
                            {
                                var map = tags.TagExists("map") ? tags.GetTag<string>("map") : null;
                                var offsetArray = tags.TagExists("offset") ? tags.GetTag<int[]>("offset") : null;

                                if (!string.IsNullOrEmpty(map) && offsetArray != null && offsetArray.Length >= 2)
                                {
                                    Vector3 offset;
                                    if (offsetArray.Length == 2)
                                        offset = new Vector3(offsetArray[0], offsetArray[1], 0);
                                    else
                                        offset = new Vector3(offsetArray[0], offsetArray[1], offsetArray[2]);

                                    offsetMaps.Add(new OffsetMapInfo
                                    {
                                        Map = map,
                                        Offset = offset
                                    });
                                }
                                else
                                {
                                    Logger.Log(LogType.Info,
                                        $"LevelLoader: invalid offsetmap tag in '{path}' line {i + 1}");
                                }

                                break;
                            }

                        case LevelTagType.Backdrop:
                            try
                            {
                                backdrop = TagsLoader.LoadBackdrop(tags) ?? new BackdropInfo();
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LogType.Error,
                                    $"LevelLoader: exception in LoadBackdrop at '{path}' line {i + 1}: {ex.Message}");
                            }

                            break;
                    }

                    list.Add(new LevelTag(tagType, tags));
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.Error,
                        $"LevelLoader: exception while parsing line {i + 1} in '{path}': {ex.Message}");
                    // swallow this line and move on so one bad line doesn't kill the whole level
                }
            }

            var levelInfo = new LevelInfo(levelTags, path, actionTags, entities, structures, offsetMaps, shader, backdrop);

            // wire back Parent so TextureHandler can use Entity.Parent
            foreach (var entity in levelInfo.Entities)
                entity.Parent = levelInfo;

            Logger.Log(LogType.Info,
                $"LevelLoader.Load: '{path}' => {levelInfo.Entities.Count} entities, {structures.Count} structures, {offsetMaps.Count} offset maps");

            return levelInfo;
        }

        public static LevelTags GetTags(string line)
        {
            var tags = new LevelTags();

            if (string.IsNullOrWhiteSpace(line))
                return tags;

            var tagList = line.Split(new[] { "}{" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < tagList.Length; i++)
            {
                var t = tagList[i];

                if (!t.EndsWith("}}", StringComparison.Ordinal))
                    t += "}";

                if (!t.StartsWith("{", StringComparison.Ordinal))
                    t = "{" + t;

                ProcessTag(ref tags, t);
            }

            return tags;
        }

        public static void ProcessTag(ref LevelTags tags, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            var tagName = "";
            var tagContent = "";

            // strip outer { ... }
            if (tag.Length < 2)
                return;

            tag = tag.Remove(0, 1);
            tag = tag.Remove(tag.Length - 1, 1);

            var idxBrace = tag.IndexOf("{", StringComparison.Ordinal);
            if (idxBrace > 0)
            {
                // e.g.  "Level"{LEV[...]   ->  tagName "level", tagContent "{LEV[...]"
                var rawNamePart = tag.Remove(idxBrace - 1);
                if (rawNamePart.Length > 1)
                    tagName = rawNamePart.Remove(0, 1).ToLowerInvariant();

                tagContent = tag.Remove(0, idxBrace);
            }

            if (string.IsNullOrEmpty(tagContent) || string.IsNullOrEmpty(tagName))
                return;

            var contentRows = tagContent.Split('}');
            for (var i = 0; i < contentRows.Length; i++)
            {
                if (contentRows[i].Length <= 0)
                    continue;

                var row = contentRows[i];

                if (!row.StartsWith("{", StringComparison.Ordinal))
                    row = "{" + row;

                // remove leading '{'
                if (row.Length <= 1)
                    continue;

                row = row.Remove(0, 1);

                var idxOpen = row.IndexOf("[", StringComparison.Ordinal);
                if (idxOpen < 0)
                    continue;

                var subTagType = row.Remove(idxOpen);          // e.g. "int"
                var subTagValue = row.Remove(0, idxOpen + 1);  // e.g. "123]"

                if (subTagValue.Length == 0)
                    continue;

                // strip trailing ']'
                if (subTagValue[subTagValue.Length - 1] == ']')
                    subTagValue = subTagValue.Remove(subTagValue.Length - 1, 1);

                var valueString = subTagValue ?? string.Empty;

                try
                {
                    switch (subTagType.ToLowerInvariant())
                    {
                        case "int":
                            tags.Add(tagName, int.Parse(valueString, CultureInfo.InvariantCulture));
                            break;

                        case "str":
                            tags.Add(tagName, valueString);
                            break;

                        case "sng":
                            tags.Add(tagName, float.Parse(valueString, CultureInfo.InvariantCulture));
                            break;

                        case "bool":
                            tags.Add(tagName, int.Parse(valueString, CultureInfo.InvariantCulture) == 1);
                            break;

                        case "intarr":
                            tags.Add(tagName,
                                valueString.Split(',')
                                           .Select(s => Convert.ToInt32(s, CultureInfo.InvariantCulture))
                                           .ToArray());
                            break;

                        case "rec":
                            {
                                var content = valueString.Split(',')
                                    .Select(s => int.Parse(s, CultureInfo.InvariantCulture))
                                    .ToArray();

                                if (content.Length >= 4)
                                    tags.Add(tagName, new Rectangle(content[0], content[1], content[2], content[3]));
                                break;
                            }

                        case "recarr":
                            {
                                var recs = valueString.Split(']')
                                    .Where(s => s.Length > 0)
                                    .Select(s => s.TrimStart('['))
                                    .Select(s => s.Split(','))
                                    .Select(arr => arr
                                        .Select(x => int.Parse(x, CultureInfo.InvariantCulture))
                                        .ToArray())
                                    .Where(a => a.Length >= 4)
                                    .Select(a => new Rectangle(a[0], a[1], a[2], a[3]))
                                    .ToArray();

                                tags.Add(tagName, recs);
                                break;
                            }

                        case "sngarr":
                            tags.Add(tagName,
                                valueString.Split(',')
                                           .Select(s => float.Parse(s, CultureInfo.InvariantCulture))
                                           .ToArray());
                            break;

                        default:
                            Logger.Log(LogType.Info, $"Unknown tag type! {subTagType.ToLowerInvariant()}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.Error,
                        $"LevelLoader.ProcessTag: error parsing tag '{tagName}' type '{subTagType}' value '{valueString}': {ex.Message}");
                }
            }
        }
    }
}