
using System;
using Microsoft.Xna.Framework;
using GE2D3D.MapEditor.Runtime.Components;
using GE2D3D.MapEditor.Data;
using GE2D3D.MapEditor.Components.Camera;

namespace GE2D3D.MapEditor.Runtime.Systems
{
    /// <summary>
    /// High-level coordinator for all runtime systems.
    /// </summary>
    public class RuntimeEngine
    {
        public RuntimeScene Scene { get; private set; }

        private readonly PhysicsSystem _physicsSystem;
        private readonly PlayerControllerSystem _playerSystem;
        private readonly CameraSystem _cameraSystem;
        private readonly LightSystem _lightSystem;
        private readonly ScriptSystem _scriptSystem;
        private readonly DebugOverlaySystem _debugSystem;
        private readonly AnimationSystem _animationSystem;
        private readonly AiSystem _aiSystem;
        private readonly AudioSystem _audioSystem;
        private readonly UiSystem _uiSystem;
        private readonly TimelineSystem _timelineSystem;

        /// <summary>
        /// Construct a runtime engine from a level and an editor camera.
        /// </summary>
        public RuntimeEngine(LevelInfo levelInfo, BaseCamera camera)
        {
            if (levelInfo == null) throw new ArgumentNullException(nameof(levelInfo));
            if (camera == null) throw new ArgumentNullException(nameof(camera));

            Scene = RuntimeSceneBuilder.FromLevel(levelInfo);

            _physicsSystem = new PhysicsSystem();
            _playerSystem = new PlayerControllerSystem();
            _cameraSystem = new CameraSystem(camera);
            _lightSystem = new LightSystem();
            _scriptSystem = new ScriptSystem();
            _debugSystem = new DebugOverlaySystem();
            _animationSystem = new AnimationSystem();
            _aiSystem = new AiSystem();
            _audioSystem = new AudioSystem();
            _uiSystem = new UiSystem();
            _timelineSystem = new TimelineSystem();
        }

        /// <summary>
        /// Main update entry point for the runtime engine.
        /// </summary>
        public void Update(GameTime gameTime, RuntimeInputSnapshot input)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (dt <= 0f) dt = 1f / 60f;

            // Script step (once Track M is active, this will execute behaviour)
            _scriptSystem.Update(Scene, dt);

            // Player and input-driven motion
            _playerSystem.Update(Scene, input, dt);

            // Physics (collisions & triggers)
            _physicsSystem.Update(Scene, dt, _scriptSystem);

            // AI behaviours
            _aiSystem.Update(Scene, dt);

            // Animation state
            _animationSystem.Update(Scene, dt);

            // Camera follow / free camera
            _cameraSystem.Update(Scene, dt);

            // Lighting view (for now this just computes some aggregates)
            _lightSystem.Update(Scene, dt);

            // UI and audio
            _uiSystem.Update(Scene, dt);
            _audioSystem.Update(Scene, dt);

            // Timeline / cutscene playback
            _timelineSystem.Update(Scene, dt);

            // Debug overlay information
            _debugSystem.Update(Scene, input, dt);
        }
    }
}
