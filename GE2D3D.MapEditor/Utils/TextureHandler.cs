using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GE2D3D.MapEditor.Data;
// NOTE: same namespace, so no need to "using GE2D3D.MapEditor.Utils;" for EditorPaths

namespace GE2D3D.MapEditor.Utils
{
    public static class TextureHandler
    {
        // --------------------------------------------------------------------
        // Cropped texture cache
        // --------------------------------------------------------------------
        private static Dictionary<KeyValuePair<string, Rectangle>, KeyValuePair<Texture2D, bool>> CroppedTextures { get; }
            = new Dictionary<KeyValuePair<string, Rectangle>, KeyValuePair<Texture2D, bool>>();

        public static KeyValuePair<Texture2D, bool> GetCroppedTexture(string texturePath, Rectangle textureRectangle) =>
            GetCroppedTexture(new KeyValuePair<string, Rectangle>(texturePath, textureRectangle));

        public static KeyValuePair<Texture2D, bool> GetCroppedTexture(KeyValuePair<string, Rectangle> key) =>
            !CroppedTextures.ContainsKey(key) ? default(KeyValuePair<Texture2D, bool>) : CroppedTextures[key];

        public static bool HasCroppedTexture(string texturePath, Rectangle textureRectangle) =>
            HasCroppedTexture(new KeyValuePair<string, Rectangle>(texturePath, textureRectangle));

        public static bool HasCroppedTexture(KeyValuePair<string, Rectangle> key) =>
            CroppedTextures.ContainsKey(key);

        public static void AddCroppedTexture(string texturePath, Rectangle textureRectangle, KeyValuePair<Texture2D, bool> value) =>
            AddCroppedTexture(new KeyValuePair<string, Rectangle>(texturePath, textureRectangle), value);

        public static void AddCroppedTexture(KeyValuePair<string, Rectangle> key, KeyValuePair<Texture2D, bool> value) =>
            CroppedTextures.Add(key, value);

        public static KeyValuePair<Texture2D, bool> CropTexture(Texture2D texture, Rectangle rectangle)
        {
            var pixels = new Color[rectangle.Width * rectangle.Height];
            texture.GetData(0, rectangle, pixels, 0, rectangle.Width * rectangle.Height);

            var hasTransparent = false;
            for (var i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].A < 255)
                {
                    hasTransparent = true;
                    break;
                }
            }

            var newTex = new Texture2D(texture.GraphicsDevice, rectangle.Width, rectangle.Height);
            newTex.SetData(pixels);

            return new KeyValuePair<Texture2D, bool>(newTex, hasTransparent);
        }

        // --------------------------------------------------------------------
        // Texture loading + caching
        // --------------------------------------------------------------------

        private static Dictionary<string, Texture2D> LoadedTextures { get; } =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Load texture for an EntityInfo.
        ///
        /// Priority:
        ///   1) If TexturePath is absolute, use as-is.
        ///   2) If Parent.TexturesLocation is set, use that as base.
        ///   3) Else if Parent.DirectoryLocation is set, use "<DirectoryLocation>\textures".
        ///   4) Else fall back to "<ContentRoot>\Textures".
        ///
        /// Texture file gets ".png" appended if no extension.
        /// </summary>
        public static Texture2D LoadTexture(GraphicsDevice graphicsDevice, EntityInfo entity)
        {
            if (graphicsDevice == null)
                throw new ArgumentNullException(nameof(graphicsDevice));
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var cacheKey = entity.TexturePath ?? string.Empty;

            if (LoadedTextures.TryGetValue(cacheKey, out var cached) && !cached.IsDisposed)
                return cached;

            if (string.IsNullOrWhiteSpace(entity.TexturePath))
            {
                Debug.WriteLine("[TextureHandler] entity with empty TexturePath ? using dummy texture.");
                var dummyEmpty = CreateDummyTexture(graphicsDevice, Color.Magenta);
                LoadedTextures[cacheKey] = dummyEmpty;
                return dummyEmpty;
            }

            var levelInfo = entity.Parent;
            string texName = entity.TexturePath;

            // ensure extension
            if (!Path.HasExtension(texName))
                texName += ".png";

            string primaryPath;

            if (Path.IsPathRooted(texName))
            {
                // They gave us an absolute path; trust it
                primaryPath = texName;
            }
            else
            {
                // Resolve a base directory:
                // 1) explicit TexturesLocation
                string? baseDir = null;

                if (levelInfo != null &&
                    !string.IsNullOrEmpty(levelInfo.TexturesLocation))
                {
                    baseDir = levelInfo.TexturesLocation;
                }
                else if (levelInfo != null &&
                         !string.IsNullOrEmpty(levelInfo.DirectoryLocation))
                {
                    // 2) map folder + "textures"
                    baseDir = Path.Combine(levelInfo.DirectoryLocation, "textures");
                }
                else
                {
                    // 3) global game Content\Textures
                    baseDir = Path.Combine(EditorPaths.ContentRoot, "Textures");
                }

                // baseDir is guaranteed non-null by the if/else-if/else above,
                // but we still null-coalesce to keep the compiler happy.
                primaryPath = Path.Combine(baseDir ?? EditorPaths.ContentRoot, texName);
            }

            return LoadTextureInternal(graphicsDevice, cacheKey, primaryPath, entity.TexturePath);
        }

        /// <summary>
        /// Load texture from an arbitrary path (used by daycycle, backdrops, etc).
        ///
        /// If path is absolute: use as-is.
        /// If path is relative: resolve under EditorPaths.ContentRoot.
        /// If no extension: ".png" is appended.
        /// </summary>
        public static Texture2D LoadTexture(GraphicsDevice graphicsDevice, string path)
        {
            if (graphicsDevice == null)
                throw new ArgumentNullException(nameof(graphicsDevice));

            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.WriteLine("[TextureHandler] empty path requested ? using dummy texture.");
                return CreateDummyTexture(graphicsDevice, Color.Magenta);
            }

            var cacheKey = path;

            if (LoadedTextures.TryGetValue(cacheKey, out var cached) && !cached.IsDisposed)
                return cached;

            string primaryPath;

            if (Path.IsPathRooted(path))
            {
                primaryPath = path;
            }
            else
            {
                var trimmed = path.TrimStart('\\', '/');

                if (!Path.HasExtension(trimmed))
                    trimmed += ".png";

                // Resolve relative to auto-detected Content root
                primaryPath = Path.Combine(EditorPaths.ContentRoot, trimmed);
            }

            return LoadTextureInternal(graphicsDevice, cacheKey, primaryPath, path);
        }

        /// <summary>
        /// Shared loader for both entity-based and string-based overloads.
        /// Just validates primaryPath and returns a dummy if it fails.
        /// </summary>
        private static Texture2D LoadTextureInternal(
            GraphicsDevice graphicsDevice,
            string cacheKey,
            string primaryPath,
            string labelForLogs)
        {
            string fullPath = primaryPath;

            try
            {
                fullPath = Path.GetFullPath(primaryPath);
            }
            catch
            {
                // keep as-is if GetFullPath fails
            }

            if (!File.Exists(fullPath))
            {
                Debug.WriteLine(
                    $"[TextureHandler] file not found for '{labelForLogs}' at '{fullPath}'. Using dummy.");
                var dummyMissing = CreateDummyTexture(graphicsDevice, Color.HotPink);
                LoadedTextures[cacheKey] = dummyMissing;
                return dummyMissing;
            }

            try
            {
                Texture2D texture;
                using (var stream = File.OpenRead(fullPath))
                {
                    texture = Texture2D.FromStream(graphicsDevice, stream);
                }

                // Replace magenta with transparent
                MakeMagentaTransparent(texture);

                LoadedTextures[cacheKey] = texture;
                Debug.WriteLine($"[TextureHandler] loaded texture '{labelForLogs}' from '{fullPath}'.");
                return texture;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[TextureHandler] FAILED to load texture '{labelForLogs}' from '{fullPath}': {ex.Message}. Using dummy.");
                var dummyError = CreateDummyTexture(graphicsDevice, Color.Red);
                LoadedTextures[cacheKey] = dummyError;
                return dummyError;
            }
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------

        private static void MakeMagentaTransparent(Texture2D texture)
        {
            if (texture == null)
                return;

            var pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);

            for (var i = 0; i < pixels.Length; i++)
            {
                if (pixels[i] == Color.Magenta)
                {
                    // MonoGame has Color.Transparent, not TransparentBlack
                    pixels[i] = Color.Transparent; // or new Color(0, 0, 0, 0);
                }
            }

            texture.SetData(pixels);
        }

        private static Texture2D CreateDummyTexture(GraphicsDevice device, Color color)
        {
            var tex = new Texture2D(device, 4, 4);
            var data = new Color[4 * 4];
            for (int i = 0; i < data.Length; i++)
                data[i] = color;
            tex.SetData(data);
            return tex;
        }

        public static void Dispose()
        {
            foreach (var pair in CroppedTextures)
                pair.Value.Key.Dispose();
            CroppedTextures.Clear();

            foreach (var pair in LoadedTextures)
                pair.Value.Dispose();
            LoadedTextures.Clear();
        }
    }
}