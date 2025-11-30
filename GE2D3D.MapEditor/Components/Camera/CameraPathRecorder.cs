using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Components.Camera
{
    public class CameraKeyframe
    {
        public Vector3 Position;
        public Vector3 Target;
        public float Time; // seconds along path
    }

    public class CameraPathRecorder
    {
        private readonly BaseCamera _camera;
        private readonly List<CameraKeyframe> _keyframes = new List<CameraKeyframe>();

        private bool _isRecording;
        private bool _isPlaying;
        private float _playTime;

        public IReadOnlyList<CameraKeyframe> Keyframes => _keyframes;

        public bool IsRecording => _isRecording;

        public bool IsPlaying => _isPlaying;

        public float Duration => _keyframes.Count > 0 ? _keyframes[_keyframes.Count - 1].Time : 0f;


        public CameraPathRecorder(BaseCamera camera)
        {
            _camera = camera;
        }

        public void Clear()
        {
            _keyframes.Clear();
            _isRecording = false;
            _isPlaying = false;
            _playTime = 0f;
        }

        public void SetKeyframes(IEnumerable<CameraKeyframe> frames)
        {
            _keyframes.Clear();

            if (frames != null)
            {
                foreach (var f in frames)
                {
                    _keyframes.Add(new CameraKeyframe
                    {
                        Position = f.Position,
                        Target = f.Target,
                        Time = f.Time
                    });
                }
            }

            _isRecording = false;
            _isPlaying = false;
            _playTime = 0f;
        }


        public void ToggleRecording()
        {
            if (_isRecording)
            {
                _isRecording = false;
            }
            else
            {
                _keyframes.Clear();
                _isRecording = true;
                _playTime = 0f;
                AddKeyframe(0f);
            }
        }

        public void StartPlayback()
        {
            if (_keyframes.Count < 2)
                return;

            _isRecording = false;
            _isPlaying = true;
            _playTime = 0f;
        }

        public void Update(GameTime gameTime)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_isRecording)
            {
                if (_keyframes.Count == 0)
                    AddKeyframe(0f);

                _playTime += dt;
                // sample every 0.5s
                if (_playTime >= (_keyframes[_keyframes.Count - 1].Time + 0.5f))
                    AddKeyframe(_playTime);
            }

            if (_isPlaying)
            {
                _playTime += dt;
                ApplyAtTime(_playTime);

                if (_playTime > _keyframes[_keyframes.Count - 1].Time)
                    _isPlaying = false;
            }
        }

        private void AddKeyframe(float t)
        {
            _keyframes.Add(new CameraKeyframe
            {
                Time = t,
                Position = _camera.Position,
                Target = _camera.Target
            });
        }

        public void ApplyAtTime(float t)
        {
            if (_keyframes.Count == 0)
                return;

            if (t <= _keyframes[0].Time)
            {
                _camera.Position = _keyframes[0].Position;
                _camera.Target = _keyframes[0].Target;
                return;
            }

            if (t >= _keyframes[_keyframes.Count - 1].Time)
            {
                var last = _keyframes[_keyframes.Count - 1];
                _camera.Position = last.Position;
                _camera.Target = last.Target;
                return;
            }

            CameraKeyframe k0 = _keyframes[0], k1 = _keyframes[0];
            for (int i = 0; i < _keyframes.Count - 1; i++)
            {
                if (t >= _keyframes[i].Time && t <= _keyframes[i + 1].Time)
                {
                    k0 = _keyframes[i];
                    k1 = _keyframes[i + 1];
                    break;
                }
            }

            var span = k1.Time - k0.Time;
            var alpha = span > 0 ? (t - k0.Time) / span : 0f;
            _camera.Position = Vector3.Lerp(k0.Position, k1.Position, alpha);
            _camera.Target = Vector3.Lerp(k0.Target, k1.Target, alpha);
        }
    }
}