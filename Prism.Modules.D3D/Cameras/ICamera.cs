using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Prism.Modules.D3D.Cameras
{
    /// <summary>
    /// Basic camera abstraction used by the D3D module and editor tools.
    /// </summary>
    public interface ICamera
    {
        Matrix View { get; }
        Matrix Projection { get; }

        Vector3 Position { get; set; }
        Vector3 Target { get; set; }

        float NearPlane { get; set; }
        float FarPlane { get; set; }

        /// <summary>
        /// Called when the viewport size changes so the projection can be updated.
        /// </summary>
        /// <param name="width">Viewport width in pixels.</param>
        /// <param name="height">Viewport height in pixels.</param>
        void UpdateViewport(int width, int height);

        /// <summary>
        /// Computes a picking ray from a screen-space coordinate.
        /// </summary>
        /// <param name="screenPosition">Screen-space position in pixels.</param>
        /// <param name="viewport">MonoGame viewport.</param>
        /// <returns>A world-space picking ray.</returns>
        Ray GetPickRay(Vector2 screenPosition, Viewport viewport);
    }
}
