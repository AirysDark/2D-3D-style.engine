using System;

namespace GE2D3D.MapEditor.Runtime.Systems
{
    /// <summary>
    /// Logical UI system surface for HUD/menus.
    /// Rendering is handled separately by overlay drawing code.
    /// </summary>
    public class UiSystem
    {
        public bool ShowHud = true;

        public void Update(RuntimeScene scene, float dt)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            // Hook for future HUD state updates.
        }
    }
}
