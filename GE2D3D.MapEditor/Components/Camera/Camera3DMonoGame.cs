#nullable enable
using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GE2D3D.MapEditor.Components.Camera
{
    /// <summary>
    /// 3D editor-style camera:
    /// - WASD / arrows: move
    /// - Q / Space: up
    /// - Z / Ctrl: down
    /// - Mouse update is handled by BaseCamera (RMB orbit, etc.).
    ///
    /// Works in:
    /// - Classic MonoGame (Game-based ctor)
    /// - WPF host (GraphicsDevice-based ctor; Game is null).
    /// </summary>
    public class Camera3DMonoGame : BaseCamera, IUpdateable
    {
        private bool _enabled = true;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    OnEnabledChanged(this, EventArgs.Empty);
                }
            }
        }

        private int _updateOrder;
        public int UpdateOrder
        {
            get => _updateOrder;
            set
            {
                if (_updateOrder != value)
                {
                    _updateOrder = value;
                    OnUpdateOrderChanged(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler<EventArgs>? EnabledChanged;
        public event EventHandler<EventArgs>? UpdateOrderChanged;

        /// <summary>
        /// Optional Game reference (available in full MonoGame, null in WPF host).
        /// </summary>
        private Game? Game { get; }

        private KeyboardState _currentKeyboardState = Keyboard.GetState();

        // --- CTOR 1: original Game-based ctor (for classic MonoGame usage) ---
        public Camera3DMonoGame(Game game)
            : base(game?.GraphicsDevice ?? throw new ArgumentNullException(nameof(game)))
        {
            Game = game;
        }

        // --- CTOR 2: GraphicsDevice-based ctor (for WPF / editor host) ---
        public Camera3DMonoGame(GraphicsDevice graphicsDevice)
            : base(graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice)))
        {
            // No Game here; mouse cursor visibility will be a no-op.
        }

        public override void Initialize()
        {
            // Ensure states are initialized before first Update()
            _currentKeyboardState = Keyboard.GetState();
            _currentMouseState = Mouse.GetState();
        }

        public void Update(GameTime gameTime)
        {
            if (!Enabled)
                return;

            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds * 2.5f;

            UpdateMouse(Mouse.GetState());
            UpdateKeyboard(Keyboard.GetState(), dt);
        }

        private void UpdateKeyboard(KeyboardState keyboardState, float elapsed)
        {
            _currentKeyboardState = keyboardState;

            _velocity = _currentKeyboardState.IsKeyDown(Keys.LeftShift)
                ? VelocityFast
                : VelocityStandard;

            var direction = Vector3.Zero;

            if (_currentKeyboardState.IsKeyDown(Keys.W) || _currentKeyboardState.IsKeyDown(Keys.Up))
            {
                if (!_forwardsPressed)
                {
                    _forwardsPressed = true;
                    _currentVelocity.Z = 0.0f;
                }

                direction.Z += 1.0f;
            }
            else
            {
                _forwardsPressed = false;
            }

            if (_currentKeyboardState.IsKeyDown(Keys.S) || _currentKeyboardState.IsKeyDown(Keys.Down))
            {
                if (!_backwardsPressed)
                {
                    _backwardsPressed = true;
                    _currentVelocity.Z = 0.0f;
                }

                direction.Z -= 1.0f;
            }
            else
            {
                _backwardsPressed = false;
            }

            if (_currentKeyboardState.IsKeyDown(Keys.D) || _currentKeyboardState.IsKeyDown(Keys.Right))
            {
                if (!_strafeRightPressed)
                {
                    _strafeRightPressed = true;
                    _currentVelocity.X = 0.0f;
                }

                direction.X += 1.0f;
            }
            else
            {
                _strafeRightPressed = false;
            }

            if (_currentKeyboardState.IsKeyDown(Keys.A) || _currentKeyboardState.IsKeyDown(Keys.Left))
            {
                if (!_strafeLeftPressed)
                {
                    _strafeLeftPressed = true;
                    _currentVelocity.X = 0.0f;
                }

                direction.X -= 1.0f;
            }
            else
            {
                _strafeLeftPressed = false;
            }

            if (_currentKeyboardState.IsKeyDown(Keys.Q) || _currentKeyboardState.IsKeyDown(Keys.Space))
            {
                if (!_lshiftPressed)
                {
                    _lshiftPressed = true;
                    _currentVelocity.Y = 0.0f;
                }

                direction.Y += 1.0f;
            }
            else
            {
                _lshiftPressed = false;
            }

            if (_currentKeyboardState.IsKeyDown(Keys.Z) || _currentKeyboardState.IsKeyDown(Keys.LeftControl))
            {
                if (!_spacePressed)
                {
                    _spacePressed = true;
                    _currentVelocity.Y = 0.0f;
                }

                direction.Y -= 1.0f;
            }
            else
            {
                _spacePressed = false;
            }

            UpdatePosition(ref direction, elapsed);
        }

        protected override void SetMousePosition(int x, int y)
        {
            // In a classic Game window this recenters the OS cursor.
            // In a WPF host, this will still move the OS cursor on screen,
            // which is acceptable for now; if it feels bad we can later
            // override BaseCamera to use relative mouse input instead.
            Mouse.SetPosition(x, y);
        }

        protected override void SetMouseCursorVisible(bool visible)
        {
            // In full MonoGame we toggle the Game's mouse visibility.
            // In the editor host Game will be null, so just no-op.
            if (Game is not null)
                Game.IsMouseVisible = visible;
        }

        protected override Point GetScreenCenter()
        {
            return new Point(
                GraphicsDevice.Viewport.Width / 2,
                GraphicsDevice.Viewport.Height / 2);
        }

        protected virtual void OnUpdateOrderChanged(object sender, EventArgs args)
            => UpdateOrderChanged?.Invoke(sender, args);

        protected virtual void OnEnabledChanged(object sender, EventArgs args)
            => EnabledChanged?.Invoke(sender, args);

        // --------------------------------------------------------------------
        // Editor helpers: presets + focus + zoom
        // --------------------------------------------------------------------

        /// <summary>
        /// Look straight down at the specified center point.
        /// </summary>
        public void LookFromTop(Vector3 center)
        {
            Position = center + new Vector3(0f, 50f, 0f);
            Target = center;
        }

        /// <summary>
        /// Look at the origin from above.
        /// </summary>
        public void LookFromTop()
        {
            LookFromTop(Vector3.Zero);
        }

        /// <summary>
        /// Look at the specified center from the "front" (positive Z).
        /// </summary>
        public void LookFromFront(Vector3 center)
        {
            Position = center + new Vector3(0f, 10f, 50f);
            Target = center;
        }

        public void LookFromFront()
        {
            LookFromFront(Vector3.Zero);
        }

        /// <summary>
        /// Look at the specified center from the "side" (positive X).
        /// </summary>
        public void LookFromSide(Vector3 center)
        {
            Position = center + new Vector3(50f, 10f, 0f);
            Target = center;
        }

        public void LookFromSide()
        {
            LookFromSide(Vector3.Zero);
        }

        /// <summary>
        /// Zoom towards or away from the current Target.
        /// Positive moves closer, negative moves away.
        /// </summary>
        public void Zoom(float amount)
        {
            var dir = Target - Position;
            if (dir == Vector3.Zero)
                return;

            dir.Normalize();
            Position += dir * amount;
        }

        /// <summary>
        /// Focus on a point with a given distance in front of it.
        /// </summary>
        public void FocusOn(Vector3 center, float distance)
        {
            if (distance < 0.1f)
                distance = 0.1f;

            // Keep current forward direction if possible,
            // fallback to a default forward.
            var forward = Forward;
            if (forward == Vector3.Zero)
                forward = Vector3.Forward;

            forward.Normalize();
            Position = center - forward * distance;
            Target = center;
        }
    }
}