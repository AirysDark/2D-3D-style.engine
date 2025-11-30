using System;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using GE2D3D.MapEditor.Renders;
using GE2D3D.MapEditor.Components.Input;
using GE2D3D.MapEditor.Data;

namespace RenderTest
{
    /// <summary>
    /// Standalone MonoGame host to test GE2D3D rendering, camera controls, and gizmos.
    /// </summary>
    public class Game1 : Game
    {
        public static Point DefaultResolution => new Point(800, 600);

        private GraphicsDeviceManager Graphics { get; }

        // Shared pipeline: camera + selector + render + debug + camera controller
        private RenderBootstrap? _bootstrap;

        // For wheel delta
        private int _previousScrollWheelValue;

        // For now we don't have selection logic in RenderTest; keep it null.
        private EntityInfo? _currentSelection;

        public Game1()
        {
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            Graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = false;

            Graphics.PreferredBackBufferWidth = 1440;
            Graphics.PreferredBackBufferHeight = 900;
            Graphics.ApplyChanges();

            //Window.AllowUserResizing = true;
            //Window.ClientSizeChanged += OnResize;
        }

        public void OnResize(object? sender, EventArgs e)
        {
            if (Graphics.GraphicsDevice.Viewport.Width < DefaultResolution.X ||
                Graphics.GraphicsDevice.Viewport.Height < DefaultResolution.Y)
            {
                Resize(DefaultResolution);
                return;
            }
        }

        public void Resize(Point size)
        {
            if (size.X < DefaultResolution.X || size.Y < DefaultResolution.Y)
                return;

            Graphics.PreferredBackBufferWidth = size.X;
            Graphics.PreferredBackBufferHeight = size.Y;

            Graphics.ApplyChanges();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// </summary>
        protected override void Initialize()
        {
            Graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            Graphics.ApplyChanges();

            // Pick whichever map you want to test:
            var path = @"C:\GitHub\Maps\Goldenrod\goldenrod.dat";
            //var path = @"C:\GitHub\Maps\YourRoom\yourroom.dat";
            //var path = @"C:\GitHub\Maps\UnderwaterCave\main.dat";
            //var path = @"C:\GitHub\Maps\Kolben\devoffices.dat";

            if (!File.Exists(path))
                throw new FileNotFoundException("Test map not found", path);

            // Build the whole render pipeline (camera, selector, render, debug, camera controller)
            _bootstrap = RenderBootstrap.FromMapPath(this, path);

            // Initialize scroll baseline
            _previousScrollWheelValue = Mouse.GetState().ScrollWheelValue;

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();
            // Use this.Content to load additional game content if you need it
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if (_bootstrap != null)
            {
                var kb = Keyboard.GetState();
                var mouse = Mouse.GetState();

                // Mouse wheel delta this frame
                var wheelDeltaRaw = mouse.ScrollWheelValue - _previousScrollWheelValue;
                _previousScrollWheelValue = mouse.ScrollWheelValue;
                var wheelDelta = wheelDeltaRaw / 120f; // typical 120 ticks per notch

                var input = new EditorInputSnapshot
                {
                    MousePosition = new Microsoft.Xna.Framework.Point(mouse.X, mouse.Y),
                    LeftButtonDown = mouse.LeftButton == ButtonState.Pressed,
                    RightButtonDown = mouse.RightButton == ButtonState.Pressed,
                    MouseWheelDelta = wheelDelta,

                    // Camera movement: WASD + QE up/down
                    KeyForward = kb.IsKeyDown(Keys.W),
                    KeyBackward = kb.IsKeyDown(Keys.S),
                    KeyLeft = kb.IsKeyDown(Keys.A),
                    KeyRight = kb.IsKeyDown(Keys.D),
                    KeyUp = kb.IsKeyDown(Keys.E),
                    KeyDown = kb.IsKeyDown(Keys.Q),

                    // Hold LeftAlt to temporarily disable snapping (or however you want to interpret it)
                    KeySnapToggle = kb.IsKeyDown(Keys.LeftAlt),

                    // Simple shortcuts for camera path recorder
                    KeyRecordCameraPath = kb.IsKeyDown(Keys.R),
                    KeyPlayCameraPath = kb.IsKeyDown(Keys.P)
                };

                // No selection logic in the test app yet, so pass _currentSelection (null)
                _bootstrap.UpdateEditor(input, _currentSelection, gameTime);
                _bootstrap.UpdateCameraPath(input, gameTime);
            }

            // Camera, controller, render, and debug text all update via Game.Components
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Render is an IDrawable component; base.Draw() will call it.
            base.Draw(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _bootstrap?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}