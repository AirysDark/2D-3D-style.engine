using System;
using System.Collections.Generic;
using System.Linq;

namespace GE2D3D.MapEditor.Data.Models
{
    /// <summary>
    /// Lightweight metadata for a placeable model/prefab in the editor.
    /// This is purely editor-side; the runtime still works with EntityInfo + ModelID.
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
            return string.IsNullOrEmpty(Category)
                ? $"{Name} (#{ModelId})"
                : $"{Name} ({Category}, #{ModelId})";
        }
    }

    /// <summary>
    /// Static catalog of all editor-visible model prefabs.
    /// This gives the asset browser a stable, human-readable list of
    /// the built-in model IDs exposed by BaseModel.GetModelByEntityInfo.
    /// </summary>
    public static class ModelPrefabCatalog
    {
        // Order here controls how they appear in the asset browser by default.
        private static readonly ModelPrefabInfo[] _all =
        {
            new ModelPrefabInfo(0,  "Floor",          "2D"),
            new ModelPrefabInfo(1,  "Block",          "Block"),
            new ModelPrefabInfo(2,  "Slide",          "Block"),
            new ModelPrefabInfo(3,  "Billboard",      "2D"),
            new ModelPrefabInfo(4,  "Sign",           "Prop"),
            new ModelPrefabInfo(5,  "Corner",         "Block"),
            new ModelPrefabInfo(6,  "Inside Corner",  "Block"),
            new ModelPrefabInfo(7,  "Step",           "Steps"),
            new ModelPrefabInfo(8,  "Inside Step",    "Steps"),
            new ModelPrefabInfo(9,  "Cliff",          "Cliffs"),
            new ModelPrefabInfo(10, "Cliff Inside",   "Cliffs"),
            new ModelPrefabInfo(11, "Cliff Corner",   "Cliffs"),
            new ModelPrefabInfo(12, "Cube",           "Block"),
            new ModelPrefabInfo(13, "Cross",          "2D"),
            new ModelPrefabInfo(14, "Double Floor",   "2D"),
            new ModelPrefabInfo(15, "Pyramid",        "Block"),
            new ModelPrefabInfo(16, "Stairs",         "Blocks"),
        };

        public static IReadOnlyList<ModelPrefabInfo> All => _all;

        public static ModelPrefabInfo? GetById(int modelId)
        {
            return _all.FirstOrDefault(p => p.ModelId == modelId);
        }
    }
}
