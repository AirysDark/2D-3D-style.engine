using System;
using System.IO;

namespace GE2D3D.MapEditor.Utils
{
    public static class EditorPaths
    {
        // Manual override set through the editor UI ("Set Project Root...")
        private static string _projectRootOverride;

        /// <summary>
        /// Set the project root explicitly.
        /// This folder MUST contain the Content/ folder.
        /// </summary>
        public static void SetProjectRoot(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                _projectRootOverride = path;
            }
        }

        /// <summary>
        /// Backwards-compatible alias for older code paths. 
        /// "Game root" and "project root" are the same concept here.
        /// </summary>
        public static void SetGameRoot(string path)
        {
            SetProjectRoot(path);
        }

        /// <summary>
        /// Returns the detected Project Root folder.
        /// Priority:
        /// 1) User override (SetProjectRoot / SetGameRoot)
        /// 2) Auto-detect a folder containing a Content/ subfolder, starting from EXE and walking upwards
        /// 3) Fallback = EXE directory (no more Documents\MapEditor)
        /// </summary>
        public static string GetProjectRoot()
        {
            // 1) User override
            if (!string.IsNullOrWhiteSpace(_projectRootOverride) &&
                Directory.Exists(_projectRootOverride))
            {
                return _projectRootOverride;
            }

            // 2) Try to auto-detect from the EXE location upwards
            try
            {
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                var dir = exeDir;

                while (!string.IsNullOrEmpty(dir))
                {
                    var contentCandidate = Path.Combine(dir, "Content");
                    if (Directory.Exists(contentCandidate))
                    {
                        // Found a folder with a Content/ subfolder -> treat that as project root
                        return dir;
                    }

                    dir = Path.GetDirectoryName(dir);
                }

                // 3) Fallback: use EXE directory as project root (no Documents)
                return exeDir.TrimEnd('\\', '/');
            }
            catch
            {
                // Absolute worst-case fallback: EXE base directory
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                return exeDir.TrimEnd('\\', '/');
            }
        }

        /// <summary>
        /// Returns the Content/ folder under the project root.
        /// </summary>
        public static string GetContentRoot()
        {
            var root = GetProjectRoot();
            var content = Path.Combine(root, "Content");

            // Ensure Content exists
            if (!Directory.Exists(content))
                Directory.CreateDirectory(content);

            return content;
        }

        public static string ContentRoot => GetContentRoot();

        /// <summary>
        /// Returns Content/Data/maps/
        /// </summary>
        public static string GetMapsFolder()
        {
            var content = GetContentRoot();
            var maps = Path.Combine(content, "Data", "maps");

            // Ensure directory exists
            if (!Directory.Exists(maps))
                Directory.CreateDirectory(maps);

            return maps;
        }

        public static string GetTexturesFolder()
        {
            var folder = Path.Combine(GetContentRoot(), "Textures");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        public static string GetBackdropsFolder()
        {
            var folder = Path.Combine(GetContentRoot(), "Data", "Backdrops");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        public static string GetSongsFolder()
        {
            var folder = Path.Combine(GetContentRoot(), "Songs");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        /// <summary>
        /// Returns Content/Textures/NPC/
        /// </summary>
        public static string GetNpcFolder()
        {
            // NPC textures live under Textures/NPC
            var folder = Path.Combine(GetTexturesFolder(), "NPC");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        public static string GetEffectsFolder()
        {
            var folder = Path.Combine(GetContentRoot(), "Effects");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        public static string GetSkyDomeFolder()
        {
            var folder = Path.Combine(GetContentRoot(), "SkyDomeResource");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }
    }
}