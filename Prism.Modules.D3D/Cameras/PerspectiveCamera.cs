using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Prism.Modules.D3D.Cameras
{
    /// <summary>
    /// Simple perspective camera for 3D / 2.5D views.
    /// </summary>
    public class PerspectiveCamera : ICamera
    {
        private Matrix _view;
        private Matrix _projection;
        private int _viewportWidth;
        private int _viewportHeight;

        private float _yaw;
        private float _pitch;
        private float _fieldOfView = MathHelper.ToRadians(45f);

        public Matrix View => _view;
        public Matrix Projection => _projection;

        public Vector3 Position { get; set; }
        public Vector3 Target { get; set; }

        public float NearPlane { get; set; } = 0.1f;
        public float FarPlane { get; set; } = 5000f;

        public float Yaw
        {
            get => _yaw;
            set { _yaw = value; UpdateView(); }
        }

        public float Pitch
        {
            get => _pitch;
            set { _pitch = MathHelper.Clamp(value, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f); UpdateView(); }
        }

        public float FieldOfView
        {
            get => _fieldOfView;
            set { _fieldOfView = MathHelper.Clamp(value, MathHelper.ToRadians(10f), MathHelper.ToRadians(120f)); UpdateProjection(); }
        }

        public PerspectiveCamera()
        {
            Position = new Vector3(0, 10, 20);
            Target = Vector3.Zero;
            _yaw = 0f;
            _pitch = -0.4f;
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
            // Build a direction vector from yaw/pitch.
            var rotation = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f);
            var forward = Vector3.Transform(Vector3.Forward, rotation);
            Target = Position + forward;

            _view = Matrix.CreateLookAt(Position, Target, Vector3.Up);
        }

        private void UpdateProjection()
        {
            if (_viewportWidth <= 0 || _viewportHeight <= 0)
                return;

            float aspectRatio = (float)_viewportWidth / _viewportHeight;
            _projection = Matrix.CreatePerspectiveFieldOfView(_fieldOfView, aspectRatio, NearPlane, FarPlane);
        }

        public Ray GetPickRay(Vector2 screenPosition, Viewport viewport)
        {
            var nearPoint = viewport.Unproject(new Vector3(screenPosition, 0f), _projection, _view, Matrix.Identity);
            var farPoint = viewport.Unproject(new Vector3(screenPosition, 1f), _projection, _view, Matrix.Identity);

            var direction = Vector3.Normalize(farPoint - nearPoint);
            return new Ray(nearPoint, direction);
        }

        public void Orbit(Vector3 target, float deltaYaw, float deltaPitch, float distance)
        {
            _yaw += deltaYaw;
            _pitch = MathHelper.Clamp(_pitch + deltaPitch, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);

            var rotation = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f);
            Position = target + Vector3.Transform(new Vector3(0, 0, distance), rotation);
            UpdateView();
        }
    }
}
