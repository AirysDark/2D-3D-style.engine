using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Prism.Modules.D3D.Cameras;

namespace Prism.Modules.D3D.Picking
{
    /// <summary>
    /// Default implementation of IPickingService using MonoGame math.
    /// </summary>
    public class PickingService : IPickingService
    {
        public Ray GetPickRay(ICamera camera, Vector2 screenPosition, Viewport viewport)
        {
            var projection = camera.Projection;
            var view = camera.View;

            // Unproject screen point to world space
            var nearPoint = viewport.Unproject(
                new Vector3(screenPosition, 0f), projection, view, Matrix.Identity);

            var farPoint = viewport.Unproject(
                new Vector3(screenPosition, 1f), projection, view, Matrix.Identity);

            var direction = Vector3.Normalize(farPoint - nearPoint);
            return new Ray(nearPoint, direction);
        }

        public Vector3? IntersectRayWithPlane(Ray ray, Vector3 planeNormal, float planeD)
        {
            // Ray-plane intersection formula:
            // dot(N, R.Direction) * t = - (dot(N, R.Position) + D)

            float denom = Vector3.Dot(planeNormal, ray.Direction);

            // MonoGame removed MathHelper.WithinEpsilon ? use our own epsilon
            if (Math.Abs(denom) < 1e-6f)
                return null;    // Parallel to plane ? no hit

            float t = -(Vector3.Dot(planeNormal, ray.Position) + planeD) / denom;
            if (t < 0f)
                return null;    // Intersection behind the camera

            return ray.Position + ray.Direction * t;
        }
    }
}