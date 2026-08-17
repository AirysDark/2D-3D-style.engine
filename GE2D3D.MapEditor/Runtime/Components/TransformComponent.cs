
using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Runtime.Components
{
    /// <summary>
    /// Basic spatial transform for a runtime entity.
    /// </summary>
    public class TransformComponent : IRuntimeComponent
    {
        public Vector3 Position;
        public Vector3 Scale = Vector3.One;
        public Quaternion Rotation = Quaternion.Identity;
    }
}
