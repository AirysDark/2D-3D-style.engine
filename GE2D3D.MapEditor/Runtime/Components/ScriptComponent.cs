using GE2D3D.MapEditor.Runtime.Scripts;

namespace GE2D3D.MapEditor.Runtime.Components
{
    /// <summary>
    /// Component for attaching script-driven behaviour.
    /// ScriptName should contain a fully qualified type name implementing IGameScript.
    /// </summary>
    public class ScriptComponent : IRuntimeComponent
    {
        /// <summary>
        /// Fully qualified script type name, e.g. "MyGame.Scripts.NpcGuardScript".
        /// </summary>
        public string? ScriptName;

        /// <summary>
        /// Cached script instance created by the ScriptSystem.
        /// </summary>
        public IGameScript? Instance;
    }
}
