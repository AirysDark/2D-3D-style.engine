
using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Runtime.Systems
{
    /// <summary>
    /// High-level input snapshot for the runtime engine (separate from editor input).
    /// </summary>
    public struct RuntimeInputSnapshot
    {
        public Vector2 Move;      // XZ movement (e.g. WASD or stick)
        public bool Jump;
        public bool Interact;

        public bool ToggleDebugOverlay;
    }
}
