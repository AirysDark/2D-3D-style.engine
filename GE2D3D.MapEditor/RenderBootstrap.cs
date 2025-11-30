using GE2D3D.MapEditor.Components;
using GE2D3D.MapEditor.Components.Camera;
using GE2D3D.MapEditor.Components.Debug;
using GE2D3D.MapEditor.Components.Input;
using GE2D3D.MapEditor.Components.ModelSelector;
using GE2D3D.MapEditor.Components.Render;
using GE2D3D.MapEditor.Data;
using GE2D3D.MapEditor.Data.Models;
using GE2D3D.MapEditor.Utils;
using GE2D3D.MapEditor.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

// Alias so we clearly use the settings type from Components.Render
using EditorRenderSettings = GE2D3D.MapEditor.Components.Render.EditorRenderSettings;

namespace GE2D3D.MapEditor.Renders
{
    /// <summary>
    /// Logical layers the editor can toggle on/off from WPF UI.
    /// </summary>
    public enum SceneLayer
    {
        Grid,
        Collision,
        Props,
        Lights,
        Triggers
    }

    public sealed class RenderBootstrap : IDisposable
    {
        private readonly Game _game;
        private bool _disposed;

        public LevelInfo LevelInfo { get; private set; }
        public Level? Level { get; private set; }

        public BaseCamera Camera { get; }
        public BaseModelSelector ModelSelector { get; }
        public Render Render { get; }
        public DebugTextComponent DebugText { get; }
        public EditorCameraController CameraController { get; }
        public CameraPathRecorder CameraPathRecorder { get; }

        public EditorRenderSettings Settings => Render.EditorSettings;
        public LayerVisibility Layers => Render.Layers;

        
        // Environment / skybox state
        public bool SkyboxEnabled { get; private set; }

        /// <summary>
        /// Last ambient colour derived from the sky texture (0..1 per component).
        /// </summary>
        public Vector3 AmbientFromSky { get; private set; } = new Vector3(0.2f, 0.2f, 0.25f);

        private Texture2D? _innerSkyboxTexture;
        private Texture2D? _outerSkyboxTexture;

        private string? _innerSkyboxPath;
        private string? _outerSkyboxPath;

        public CameraBookmarkStore CameraBookmarks { get; }


        private RenderBootstrap(Game game, LevelInfo levelInfo)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            LevelInfo = levelInfo ?? throw new ArgumentNullException(nameof(levelInfo));

            // Camera
            Camera = new Camera3DMonoGame(_game);
            _game.Components.Add(Camera);

            // Camera bookmarks (persisted to local app data)
            var bookmarkPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GE2D3D.MapEditor",
                "camera_bookmarks.json");
            CameraBookmarks = new CameraBookmarkStore(Camera, bookmarkPath);

            // Selection
            ModelSelector = new ModelSelectorDefault(Camera);
            _game.Components.Add(ModelSelector);
            HookSelection();

            // Level + renderer
            Level = new Level(LevelInfo, _game.GraphicsDevice);

            Render = new Render(_game.GraphicsDevice, Camera, ModelSelector, LevelInfo);
            Render.ReloadLevel(Level);
            _game.Components.Add(Render);

            // Debug overlay
            DebugText = new DebugTextComponent(_game.GraphicsDevice, _game.Components);
            _game.Components.Add(DebugText);

            // Editor-style camera controls
            CameraController = new EditorCameraController(_game, Camera);
            _game.Components.Add(CameraController);

            // Camera paths (flythroughs)
            CameraPathRecorder = new CameraPathRecorder(Camera);
        }

        public static RenderBootstrap FromMapPath(Game game, string mapPath)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (string.IsNullOrWhiteSpace(mapPath))
                throw new ArgumentException("Map path cannot be empty.", nameof(mapPath));

            var text = File.ReadAllText(mapPath);
            var levelInfo = LevelLoader.Load(text, mapPath);
            return new RenderBootstrap(game, levelInfo);
        }

        public static RenderBootstrap FromLevelInfo(Game game, LevelInfo levelInfo)
        {
            return new RenderBootstrap(game, levelInfo);
        }

        public void Draw(GameTime gameTime)
        {
            Render.Draw(gameTime);
        }

        // -------------------------------------------------------------
        
        // -------------------------------------------------------------
        // Skybox / Environment
        // -------------------------------------------------------------
        public void SetSkyboxSettings(bool enabled, string? innerPath, string? outerPath)
        {
            SkyboxEnabled = enabled;

            _innerSkyboxPath = innerPath;
            _outerSkyboxPath = outerPath;

            // Sync with editor settings so render layer can query it.
            Settings.ShowSkybox = enabled;

            // Dispose any previous textures
            _innerSkyboxTexture?.Dispose();
            _outerSkyboxTexture?.Dispose();
            _innerSkyboxTexture = null;
            _outerSkyboxTexture = null;

            if (!enabled)
            {
                AmbientFromSky = new Vector3(0.2f, 0.2f, 0.25f);
                Render.SetSkyboxTextures(null, null, false);
                Render.SetAmbientFromSky(AmbientFromSky, 1.0f);
                return;
            }

            var gd = _game.GraphicsDevice;

            _innerSkyboxTexture = TryLoadSkyTexture(gd, innerPath);
            _outerSkyboxTexture = TryLoadSkyTexture(gd, outerPath);

            var ambient = ComputeAmbientFromTextureTopHalf(_innerSkyboxTexture);
            if (ambient.HasValue)
                AmbientFromSky = ambient.Value;

            Render.SetSkyboxTextures(_innerSkyboxTexture, _outerSkyboxTexture, enabled);
            Render.SetAmbientFromSky(AmbientFromSky, 1.0f);
        }

        private static Texture2D? TryLoadSkyTexture(GraphicsDevice gd, string? path)
        {
            if (gd == null)
                throw new ArgumentNullException(nameof(gd));

            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                return TextureHandler.LoadTexture(gd, path);
            }
            catch
            {
                // Skybox is optional; never crash here.
                return null;
            }
        }

        private static Vector3? ComputeAmbientFromTextureTopHalf(Texture2D? tex)
        {
            if (tex == null || tex.Width <= 0 || tex.Height <= 0)
                return null;

            int width = tex.Width;
            int height = tex.Height;

            int sampleHeight = height / 2;
            if (sampleHeight <= 0)
                sampleHeight = height;

            var rect = new Rectangle(0, 0, width, sampleHeight);
            var pixels = new Color[width * sampleHeight];

            tex.GetData(0, rect, pixels, 0, pixels.Length);

            Vector3 sum = Vector3.Zero;
            int count = pixels.Length;

            for (int i = 0; i < count; i++)
            {
                var v = pixels[i].ToVector3();
                sum += v;
            }

            if (count == 0)
                return null;

            var avg = sum / count;

            avg.X = MathHelper.Clamp(avg.X, 0f, 1f);
            avg.Y = MathHelper.Clamp(avg.Y, 0f, 1f);
            avg.Z = MathHelper.Clamp(avg.Z, 0f, 1f);

            return avg;
        }


        // Anti-aliasing
        // -------------------------------------------------------------
        public void SetAntiAliasing(AntiAliasing mode)
        {
            Render.SetAntiAliasing(mode);
            Settings.AntiAliasing = mode;
        }

        // -------------------------------------------------------------
        // Level reload
        // -------------------------------------------------------------
        public void ReloadLevel(LevelInfo levelInfo)
        {
            if (levelInfo == null) throw new ArgumentNullException(nameof(levelInfo));

            LevelInfo = levelInfo;

            Level = new Level(levelInfo, _game.GraphicsDevice);
            Render.ReloadLevel(Level);

            // Hook for map-based lighting if/when LevelInfo exposes it.
            ApplyLevelLighting(levelInfo);
        }

        public void ReloadLevel() => ReloadLevel(LevelInfo);

        public void ClearLevel()
        {
            Level = null;
            Render.ReloadLevel(null);
        }

        // -------------------------------------------------------------
        // Selection + focus tools
        // -------------------------------------------------------------
        // Selection + focus tools
        // -------------------------------------------------------------
        private void HookSelection()
        {
            if (ModelSelector != null)
            {
                ModelSelector.SelectedModelChanged += OnSelectedModelChanged;
            }
        }

        private void OnSelectedModelChanged(object? sender, BaseModel? model)
        {
            if (model?.Entity != null)
            {
                Render.EditorSettings.SelectedEntity = model.Entity;
            }
            else
            {
                Render.EditorSettings.SelectedEntity = null;
            }
        }

        public void SelectEntityById(int entityId)
        {
            if (ModelSelector is ModelSelectorDefault selector)
                selector.SelectByEntityId(entityId);
        }

        public void FocusOnBounds(BoundingBox bounds)
        {
            var center = (bounds.Min + bounds.Max) * 0.5f;
            var size = bounds.Max - bounds.Min;
            var radius = size.Length() * 0.5f;
            if (radius < 0.1f)
                radius = 1f;

            var forward = Camera.Forward;
            if (forward == Vector3.Zero)
                forward = Vector3.Forward;

            forward.Normalize();

            var distance = radius * 2.5f;
            var newPos = center - forward * distance;

            Camera.Position = newPos;
            Camera.Target = center;
        }

        public void FocusOnEntity(EntityInfo entity)
        {
            if (entity == null)
                return;

            // Approximate a bounding box around the entity position.
            var center = entity.Position;
            var halfExtent = new Vector3(1f, 1f, 1f);

            var bounds = new BoundingBox(center - halfExtent, center + halfExtent);
            FocusOnBounds(bounds);
        }


        // -------------------------------------------------------------
        // ENTITY QUERIES (for inspectors / lists)
        // -------------------------------------------------------------
        public System.Collections.Generic.IReadOnlyList<EntityInfo> GetAllEntities()
        {
            return Level?.AllEntities ?? Array.Empty<EntityInfo>();
        }
        public void FocusOnSelection()
        {
            var selected = Render.EditorSettings.SelectedEntity;
            if (selected != null)
            {
                FocusOnEntity(selected);
            }
        }

        // -------------------------------------------------------------
        // LAYER VISIBILITY

        // -------------------------------------------------------------

        /// <summary>
        /// Generic way to mutate layer visibility (for bulk operations).
        /// </summary>
        public void SetLayerVisibility(Action<LayerVisibility> mutator)
        {
            mutator?.Invoke(Render.Layers);
        }

        /// <summary>
        /// Simple per-layer toggle used by WPF (SceneView.xaml.cs).
        /// </summary>
        public void SetLayerVisible(SceneLayer layer, bool visible)
        {
            switch (layer)
            {
                case SceneLayer.Grid:
                    Settings.ShowGrid = visible;
                    break;

                case SceneLayer.Collision:
                    Settings.ShowCollision = visible;
                    Render.Layers.ShowCollision = visible;
                    break;

                case SceneLayer.Props:
                    Settings.ShowProps = visible;
                    Render.Layers.ShowProps = visible;
                    break;

                case SceneLayer.Lights:
                    Settings.ShowLights = visible;
                    Render.Layers.ShowLights = visible;
                    break;

                case SceneLayer.Triggers:
                    Settings.ShowTriggers = visible;
                    Render.Layers.ShowTriggers = visible;
                    break;
            }
        }

        /// <summary>
        /// Bulk layer visibility setter used by SceneViewModel.PushLayerStateToRender().
        /// Geometry flag is reserved for future use if you add a dedicated geometry layer.
        /// </summary>
        public void SetLayerVisibility(
            bool showGeometry,
            bool showProps,
            bool showCollision,
            bool showLights,
            bool showTriggers,
            bool showGrid)
        {
            // Geometry: currently assumed always drawn; you can wire this into
            // Level/Render later if you add an explicit "ShowGeometry" flag.
            // For now we just store other layers in Settings + Layers.

            Settings.ShowGrid = showGrid;
            Settings.ShowCollision = showCollision;
            Settings.ShowProps = showProps;
            Settings.ShowLights = showLights;
            Settings.ShowTriggers = showTriggers;

            Render.Layers.ShowCollision = showCollision;
            Render.Layers.ShowProps = showProps;
            Render.Layers.ShowLights = showLights;
            Render.Layers.ShowTriggers = showTriggers;
        }

        // Convenience helpers (if used anywhere else)
        public void SetShowGrid(bool v) => Settings.ShowGrid = v;
        public void SetShowCollision(bool v) { Settings.ShowCollision = v; Render.Layers.ShowCollision = v; }
        public void SetShowProps(bool v) { Settings.ShowProps = v; Render.Layers.ShowProps = v; }
        public void SetShowLights(bool v) { Settings.ShowLights = v; Render.Layers.ShowLights = v; }
        public void SetShowTriggers(bool v) { Settings.ShowTriggers = v; Render.Layers.ShowTriggers = v; }

        // -------------------------------------------------------------
        // GRID SNAP
        // -------------------------------------------------------------
        /// <summary>
        /// Called from SceneViewModel when WPF snap controls change.
        /// This just updates EditorRenderSettings; gizmos/render can read from there.
        /// </summary>
        public void SetGridSnap(bool useGridSnap, float gridSize)
        {
            if (gridSize <= 0f)
                gridSize = 1f;

            // EditorRenderSettings uses EnableGridSnap + Vector3 GridSize
            Settings.EnableGridSnap = useGridSnap;
            Settings.GridSize = new Vector3(gridSize, gridSize, gridSize);
        }


        /// <summary>
        /// Called from SceneViewModel when WPF rotation snap controls change.
        /// Updates EditorRenderSettings so gizmos/render can respect angle snapping.
        /// </summary>
        public void SetRotationSnap(bool enableRotationSnap, float rotationSnapDegrees)
        {
            if (rotationSnapDegrees <= 0f)
                rotationSnapDegrees = 1f;

            Settings.EnableRotationSnap = enableRotationSnap;
            Settings.RotationSnapDegrees = rotationSnapDegrees;
        }

        // -------------------------------------------------------------
        // EDITOR UPDATE HOOKS
        // -------------------------------------------------------------
        public void UpdateEditor(EditorInputSnapshot input, EntityInfo? selectedEntity, GameTime gameTime)
        {
            Render.UpdateEditor(input, selectedEntity, gameTime);
        }

        public void UpdateCameraPath(EditorInputSnapshot input, GameTime gameTime)
        {
            if (input.KeyRecordCameraPath)
                CameraPathRecorder.ToggleRecording();

            if (input.KeyPlayCameraPath)
                CameraPathRecorder.StartPlayback();

            CameraPathRecorder.Update(gameTime);
        }

        // -------------------------------------------------------------
        // LIGHTING (stubbed hooks for now)
        // -------------------------------------------------------------

        /// <summary>
        /// Called by WPF UI or hotkeys. Currently just a placeholder hook.
        /// You can later forward this into Render / BasicEffect lighting.
        /// </summary>
        
        public void SetDirectionalLight(
            Vector3 direction,
            Color diffuse,
            Color ambient,
            float intensity = 1f)
        {
            // Intensity is a scalar for ambient for now.
            var ambientVec = ambient.ToVector3() * intensity;
            ambientVec.X = MathHelper.Clamp(ambientVec.X, 0f, 1f);
            ambientVec.Y = MathHelper.Clamp(ambientVec.Y, 0f, 1f);
            ambientVec.Z = MathHelper.Clamp(ambientVec.Z, 0f, 1f);

            var ambientColor = new Color(ambientVec);

            Render.SetDirectionalLight(direction, diffuse, ambientColor);
        }


        /// <summary>
        /// Applies lighting defaults defined in the .dat map (if any).
        /// Currently a no-op until LevelInfo exposes lighting fields and Render supports it.
        /// </summary>
        public void ApplyLevelLighting(LevelInfo info)
        {
            _ = info;
            // TODO: read lighting from info and apply via SetDirectionalLight when Render supports it.
        }

        // -------------------------------------------------------------

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _game.Components.Remove(CameraController);
            _game.Components.Remove(DebugText);
            _game.Components.Remove(Render);
            _game.Components.Remove(ModelSelector);
            _game.Components.Remove(Camera);

            Level = null;

            try { Render.Dispose(); }
            catch { /* ignore */ }
        }
    }
}