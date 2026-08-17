using System;
using GE2D3D.MapEditor.Components.Camera;
using GE2D3D.MapEditor.Data;
using GE2D3D.MapEditor.Runtime.Systems;
using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Runtime
{
    /// <summary>
    /// Controls in-editor Play/Stop state.
    /// The editor remains alive while a separate runtime scene is created from
    /// the current LevelInfo, so runtime state can be discarded on Stop without
    /// modifying the editor's source scene.
    /// </summary>
    public sealed class PlayModeController : IDisposable
    {
        private RuntimeEngine? _runtimeEngine;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;
        public RuntimeEngine? RuntimeEngine => _runtimeEngine;
        public RuntimeScene? RuntimeScene => _runtimeEngine?.Scene;

        public event EventHandler<bool>? PlayStateChanged;

        public void Start(LevelInfo levelInfo, BaseCamera camera)
        {
            if (levelInfo == null) throw new ArgumentNullException(nameof(levelInfo));
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (_isPlaying) return;

            _runtimeEngine = new RuntimeEngine(levelInfo, camera);
            _isPlaying = true;
            PlayStateChanged?.Invoke(this, true);
        }

        public void Update(GameTime gameTime, RuntimeInputSnapshot input)
        {
            if (!_isPlaying || _runtimeEngine == null) return;
            _runtimeEngine.Update(gameTime, input);
        }

        public void Stop()
        {
            if (!_isPlaying) return;

            _runtimeEngine = null;
            _isPlaying = false;
            PlayStateChanged?.Invoke(this, false);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
