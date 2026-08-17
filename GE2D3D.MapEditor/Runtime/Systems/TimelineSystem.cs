using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GE2D3D.MapEditor.Runtime.Components;

namespace GE2D3D.MapEditor.Runtime.Systems
{
    public enum TimelineState
    {
        Stopped,
        Playing
    }

    public class TimelineKeyframe
    {
        public float Time;
        public int? EntityId;
        public Vector3? Position;
    }

    /// <summary>
    /// Very simple timeline system that can move entities along keyframed positions over time.
    /// This is intentionally minimal but provides a concrete implementation for cutscene-like sequences.
    /// </summary>
    public class TimelineSystem
    {
        public TimelineState State { get; private set; } = TimelineState.Stopped;
        public float CurrentTime { get; private set; }

        private readonly List<TimelineKeyframe> _keyframes = new();

        public void Clear()
        {
            _keyframes.Clear();
            CurrentTime = 0f;
            State = TimelineState.Stopped;
        }

        public void AddKeyframe(TimelineKeyframe kf)
        {
            _keyframes.Add(kf);
            _keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        public void Play()
        {
            if (_keyframes.Count == 0)
                return;

            CurrentTime = 0f;
            State = TimelineState.Playing;
        }

        public void Stop()
        {
            State = TimelineState.Stopped;
        }

        public void Update(RuntimeScene scene, float dt)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            if (State != TimelineState.Playing)
                return;

            CurrentTime += dt;

            foreach (var kf in _keyframes)
            {
                if (!kf.Position.HasValue || !kf.EntityId.HasValue)
                    continue;

                if (Math.Abs(kf.Time - CurrentTime) < 0.05f)
                {
                    var entity = scene.FindById(kf.EntityId.Value);
                    if (entity != null && entity.TryGetComponent(out TransformComponent transform))
                    {
                        transform.Position = kf.Position.Value;
                    }
                }
            }

            // Stop when we pass the last keyframe.
            if (_keyframes.Count > 0 && CurrentTime > _keyframes[^1].Time)
            {
                State = TimelineState.Stopped;
            }
        }
    }
}
