using System;

namespace GE2D3D.MapEditor.Components.Tools
{
    /// <summary>
    /// High-level editor tool modes used by the viewport.
    /// This will eventually drive selection vs prefab placement,
    /// ghost previews, etc.
    /// </summary>
    public enum EditorToolMode
    {
        /// <summary>
        /// Default mode: click selects entities and manipulates the gizmo.
        /// </summary>
        Select = 0,

        /// <summary>
        /// Prefab placement mode: the viewport will place new entities
        /// using the currently selected prefab from the asset browser.
        /// (Wiring for this comes in later parts.)
        /// </summary>
        PlacePrefab = 1
    }
}
