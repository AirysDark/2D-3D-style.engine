using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using GE2D3D.MapEditor.Components.Camera;

namespace GE2D3D.MapEditor.Components
{
    /// <summary>
    /// Editor-style camera controls:
    /// - WASD: move in XZ plane relative to camera
    /// - Space / LeftCtrl: move up/down
    /// - Right mouse drag: orbit (yaw/pitch)
    /// - Scroll wheel: zoom along forward vector
    ///
    /// Also exposes explicit methods for WPF:
    /// - SetPresetTop/Front/Side
    /// - OnMouseDrag / OnMouseWheel / EndDrag
    /// - OnKeyDown / OnKeyUp (WPF Key events)
    /// </summary>
    public class EditorCameraController : GameComponent
    {
        private readonly BaseCamera _camera;
        private MouseState _lastMouseState;

        private float _yaw;
        private float _pitch;

        public float MoveSpeed { get; set; } = 20f;
        public float MouseSensitivity { get; set; } = 0.01f;
        public float ZoomSpeed { get; set; } = 5f;

        // WPF-driven movement flags (for SceneView key events)
        private bool _moveForward;
        private bool _moveBack;
        private bool _moveLeft;
        private bool _moveRight;
        private bool _moveUp;
        private bool _moveDown;

        public EditorCameraController(Game game, BaseCamera camera)
            : base(game)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _lastMouseState = Mouse.GetState();

            // Initialize yaw/pitch from camera forward if possible
            var fwd = _camera.Forward;
            if (fwd != Vector3.Zero)
            {
                fwd.Normalize();
                _yaw = (float)Math.Atan2(fwd.X, fwd.Z);
                _pitch = (float)Math.Asin(fwd.Y);
            }
            else
            {
                _yaw = 0f;
                _pitch = 0f;
            }
        }

        public override void Update(GameTime gameTime)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();

            // -----------------------------------------------------------------
            // WASD + vertical (space / ctrl) movement
            //   - works from BOTH MonoGame keyboard state
            //   - AND WPF key flags (_moveForward, etc.)
            // -----------------------------------------------------------------
            var move = Vector3.Zero;

            // Flatten forward/right to XZ plane for movement
            var forward = _camera.Forward;
            var right = _camera.Right;

            forward.Y = 0f;
            right.Y = 0f;

            if (forward != Vector3.Zero)
                forward.Normalize();
            if (right != Vector3.Zero)
                right.Normalize();

            bool forwardPressed = keyboard.IsKeyDown(Keys.W) || _moveForward;
            bool backPressed = keyboard.IsKeyDown(Keys.S) || _moveBack;
            bool rightPressed = keyboard.IsKeyDown(Keys.D) || _moveRight;
            bool leftPressed = keyboard.IsKeyDown(Keys.A) || _moveLeft;
            bool upPressed = keyboard.IsKeyDown(Keys.Space) || _moveUp;
            bool downPressed = keyboard.IsKeyDown(Keys.LeftControl) || _moveDown;

            if (forwardPressed) move += forward;
            if (backPressed) move -= forward;
            if (rightPressed) move += right;
            if (leftPressed) move -= right;

            if (upPressed) move += Vector3.Up;
            if (downPressed) move += Vector3.Down;

            if (move != Vector3.Zero)
            {
                move.Normalize();
                _camera.Position += move * MoveSpeed * dt;

                // keep target in front of camera when moving
                UpdateOrbitDirection();
            }

            // -----------------------------------------------------------------
            // Right mouse orbit (standalone MonoGame use)
            // -----------------------------------------------------------------
            if (mouse.RightButton == ButtonState.Pressed &&
                _lastMouseState.RightButton == ButtonState.Pressed)
            {
                var dx = mouse.X - _lastMouseState.X;
                var dy = mouse.Y - _lastMouseState.Y;

                _yaw += dx * MouseSensitivity;
                _pitch += dy * MouseSensitivity;

                // Clamp pitch so we don't flip upside down
                _pitch = MathHelper.Clamp(_pitch, -1.4f, 1.4f);

                UpdateOrbitDirection();
            }

            // -----------------------------------------------------------------
            // Scroll wheel zoom (standalone MonoGame use)
            // -----------------------------------------------------------------
            var scrollDelta = mouse.ScrollWheelValue - _lastMouseState.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                ZoomBy(scrollDelta);
            }

            _lastMouseState = mouse;

            base.Update(gameTime);
        }

        /// <summary>
        /// Recompute camera.Target from our yaw/pitch and current Position.
        /// </summary>
        private void UpdateOrbitDirection()
        {
            // Standard spherical -> cartesian conversion
            var dir = new Vector3(
                (float)(Math.Cos(_pitch) * Math.Sin(_yaw)), // X
                (float)(Math.Sin(_pitch)),                  // Y
                (float)(Math.Cos(_pitch) * Math.Cos(_yaw))  // Z
            );

            if (dir != Vector3.Zero)
                dir.Normalize();

            _camera.Target = _camera.Position + dir;
        }

        private void ZoomBy(int wheelDelta)
        {
            var zoomDir = _camera.Forward;
            if (zoomDir == Vector3.Zero)
                return;

            zoomDir.Normalize();
            _camera.Position += zoomDir * (wheelDelta / 120f) * ZoomSpeed;
            UpdateOrbitDirection();
        }

        // =====================================================================
        // WPF / external control API (called from SceneView.xaml.cs)
        // =====================================================================

        public void SetPresetTop()
        {
            // Look straight down at current target, at current distance.
            var target = _camera.Target;
            var dir = _camera.Position - target;
            var dist = dir.Length();
            if (dist <= 0.001f)
                dist = 50f;

            _camera.Position = target + Vector3.Up * dist;

            // direction from camera to target is straight down
            var toTarget = target - _camera.Position;
            if (toTarget != Vector3.Zero)
            {
                toTarget.Normalize();
                _yaw = (float)Math.Atan2(toTarget.X, toTarget.Z);
                _pitch = (float)Math.Asin(toTarget.Y);
            }

            UpdateOrbitDirection();
        }

        public void SetPresetFront()
        {
            // Simple "front" preset: look along -Z toward target.
            var target = _camera.Target;
            var dir = _camera.Position - target;
            var dist = dir.Length();
            if (dist <= 0.001f)
                dist = 50f;

            _camera.Position = target + new Vector3(0f, 0f, dist);

            var toTarget = target - _camera.Position;
            if (toTarget != Vector3.Zero)
            {
                toTarget.Normalize();
                _yaw = (float)Math.Atan2(toTarget.X, toTarget.Z);
                _pitch = (float)Math.Asin(toTarget.Y);
            }

            UpdateOrbitDirection();
        }

        public void SetPresetSide()
        {
            // Simple "side" preset: look along -X toward target.
            var target = _camera.Target;
            var dir = _camera.Position - target;
            var dist = dir.Length();
            if (dist <= 0.001f)
                dist = 50f;

            _camera.Position = target + new Vector3(dist, 0f, 0f);

            var toTarget = target - _camera.Position;
            if (toTarget != Vector3.Zero)
            {
                toTarget.Normalize();
                _yaw = (float)Math.Atan2(toTarget.X, toTarget.Z);
                _pitch = (float)Math.Asin(toTarget.Y);
            }

            UpdateOrbitDirection();
        }

        /// <summary>
        /// Mouse drag from WPF view. dx,dy in screen pixels.
        /// Called while right mouse is held.
        /// </summary>
        public void OnMouseDrag(float dx, float dy, bool rightButtonDown)
        {
            if (!rightButtonDown)
                return;

            _yaw += dx * MouseSensitivity;
            _pitch += dy * MouseSensitivity;

            _pitch = MathHelper.Clamp(_pitch, -1.4f, 1.4f);

            UpdateOrbitDirection();
        }

        /// <summary>
        /// Mouse wheel (zoom) from WPF view. Positive delta = wheel up.
        /// </summary>
        public void OnMouseWheel(int delta)
        {
            if (delta == 0)
                return;

            ZoomBy(delta);
        }

        /// <summary>
        /// Called when the right mouse button is released in WPF.
        /// </summary>
        public void EndDrag()
        {
            // No special state to clear yet, but method exists
            // so SceneView can safely call it.
        }

        // =====================================================================
        // WPF Key handling (SceneView forwards WPF KeyDown/KeyUp here)
        // =====================================================================

        public void OnKeyDown(System.Windows.Input.Key key)
        {
            switch (key)
            {
                case System.Windows.Input.Key.W: _moveForward = true; break;
                case System.Windows.Input.Key.S: _moveBack = true; break;
                case System.Windows.Input.Key.A: _moveLeft = true; break;
                case System.Windows.Input.Key.D: _moveRight = true; break;
                case System.Windows.Input.Key.E:
                case System.Windows.Input.Key.Space:
                    _moveUp = true;
                    break;
                case System.Windows.Input.Key.Q:
                case System.Windows.Input.Key.LeftCtrl:
                    _moveDown = true;
                    break;
            }
        }

        public void OnKeyUp(System.Windows.Input.Key key)
        {
            switch (key)
            {
                case System.Windows.Input.Key.W: _moveForward = false; break;
                case System.Windows.Input.Key.S: _moveBack = false; break;
                case System.Windows.Input.Key.A: _moveLeft = false; break;
                case System.Windows.Input.Key.D: _moveRight = false; break;
                case System.Windows.Input.Key.E:
                case System.Windows.Input.Key.Space:
                    _moveUp = false;
                    break;
                case System.Windows.Input.Key.Q:
                case System.Windows.Input.Key.LeftCtrl:
                    _moveDown = false;
                    break;
            }
        }
    }
}