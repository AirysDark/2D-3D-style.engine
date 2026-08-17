using System;
using System.Collections.Generic;
using System.Linq;

namespace GE2D3D.MapEditor.Data.Models
{
    /// <summary>
    /// Lightweight metadata for an editor-visible prefab.
    /// Maps stable ModelId ? user-friendly name/category for the Asset Browser.
    /// Runtime still uses EntityInfo + ModelId and is unaffected by these values.
    /// </summary>
    public sealed class ModelPrefabInfo
    {
        public int ModelId { get; }
        public string Name { get; }
        public string Category { get; }

        public ModelPrefabInfo(int modelId, string name, string category)
        {
            ModelId = modelId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Category = category ?? string.Empty;
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Category))
                return $"{Name} (#{ModelId})";

            return $"{Name} [{Category}] (#{ModelId})";
        }
    }

    /// <summary>
    /// Complete built-in prefab catalog for the GE2D3D Editor.
    /// This drives the Asset Browser UI.
    /// Order of this array = default rendering order in the browser.
    /// </summary>
    public static class ModelPrefabCatalog
    {
        // --------------------------------------------------------------------
        //  Model Set (IDs 0?16)
        //  These MUST remain in sync with GE2D3D.Shared ? Model definitions.
        // --------------------------------------------------------------------
        private static readonly ModelPrefabInfo[] _all =
        {
            new ModelPrefabInfo(0,  "Floor",           "2D"),
            new ModelPrefabInfo(1,  "Block",           "Block"),
            new ModelPrefabInfo(2,  "Slide",           "Block"),
            new ModelPrefabInfo(3,  "Billboard",       "2D"),
            new ModelPrefabInfo(4,  "Sign",            "Prop"),

            new ModelPrefabInfo(5,  "Corner",          "Block"),
            new ModelPrefabInfo(6,  "Inside Corner",   "Block"),

            new ModelPrefabInfo(7,  "Step",            "Steps"),
            new ModelPrefabInfo(8,  "Inside Step",     "Steps"),

            new ModelPrefabInfo(9,  "Cliff",           "Cliffs"),
            new ModelPrefabInfo(10, "Cliff Inside",    "Cliffs"),
            new ModelPrefabInfo(11, "Cliff Corner",    "Cliffs"),

            new ModelPrefabInfo(12, "Cube",            "Block"),
            new ModelPrefabInfo(13, "Cross",           "2D"),
            new ModelPrefabInfo(14, "Double Floor",    "2D"),
            new ModelPrefabInfo(15, "Pyramid",         "Block"),

            // Fixed mismatched category ("Blocks" ? "Block")
            new ModelPrefabInfo(16, "Stairs",          "Block"),
        };

        public static IReadOnlyList<ModelPrefabInfo> All => _all;

        /// <summary>
        /// Lookup helper for selection, placement, ghost-preview, etc.
        /// </summary>
        public static ModelPrefabInfo? GetById(int modelId)
            => _all.FirstOrDefault(p => p.ModelId == modelId);
    }
}