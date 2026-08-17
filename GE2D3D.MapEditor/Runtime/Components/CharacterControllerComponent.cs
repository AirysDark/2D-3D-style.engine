
using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Runtime.Components
{
    /// <summary>
    /// Minimal placeholder for a kinematic character controller.
    /// </summary>
    public class CharacterControllerComponent : IRuntimeComponent
    {
        public Vector3 Velocity;
        public bool IsGrounded;
        public float MoveSpeed = 6.0f;
        public float Gravity = -9.81f;
    }
}
