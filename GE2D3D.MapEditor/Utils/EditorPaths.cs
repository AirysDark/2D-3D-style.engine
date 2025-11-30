using System;
using System.IO;
using System.Reflection;

namespace GE2D3D.MapEditor.Utils
{
    public static class EditorPaths
    {
        // Optional override from "Set Game Root..." menu
        private static string _gameRootOverride;

        /// <summary>
        /// Allow the user to override the detected P3D game root.
        /// Expects the folder that contains Content\ (i.e. ...\P3D).
        /// </summary>
        public static void SetGameRoot(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                _gameRootOverride = path;
            }
        }

        /// <summary>
        /// Returns the guessed P3D game root folder (where Content/ lives).
        /// You can hardcode your install path here if you want.
        /// </summary>
        public static string GetGameRoot()
        {
            // 0) If user picked a root via "Set Game Root...", use that
            if (!string.IsNullOrWhiteSpace(_gameRootOverride) &&
                Directory.Exists(_gameRootOverride))
            {
                return _gameRootOverride;
            }

            // 1) Hardcoded override: CHANGE THIS TO YOUR REAL GAME PATH IF YOU WANT
            var hardcoded = @"C:\p3d\P3D-Legacy\P3D";
            if (Directory.Exists(hardcoded))
                return hardcoded;

            // 2) Try relative to the editor EXE: ..\P3D
            var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var candidate = Path.GetFullPath(Path.Combine(exeDir, @"..\P3D"));
            if (Directory.Exists(candidate))
                return candidate;

            // 3) Fallback: just use the EXE directory
            return exeDir;
        }

        /// <summary>
        /// Root of the P3D Content/ folder (P3D\Content).
        /// </summary>
        public static string GetContentRoot()
        {
            var root = GetGameRoot();
            var content = Path.Combine(root, "Content");
            return Directory.Exists(content) ? content : root;
        }

        /// <summary>
        /// For legacy code that expects a static ContentRoot property.
        /// </summary>
        public static string ContentRoot => GetContentRoot();

        /// <summary>
        /// Default Maps folder (P3D\Content\Maps).
        /// </summary>
        public static string GetMapsFolder()
        {
            var content = GetContentRoot();
            var maps = Path.Combine(content, "Maps");
            return Directory.Exists(maps) ? maps : content;
        }
    }
}