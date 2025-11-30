using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GE2D3D.MapEditor.Components.Camera
{
    /// <summary>
    /// 3D editor camera built on BaseCamera.
    /// - Uses yaw / pitch for orientation
    /// - Works with RenderBootstrap + editor controller
    /// - No direct input here; input is handled externally
    /// </summary>
    public class Camera3DPrism : BaseCamera
    {
        private float _yaw;
        private float _pitch;

        /// <summary>
        /// Current yaw (radians).
        /// </summary>
        public float Yaw => _yaw;

        /// <summary>
        /// Current pitch (radians).
        /// </summary>
        public float Pitch => _pitch;

        public Camera3DPrism(GraphicsDevice graphicsDevice)
            : base(graphicsDevice)
        {
            // Default position / target
            _eye = new Vector3(0, 10, 25);
            _target = Vector3.Zero;

            // Start with a slight downward pitch
            _yaw = 0f;
            _pitch = -0.25f;

            BuildProjection(graphicsDevice.Viewport);
            RebuildViewFromYawPitch();
        }

        public override void Initialize()
        {
            // Nothing extra needed at the moment.
        }

        #region BaseCamera abstract hooks

        /// <summary>
        /// In MonoGame window we could warp the cursor; for WPF we ignore this.
        /// Editor input code should not rely on recentering.
        /// </summary>
        protected override void SetMousePosition(int x, int y)
        {
            // Intentionally left blank for editor/WPF host.
        }

        protected override void SetMouseCursorVisible(bool visible)
        {
            // Editor/WPF usually keeps the cursor visible; no-op here.
        }

        protected override Point GetScreenCenter()
        {
            // Used by BaseCamera if you decide to call UpdateMouse.
            return new Point(
                GraphicsDevice.Viewport.Width / 2,
                GraphicsDevice.Viewport.Height / 2);
        }

        #endregion

        #region Yaw / Pitch control

        /// <summary>
        /// Set yaw/pitch directly (in radians).
        /// </summary>
        public void SetYawPitch(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = MathHelper.Clamp(pitch, -1.5f, 1.5f);
            RebuildViewFromYawPitch();
        }

        /// <summary>
        /// Apply delta yaw/pitch (in radians).
        /// </summary>
        public void AddYawPitch(float deltaYaw, float deltaPitch)
        {
            _yaw += deltaYaw;
            _pitch = MathHelper.Clamp(_pitch + deltaPitch, -1.5f, 1.5f);
            RebuildViewFromYawPitch();
        }

        #endregion

        #region Movement helpers

        /// <summary>
        /// Move camera in world space and rebuild view.
        /// External controller can call this (e.g. WASD).
        /// </summary>
        public void MoveLocal(float forward, float right, float up)
        {
            // Local axes from current orientation:
            var rot = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f);

            var fwd = Vector3.Normalize(Vector3.Transform(Vector3.Forward, rot));
            var rightVec = Vector3.Normalize(Vector3.Transform(Vector3.Right, rot));
            var upVec = Vector3.Up; // world up

            _eye += fwd * forward + rightVec * right + upVec * up;
            RebuildViewFromYawPitch();
        }

        /// <summary>
        /// Rebuild projection after resize.
        /// </summary>
        public void RebuildProjectionOnResize(Viewport vp)
        {
            BuildProjection(vp);
            RebuildViewFromYawPitch();
        }

        #endregion

        #region Internal helpers

        private void BuildProjection(Viewport viewport)
        {
            float aspect = viewport.Width / (float)viewport.Height;
            ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(60f),
                aspect,
                0.1f,
                5000f);
        }

        private void RebuildViewFromYawPitch()
        {
            // Build orientation quaternion from yaw/pitch
            var rot = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f);
            Quaternion.CreateFromRotationMatrix(ref rot, out _orientation);

            // Update view matrix + axes via BaseCamera helper
            UpdateViewMatrix();

            // Make Target follow the current forward direction (for helpers like FocusOnBounds)
            _target = _eye + _viewDir;
        }

        #endregion
    }
}