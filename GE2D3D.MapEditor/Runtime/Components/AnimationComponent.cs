using System;

namespace GE2D3D.MapEditor.Runtime.Components
{
    /// <summary>
    /// High-level animation state for an entity (no direct mesh binding yet).
    /// </summary>
    public class AnimationComponent : IRuntimeComponent
    {
        public string State = "Idle";
        public float Time;
        public float Speed = 1.0f;
    }
}
