using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Prism.Modules.D3D.Cameras;

namespace Prism.Modules.D3D.Picking
{
    /// <summary>
    /// Provides picking helpers used by editor tools.
    /// </summary>
    public interface IPickingService
    {
        Ray GetPickRay(ICamera camera, Vector2 screenPosition, Viewport viewport);

        /// <summary>
        /// Intersects a ray with a plane defined by normal and distance-from-origin.
        /// Returns null if there is no intersection.
        /// </summary>
        Vector3? IntersectRayWithPlane(Ray ray, Vector3 planeNormal, float planeD);
    }
}
