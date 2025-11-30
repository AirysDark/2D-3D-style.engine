using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Prism.Modules.D3D.Cameras
{
    /// <summary>
    /// Simple 2D / top-down orthographic camera.
    /// Designed for tile/map editors and 2D style rendering.
    /// </summary>
    public class OrthographicCamera : ICamera
    {
        private Matrix _view;
        private Matrix _projection;
        private int _viewportWidth;
        private int _viewportHeight;

        public Matrix View => _view;
        public Matrix Projection => _projection;

        public Vector3 Position { get; set; }
        public Vector3 Target { get; set; }

        public float NearPlane { get; set; } = 0.1f;
        public float FarPlane { get; set; } = 1000f;

        /// <summary>
        /// Zoom factor. 1.0 = 1:1 world:screen, 2.0 = zoomed in, 0.5 = zoomed out.
        /// </summary>
        public float Zoom
        {
            get => _zoom;
            set => _zoom = MathHelper.Clamp(value, 0.05f, 100f);
        }

        private float _zoom = 1.0f;

        public OrthographicCamera()
        {
            Position = new Vector3(0, 0, 0);
            Target = new Vector3(0, 0, 1);
        }

        public void UpdateViewport(int width, int height)
        {
            _viewportWidth = width;
            _viewportHeight = height;
            UpdateProjection();
            UpdateView();
        }

        private void UpdateView()
        {
            // 2D camera looking along +Z with Y down, X right.
            var translation = Matrix.CreateTranslation(-Position.X, -Position.Y, 0f);
            _view = translation;
        }

        private void UpdateProjection()
        {
            // Use zoom to scale visible world area.
            float worldWidth = _viewportWidth / _zoom;
            float worldHeight = _viewportHeight / _zoom;

            _projection = Matrix.CreateOrthographicOffCenter(
                0, worldWidth,
                worldHeight, 0,
                NearPlane, FarPlane);
        }

        public Ray GetPickRay(Vector2 screenPosition, Viewport viewport)
        {
            // For 2D, we treat the world as Z=0 plane.
            var nearPoint = viewport.Unproject(new Vector3(screenPosition, 0f), _projection, _view, Matrix.Identity);
            var farPoint = viewport.Unproject(new Vector3(screenPosition, 1f), _projection, _view, Matrix.Identity);

            var direction = Vector3.Normalize(farPoint - nearPoint);
            return new Ray(nearPoint, direction);
        }

        /// <summary>
        /// Moves the camera in world-space.
        /// </summary>
        public void Move(Vector2 delta)
        {
            Position += new Vector3(delta, 0f);
            UpdateView();
        }
    }
}
