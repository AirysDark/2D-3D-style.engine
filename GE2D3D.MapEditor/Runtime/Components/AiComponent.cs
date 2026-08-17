using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Runtime.Components
{
    public enum AiBehaviour
    {
        Idle,
        Wander,
        Chase
    }

    /// <summary>
    /// Simple AI configuration for an entity.
    /// </summary>
    public class AiComponent : IRuntimeComponent
    {
        public AiBehaviour Behaviour = AiBehaviour.Idle;
        public float WanderRadius = 4.0f;
        public float ChaseRadius = 12.0f;
        public float MoveSpeed = 3.0f;

        public Vector3 HomePosition;
        public bool HomeInitialized;
    }
}
