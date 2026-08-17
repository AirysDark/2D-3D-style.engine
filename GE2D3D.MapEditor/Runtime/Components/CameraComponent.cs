
using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Runtime.Components
{
    /// <summary>
    /// Designates an entity as a camera; used by the runtime camera system.
    /// </summary>
    public class CameraComponent : IRuntimeComponent
    {
        public bool IsMainCamera;
        public Vector3 Offset = new Vector3(0, 8, 16);
        public Vector3 LookAtOffset = Vector3.Zero;
    }
}
