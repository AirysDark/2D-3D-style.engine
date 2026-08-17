
using System;

namespace GE2D3D.MapEditor.Runtime.Systems
{
    /// <summary>
    /// Simple runtime debug overlay coordinator.
    /// The actual drawing is handled via the existing debug font / overlay systems.
    /// </summary>
    public class DebugOverlaySystem
    {
        public bool IsDebugOverlayVisible { get; private set; }

        public void Update(RuntimeScene scene, RuntimeInputSnapshot input, float dt)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));

            if (input.ToggleDebugOverlay)
            {
                IsDebugOverlayVisible = !IsDebugOverlayVisible;
            }
        }
    }
}
