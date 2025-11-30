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
            // ----------------------------------------------------------------
            // Safety: if text is null/empty, return an "empty" level instead of NRE
            // ----------------------------------------------------------------
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
                    // Take a copy so we don't mangle the original
                    var currLine = line;

                    // Basic sanity ? must have at least one '{'
                    var firstBraceIndex = currLine.IndexOf("{", StringComparison.Ordinal);
                    if (firstBraceIndex < 0)
                        continue;

                    // Strip everything up to `{` + the " character (like original code did)
                    if (firstBraceIndex + 2 > currLine.Length)
                        continue;

                    var working = currLine.Remove(0, firstBraceIndex + 2);

                    // Now strip until "[" and final "}}"
                    var idxOpenBracket = working.IndexOf("[", StringComparison.Ordinal);
                    var idxLast = working.LastIndexOf("}}", StringComparison.Ordinal);

                    if (idxOpenBracket < 0 || idxLast < 0 || idxLast <= idxOpenBracket)
                        continue;

                    if (idxOpenBracket + 1 > working.Length || working.Length - 3 < 0)
                        continue;

                    working = working.Remove(0, idxOpenBracket + 1);
                    working = working.Remove(working.Length - 3, 3);

                    var tags = GetTags(working);

                    LevelTagType tagType = LevelTagType.None;
                    var lower = currLine.ToLowerInvariant();

                    // ----------------- STRUCTURE -----------------
                    if (lower.StartsWith(@"structure""", StringComparison.Ordinal))
                    {
                        tagType = LevelTagType.Structure;

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
                    }
                    // ----------------- ENTITY -----------------
                    else if (lower.StartsWith(@"entity""", StringComparison.Ordinal))
                    {
                        tagType = LevelTagType.Entity;
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
                    }
                    // ----------------- FLOOR -----------------
                    else if (lower.StartsWith(@"floor""", StringComparison.Ordinal))
                    {
                        tagType = LevelTagType.Floor;
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
                    }
                    // ----------------- ENTITYFIELD -----------------
                    else if (lower.StartsWith(@"entityfield""", StringComparison.Ordinal))
                    {
                        tagType = LevelTagType.EntityField;
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
                    }
                    // ----------------- LEVEL -----------------
                    else if (lower.StartsWith(@"level""", StringComparison.Ordinal))
                    {
                        tagType = LevelTagType.Level;
                        levelTags = new LevelTags(tags);
                    }
                    // ----------------- ACTIONS -----------------
                    else if (lower.StartsWith(@"actions""", StringComparison.Ordinal))
                    {
                        tagType = LevelTagType.LevelActions;
                        actionTags = new LevelTags(tags);
                    }
                    // ----------------- NPC -----------------
                    else if (lower.StartsWith(@"npc""", StringComparison.Ordinal))
                    {
                        tagType = LevelTagType.NPC;
                        // NPC loading was commented out in original source;
                        // leaving it disabled to avoid untested behavior.
                        // entities.Add(TagsLoader.LoadNpc(tags));
                    }
                    // ----------------- SHADER -----------------
                    else if (lower.StartsWith(@"shader""", StringComparison.Ordinal))
                    {
                        tagType = LevelTagType.Shader;
                        try
                        {
                            shader = TagsLoader.LoadShader(tags) ?? new ShaderInfo();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(LogType.Error,
                                $"LevelLoader: exception in LoadShader at '{path}' line {i + 1}: {ex.Message}");
                        }
                    }
                    // ----------------- OFFSETMAP -----------------
                    else if (lower.StartsWith(@"offsetmap""", StringComparison.Ordinal))
                    {
                        tagType = LevelTagType.OffsetMap;

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
                    }
                    // ----------------- BACKDROP -----------------
                    else if (lower.StartsWith(@"backdrop""", StringComparison.Ordinal))
                    {
                        tagType = LevelTagType.Backdrop;
                        try
                        {
                            backdrop = TagsLoader.LoadBackdrop(tags) ?? new BackdropInfo();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(LogType.Error,
                                $"LevelLoader: exception in LoadBackdrop at '{path}' line {i + 1}: {ex.Message}");
                        }
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