using System;
using System.Collections.Generic;

namespace GE2D3D.MapEditor.Runtime.Systems
{
    /// <summary>
    /// Simple logical audio routing system.
    /// Actual playback can be wired to MonoGame's SoundEffect/Song externally.
    /// </summary>
    public class AudioSystem
    {
        public readonly List<string> RecentlyRequestedSfx = new();
        public readonly List<string> RecentlyRequestedMusic = new();

        public void RequestSfx(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
                RecentlyRequestedSfx.Add(name);
        }

        public void RequestMusic(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
                RecentlyRequestedMusic.Add(name);
        }

        public void Update(RuntimeScene scene, float dt)
        {
            // For now this just clears the request lists once a frame.
            RecentlyRequestedSfx.Clear();
            RecentlyRequestedMusic.Clear();
        }
    }
}
