using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using GE2D3D.MapEditor.Data;

namespace GE2D3D.MapEditor.Utils
{
    public class LevelLoader
    {
        public static LevelInfo Load(string text, string path)
        {
            var stopwatch = Stopwatch.StartNew();
            MapDiagnosticsTrace.Begin(path);
            MapDiagnosticsTrace.Info("LevelLoader.Load entered", "Path=" + path);
            MapDiagnosticsTrace.Info("Raw map text received", "Characters=" + (text == null ? 0 : text.Length));

            if (string.IsNullOrWhiteSpace(text))
            {
                Logger.Log(LogType.Error, $"LevelLoader.Load: empty text for path '{path}'");
                MapDiagnosticsTrace.Error("Map text is empty", "LevelLoader returned an empty level instead of throwing.");
                var emptyTags = new LevelTags();
                var emptyLevel = new LevelInfo(emptyTags, path, new LevelTags(), new List<EntityInfo>(), new List<StructureInfo>(), new List<OffsetMapInfo>(), new ShaderInfo(), new BackdropInfo());
                MapDiagnosticsTrace.Performance("LevelLoader completed", "Elapsed=" + stopwatch.Elapsed.TotalMilliseconds.ToString("F1") + " ms");
                return emptyLevel;
            }

            var data = text.Replace("\r\n", "\n").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            MapDiagnosticsTrace.Success("Raw text split into records", "Non-empty lines=" + data.Length);

            var levelTags = new LevelTags();
            var actionTags = new LevelTags();
            var entities = new List<EntityInfo>();
            var structures = new List<StructureInfo>();
            var offsetMaps = new List<OffsetMapInfo>();
            var shader = new ShaderInfo();
            var backdrop = new BackdropInfo();
            var list = new List<LevelTag>();
            var topLevelCounts = new Dictionary<LevelTagType, int>();
            var skippedLines = 0;
            var parseFailures = 0;
            var npcCount = 0;

            for (var i = 0; i < data.Length; i++)
            {
                var line = data[i];
                if (string.IsNullOrWhiteSpace(line)) { skippedLines++; continue; }
                if (!line.Contains("{") || !line.Contains("}"))
                {
                    skippedLines++;
                    MapDiagnosticsTrace.Warning("Skipped non-map line", "Line=" + (i + 1));
                    continue;
                }

                try
                {
                    var firstBraceIndex = line.IndexOf("{", StringComparison.Ordinal);
                    if (firstBraceIndex < 0 || firstBraceIndex + 2 >= line.Length)
                    {
                        skippedLines++;
                        MapDiagnosticsTrace.Warning("Skipped malformed record", "Line=" + (i + 1) + " has no readable opening tag.");
                        continue;
                    }

                    var afterBrace = line.Substring(firstBraceIndex + 2);
                    var lower = afterBrace.ToLowerInvariant();
                    LevelTagType tagType = LevelTagType.None;
                    if (lower.StartsWith("structure\"")) tagType = LevelTagType.Structure;
                    else if (lower.StartsWith("entity\"")) tagType = LevelTagType.Entity;
                    else if (lower.StartsWith("floor\"")) tagType = LevelTagType.Floor;
                    else if (lower.StartsWith("entityfield\"")) tagType = LevelTagType.EntityField;
                    else if (lower.StartsWith("level\"")) tagType = LevelTagType.Level;
                    else if (lower.StartsWith("actions\"")) tagType = LevelTagType.LevelActions;
                    else if (lower.StartsWith("npc\"")) tagType = LevelTagType.NPC;
                    else if (lower.StartsWith("shader\"")) tagType = LevelTagType.Shader;
                    else if (lower.StartsWith("offsetmap\"")) tagType = LevelTagType.OffsetMap;
                    else if (lower.StartsWith("backdrop\"")) tagType = LevelTagType.Backdrop;

                    if (tagType == LevelTagType.None)
                    {
                        skippedLines++;
                        MapDiagnosticsTrace.Warning("Skipped unsupported top-level record", "Line=" + (i + 1) + " Preview=" + TrimForTrace(line));
                        continue;
                    }

                    Increment(topLevelCounts, tagType);
                    MapDiagnosticsTrace.Parser("Detected " + tagType, "Line=" + (i + 1));

                    var idxOpenBracket = afterBrace.IndexOf("[", StringComparison.Ordinal);
                    if (idxOpenBracket < 0 || idxOpenBracket + 1 >= afterBrace.Length)
                    {
                        parseFailures++;
                        MapDiagnosticsTrace.Error("Record body could not be located", tagType + " line=" + (i + 1));
                        continue;
                    }

                    var tagBody = afterBrace.Substring(idxOpenBracket + 1);
                    if (tagBody.Length < 3)
                    {
                        parseFailures++;
                        MapDiagnosticsTrace.Error("Record body is too short", tagType + " line=" + (i + 1));
                        continue;
                    }

                    tagBody = tagBody.Substring(0, tagBody.Length - 3);
                    var tags = GetTags(tagBody);
                    MapDiagnosticsTrace.Parser("Parsed record fields", tagType + " line=" + (i + 1) + " fields=" + tags.Count);

                    switch (tagType)
                    {
                        case LevelTagType.Structure:
                            {
                                var map = tags.TagExists("map") ? tags.GetTag<string>("map") : null;
                                var offsetArray = tags.TagExists("offset") ? tags.GetTag<float[]>("offset") : null;
                                var rotation = tags.TagExists("Rotation") ? tags.GetTag<int>("Rotation") : -1;
                                var addNPC = tags.TagExists("AddNPC") && tags.GetTag<bool>("AddNPC");
                                if (!string.IsNullOrEmpty(map) && offsetArray != null && offsetArray.Length >= 3)
                                {
                                    var info = new StructureInfo { Map = map, Offset = new Vector3(offsetArray[0], offsetArray[1], offsetArray[2]), Rotation = rotation, AddNPC = addNPC };
                                    structures.Add(info);
                                    MapDiagnosticsTrace.Success("Structure loaded", "Map=" + map + " Offset=" + info.Offset);
                                }
                                else
                                {
                                    Logger.Log(LogType.Info, $"LevelLoader: invalid structure tag in '{path}' line {i + 1}");
                                    MapDiagnosticsTrace.Warning("Structure rejected as invalid", "Line=" + (i + 1));
                                }
                                break;
                            }

                        case LevelTagType.Entity:
                            LoadEntityRecord(tags, entities, path, i + 1, "Entity");
                            break;

                        case LevelTagType.Floor:
                            try
                            {
                                var before = entities.Count;
                                var loaded = TagsLoader.LoadFloor(tags);
                                if (loaded != null) entities.AddRange(loaded);
                                MapDiagnosticsTrace.Success("Floor processed", "Line=" + (i + 1) + " objectsAdded=" + (entities.Count - before));
                            }
                            catch (Exception ex)
                            {
                                parseFailures++;
                                Logger.Log(LogType.Error, $"LevelLoader: exception in LoadFloor at '{path}' line {i + 1}: {ex.Message}");
                                MapDiagnosticsTrace.Error("Floor loader failed", "Line=" + (i + 1) + " Error=" + ex.Message);
                            }
                            break;

                        case LevelTagType.EntityField:
                            try
                            {
                                var before = entities.Count;
                                var loaded = TagsLoader.LoadEntityField(tags);
                                if (loaded != null) entities.AddRange(loaded);
                                MapDiagnosticsTrace.Success("EntityField processed", "Line=" + (i + 1) + " objectsAdded=" + (entities.Count - before));
                            }
                            catch (Exception ex)
                            {
                                parseFailures++;
                                Logger.Log(LogType.Error, $"LevelLoader: exception in LoadEntityField at '{path}' line {i + 1}: {ex.Message}");
                                MapDiagnosticsTrace.Error("EntityField loader failed", "Line=" + (i + 1) + " Error=" + ex.Message);
                            }
                            break;

                        case LevelTagType.Level:
                            levelTags = new LevelTags(tags);
                            MapDiagnosticsTrace.Success("Level settings parsed", "Line=" + (i + 1) + " fields=" + tags.Count);
                            break;

                        case LevelTagType.LevelActions:
                            actionTags = new LevelTags(tags);
                            MapDiagnosticsTrace.Success("Action settings parsed", "Line=" + (i + 1) + " fields=" + tags.Count);
                            break;

                        case LevelTagType.NPC:
                            npcCount++;
                            MapDiagnosticsTrace.Warning("NPC record detected but not loaded", "Line=" + (i + 1) + " NPC loading is currently disabled in LevelLoader.");
                            break;

                        case LevelTagType.Shader:
                            try
                            {
                                shader = TagsLoader.LoadShader(tags) ?? new ShaderInfo();
                                MapDiagnosticsTrace.Success("Shader processed", "Line=" + (i + 1));
                            }
                            catch (Exception ex)
                            {
                                parseFailures++;
                                Logger.Log(LogType.Error, $"LevelLoader: exception in LoadShader at '{path}' line {i + 1}: {ex.Message}");
                                MapDiagnosticsTrace.Error("Shader loader failed", "Line=" + (i + 1) + " Error=" + ex.Message);
                            }
                            break;

                        case LevelTagType.OffsetMap:
                            {
                                var map = tags.TagExists("map") ? tags.GetTag<string>("map") : null;
                                var offsetArray = tags.TagExists("offset") ? tags.GetTag<int[]>("offset") : null;
                                if (!string.IsNullOrEmpty(map) && offsetArray != null && offsetArray.Length >= 2)
                                {
                                    Vector3 offset = offsetArray.Length == 2 ? new Vector3(offsetArray[0], offsetArray[1], 0) : new Vector3(offsetArray[0], offsetArray[1], offsetArray[2]);
                                    offsetMaps.Add(new OffsetMapInfo { Map = map, Offset = offset });
                                    MapDiagnosticsTrace.Success("Offset map loaded", "Map=" + map + " Offset=" + offset);
                                }
                                else
                                {
                                    Logger.Log(LogType.Info, $"LevelLoader: invalid offsetmap tag in '{path}' line {i + 1}");
                                    MapDiagnosticsTrace.Warning("Offset map rejected as invalid", "Line=" + (i + 1));
                                }
                                break;
                            }

                        case LevelTagType.Backdrop:
                            try
                            {
                                backdrop = TagsLoader.LoadBackdrop(tags) ?? new BackdropInfo();
                                MapDiagnosticsTrace.Success("Backdrop processed", "Line=" + (i + 1));
                            }
                            catch (Exception ex)
                            {
                                parseFailures++;
                                Logger.Log(LogType.Error, $"LevelLoader: exception in LoadBackdrop at '{path}' line {i + 1}: {ex.Message}");
                                MapDiagnosticsTrace.Error("Backdrop loader failed", "Line=" + (i + 1) + " Error=" + ex.Message);
                            }
                            break;
                    }
                    list.Add(new LevelTag(tagType, tags));
                }
                catch (Exception ex)
                {
                    parseFailures++;
                    Logger.Log(LogType.Error, $"LevelLoader: exception while parsing line {i + 1} in '{path}': {ex.Message}");
                    MapDiagnosticsTrace.Error("Unhandled record parse failure", "Line=" + (i + 1) + " Error=" + ex.Message);
                }
            }

            var levelInfo = new LevelInfo(levelTags, path, actionTags, entities, structures, offsetMaps, shader, backdrop);
            foreach (var entity in levelInfo.Entities) entity.Parent = levelInfo;
            foreach (var pair in topLevelCounts.OrderBy(p => p.Key.ToString())) MapDiagnosticsTrace.Info("Top-level count", pair.Key + "=" + pair.Value);
            MapDiagnosticsTrace.Success("LevelInfo constructed", "Entities=" + levelInfo.Entities.Count + " Structures=" + structures.Count + " OffsetMaps=" + offsetMaps.Count + " NPCRecords=" + npcCount);
            MapDiagnosticsTrace.Info("Parser summary", "Skipped=" + skippedLines + " Failures=" + parseFailures + " ParsedRecords=" + list.Count);
            MapDiagnosticsTrace.Performance("LevelLoader completed", "Elapsed=" + stopwatch.Elapsed.TotalMilliseconds.ToString("F1") + " ms");
            Logger.Log(LogType.Info, $"LevelLoader.Load: '{path}' => {levelInfo.Entities.Count} entities, {structures.Count} structures, {offsetMaps.Count} offset maps");
            return levelInfo;
        }

        private static void LoadEntityRecord(LevelTags tags, List<EntityInfo> entities, string path, int lineNumber, string recordName)
        {
            try
            {
                var before = entities.Count;
                var loaded = TagsLoader.LoadEntity(tags);
                if (loaded != null) entities.AddRange(loaded);
                MapDiagnosticsTrace.Success(recordName + " processed", "Line=" + lineNumber + " objectsAdded=" + (entities.Count - before));
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, $"LevelLoader: exception in LoadEntity at '{path}' line {lineNumber}: {ex.Message}");
                MapDiagnosticsTrace.Error(recordName + " loader failed", "Line=" + lineNumber + " Error=" + ex.Message);
            }
        }

        public static LevelTags GetTags(string line)
        {
            var tags = new LevelTags();
            if (string.IsNullOrWhiteSpace(line)) return tags;
            var tagList = line.Split(new[] { "}{" }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < tagList.Length; i++)
            {
                var t = tagList[i];
                if (!t.EndsWith("}}", StringComparison.Ordinal)) t += "}";
                if (!t.StartsWith("{", StringComparison.Ordinal)) t = "{" + t;
                ProcessTag(ref tags, t);
            }
            return tags;
        }

        public static void ProcessTag(ref LevelTags tags, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            var tagName = "";
            var tagContent = "";
            if (tag.Length < 2) return;
            tag = tag.Remove(0, 1);
            tag = tag.Remove(tag.Length - 1, 1);
            var idxBrace = tag.IndexOf("{", StringComparison.Ordinal);
            if (idxBrace > 0)
            {
                var rawNamePart = tag.Remove(idxBrace - 1);
                if (rawNamePart.Length > 1) tagName = rawNamePart.Remove(0, 1).ToLowerInvariant();
                tagContent = tag.Remove(0, idxBrace);
            }
            if (string.IsNullOrEmpty(tagContent) || string.IsNullOrEmpty(tagName)) return;

            var contentRows = tagContent.Split('}');
            for (var i = 0; i < contentRows.Length; i++)
            {
                if (contentRows[i].Length <= 0) continue;
                var row = contentRows[i];
                if (!row.StartsWith("{", StringComparison.Ordinal)) row = "{" + row;
                if (row.Length <= 1) continue;
                row = row.Remove(0, 1);
                var idxOpen = row.IndexOf("[", StringComparison.Ordinal);
                if (idxOpen < 0) continue;
                var subTagType = row.Remove(idxOpen);
                var subTagValue = row.Remove(0, idxOpen + 1);
                if (subTagValue.Length == 0) continue;
                if (subTagValue[subTagValue.Length - 1] == ']') subTagValue = subTagValue.Remove(subTagValue.Length - 1, 1);
                var valueString = subTagValue ?? string.Empty;
                try
                {
                    switch (subTagType.ToLowerInvariant())
                    {
                        case "int": tags.Add(tagName, int.Parse(valueString, CultureInfo.InvariantCulture)); break;
                        case "str": tags.Add(tagName, valueString); break;
                        case "sng": tags.Add(tagName, float.Parse(valueString, CultureInfo.InvariantCulture)); break;
                        case "bool": tags.Add(tagName, int.Parse(valueString, CultureInfo.InvariantCulture) == 1); break;
                        case "intarr": tags.Add(tagName, valueString.Split(',').Select(s => Convert.ToInt32(s, CultureInfo.InvariantCulture)).ToArray()); break;
                        case "rec":
                            var content = valueString.Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                            if (content.Length >= 4) tags.Add(tagName, new Rectangle(content[0], content[1], content[2], content[3]));
                            break;
                        case "recarr":
                            var recs = valueString.Split(']').Where(s => s.Length > 0).Select(s => s.TrimStart('[')).Select(s => s.Split(',')).Select(arr => arr.Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToArray()).Where(a => a.Length >= 4).Select(a => new Rectangle(a[0], a[1], a[2], a[3])).ToArray();
                            tags.Add(tagName, recs);
                            break;
                        case "sngarr": tags.Add(tagName, valueString.Split(',').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray()); break;
                        default:
                            Logger.Log(LogType.Info, $"Unknown tag type! {subTagType.ToLowerInvariant()}");
                            MapDiagnosticsTrace.Warning("Unknown field value type", "Field=" + tagName + " Type=" + subTagType);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.Error, $"LevelLoader.ProcessTag: error parsing tag '{tagName}' type '{subTagType}' value '{valueString}': {ex.Message}");
                    MapDiagnosticsTrace.Error("Field parse failed", "Field=" + tagName + " Type=" + subTagType + " Error=" + ex.Message);
                }
            }
        }

        private static void Increment(Dictionary<LevelTagType, int> dictionary, LevelTagType key)
        {
            int count;
            dictionary.TryGetValue(key, out count);
            dictionary[key] = count + 1;
        }

        private static string TrimForTrace(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            const int maxLength = 120;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }
    }
}
