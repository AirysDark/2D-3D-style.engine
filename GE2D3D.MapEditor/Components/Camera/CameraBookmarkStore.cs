using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Components.Camera
{
    public class CameraBookmark
    {
        public Vector3 Position { get; set; }
        public Vector3 Target { get; set; }
    }

    /// <summary>
    /// JSON-backed camera bookmark store for slots 1..9.
    /// </summary>
    public class CameraBookmarkStore
    {
        private readonly BaseCamera _camera;
        private readonly Dictionary<int, CameraBookmark> _bookmarks = new();
        private readonly string _filePath;

        public CameraBookmarkStore(BaseCamera camera, string filePath)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

            LoadFromDisk();
        }

        public void SaveBookmark(int slot)
        {
            if (slot < 1 || slot > 9)
                throw new ArgumentOutOfRangeException(nameof(slot), "Bookmark slot must be between 1 and 9.");

            _bookmarks[slot] = new CameraBookmark
            {
                Position = _camera.Position,
                Target = _camera.Target
            };

            SaveToDisk();
        }

        public bool TryLoadBookmark(int slot, out CameraBookmark? bookmark)
        {
            if (_bookmarks.TryGetValue(slot, out var b))
            {
                bookmark = b;
                return true;
            }

            bookmark = null;
            return false;
        }

        public void ApplyBookmark(int slot)
        {
            if (TryLoadBookmark(slot, out var b) && b != null)
            {
                _camera.Position = b.Position;
                _camera.Target = b.Target;
            }
        }

        private void SaveToDisk()
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                File.WriteAllText(_filePath, JsonSerializer.Serialize(_bookmarks, options));
            }
            catch
            {
                // best-effort only
            }
        }

        private void LoadFromDisk()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return;

                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<Dictionary<int, CameraBookmark>>(json);
                if (loaded == null)
                    return;

                _bookmarks.Clear();
                foreach (var kvp in loaded)
                    _bookmarks[kvp.Key] = kvp.Value;
            }
            catch
            {
                // ignore corrupted bookmark file
            }
        }
    }
}
