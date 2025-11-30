using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GE2D3D.MapEditor.Components.Camera;
using GE2D3D.MapEditor.Components.Gizmo;
using GE2D3D.MapEditor.Components.Input;
using GE2D3D.MapEditor.Components.ModelSelector;
using GE2D3D.MapEditor.Data;
using GE2D3D.MapEditor.Effect;
using GE2D3D.MapEditor.Renders; // EditorRenderSettings
using GE2D3D.MapEditor.World;   // Level, LayerVisibility

namespace GE2D3D.MapEditor.Components.Render
{
    public enum AntiAliasing
    {
        None,
        FXAA,
        SSAA,
        MSAA
    }

    /// <summary>
    /// Core 3D render component used by both the standalone RenderTest host
    /// and the WPF SceneView host.
    /// </summary>
    public class Render : IGameComponent, IDrawable, IDisposable
    {
        private int _drawOrder;
        public int DrawOrder
        {
            get => _drawOrder;
            set
            {
                if (_drawOrder != value)
                {
                    _drawOrder = value;
                    OnDrawOrderChanged(this, EventArgs.Empty);
                }
            }
        }

        private bool _visible = true;
        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    OnVisibleChanged(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler<EventArgs>? DrawOrderChanged;
        public event EventHandler<EventArgs>? VisibleChanged;

        public int DrawCalls => StaticDrawCalls;
        internal static int StaticDrawCalls;

        private GraphicsDevice GraphicsDevice { get; }
        private SpriteBatch SpriteBatch { get; }

        // Created in ViewportChanged / Initialize
        private RenderTarget2D? RenderTarget { get; set; }

        // Created in Initialize
        private BasicEffect? BasicEffect { get; set; }
        private AlphaTestEffect? AlphaTestEffect { get; set; }
        private FxaaEffect? FxaaEffect { get; set; }

        private AntiAliasing _antiAliasing = AntiAliasing.MSAA;
        public AntiAliasing AntiAliasingMode => _antiAliasing;

        private int Scale { get; set; } = 1;

        // Created in ctor if levelInfo != null, or via ReloadLevel()
        private Level? Level { get; set; }

        // Fallback layer visibility if Level is null (e.g. nothing loaded yet)
        private readonly LayerVisibility _layersFallback = LayerVisibility.CreateDefault();

        /// <summary>
        /// Expose the current layer visibility for UI (geometry/props/collision/lights/triggers).
        /// When no Level is loaded, we use a local fallback instance so bindings still work.
        /// </summary>
        public LayerVisibility Layers => Level?.Layers ?? _layersFallback;

        private BaseCamera Camera { get; }
        private BaseModelSelector ModelSelector { get; }

        /// <summary>
        /// Editor-specific render settings (grid, collision, lights, gizmo, etc).
        /// Shared via RenderBootstrap (RenderBootstrap.Settings forwards to this).
        /// </summary>
        public EditorRenderSettings EditorSettings { get; }

        /// <summary>
        /// Selection transform gizmo (move/rotate/scale).
        /// </summary>
        private TransformGizmo? _transformGizmo;

        // --------------------------------------------------------------------
        // Directional lighting state (defaults, can be overridden by map or UI)
        // --------------------------------------------------------------------
        private Vector3 _dirLightDirection = new Vector3(-0.3f, -1f, -0.4f);
        private Color _dirLightDiffuse = Color.White;
        private Color _ambientLight = new Color(40, 40, 40);

        // Skybox state (configured via RenderBootstrap)
        private Texture2D? _innerSkyboxTexture2D;
        private Texture2D? _outerSkyboxTexture2D;
        private bool _skyboxEnabled;


        public Render(
            GraphicsDevice graphicsDevice,
            BaseCamera camera,
            BaseModelSelector modelSelector,
            LevelInfo levelInfo,
            EditorRenderSettings? editorSettings = null)
        {
            GraphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            Camera = camera ?? throw new ArgumentNullException(nameof(camera));
            ModelSelector = modelSelector ?? throw new ArgumentNullException(nameof(modelSelector));

            if (levelInfo == null)
                throw new ArgumentNullException(nameof(levelInfo));

            Level = new Level(levelInfo, graphicsDevice);

            // Allow caller to share a settings instance (WPF <-> RenderTest),
            // or create a default one if not provided.
            EditorSettings = editorSettings ?? new EditorRenderSettings();
        }

        /// <summary>
        /// Allows swapping the Level instance at runtime (used for Open Map / live reload).
        /// Caller (RenderBootstrap / SceneView) creates the Level and passes it in.
        /// Passing null clears the current level.
        /// </summary>
        public void ReloadLevel(Level? level)
        {
            if (ReferenceEquals(Level, level))
                return;

            if (Level is IDisposable disposableLevel)
                disposableLevel.Dispose();

            Level = level;

            // Copy previous visibility into new level, so UI toggles persist.
            if (Level != null)
            {
                Level.Layers.ShowGeometry = _layersFallback.ShowGeometry;
                Level.Layers.ShowProps = _layersFallback.ShowProps;
                Level.Layers.ShowCollision = _layersFallback.ShowCollision;
                Level.Layers.ShowLights = _layersFallback.ShowLights;
                Level.Layers.ShowTriggers = _layersFallback.ShowTriggers;
            }
        }

        public void Initialize()
        {
            // FXAA
            if (FxaaEffect == null)
            {
                FxaaEffect = new FxaaEffect(GraphicsDevice);
                FxaaEffect.SetDefaultQualityParameters();
            }

            ViewportChanged();

            // Basic effect
            if (BasicEffect == null)
            {
                BasicEffect = new BasicEffect(GraphicsDevice)
                {
                    TextureEnabled = true,
                    VertexColorEnabled = true,
                    FogEnabled = false,
                    LightingEnabled = true,
                    PreferPerPixelLighting = true
                };
            }

            // Alpha test effect
            if (AlphaTestEffect == null)
            {
                AlphaTestEffect = new AlphaTestEffect(GraphicsDevice)
                {
                    VertexColorEnabled = true,
                    FogEnabled = false
                };
            }

            // If the Level has its own lighting logic, let it configure BasicEffect.
            // Otherwise we fall back to our default directional light.
            if (Level != null && BasicEffect != null)
            {
                Level.UpdateLighting(BasicEffect);
            }
            else
            {
                ApplyDirectionalLightingToEffect();
            }

            // Selection transform gizmo
            _transformGizmo ??= new TransformGizmo(GraphicsDevice, Camera, EditorSettings);
        }

        public void ViewportChanged()
        {
            RenderTarget?.Dispose();

            RenderTarget = new RenderTarget2D(
                GraphicsDevice,
                GraphicsDevice.Viewport.Width * Scale,
                GraphicsDevice.Viewport.Height * Scale,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8);

            if (FxaaEffect != null && RenderTarget != null)
            {
                FxaaEffect.InverseDimensions = new Vector2(
                    1f / (GraphicsDevice.Viewport.Width * Scale),
                    1f / (GraphicsDevice.Viewport.Height * Scale));
                FxaaEffect.RenderTarget = RenderTarget;
            }
        }

        public void SetAntiAliasing(AntiAliasing antiAliasing)
        {
            _antiAliasing = antiAliasing;
            Scale = 1;

            switch (_antiAliasing)
            {
                case AntiAliasing.None:
                    break;

                case AntiAliasing.FXAA:
                    break;

                case AntiAliasing.SSAA:
                    Scale = 2;
                    break;

                case AntiAliasing.MSAA:
                    break;
            }

            ViewportChanged();
        }

        /// <summary>
        /// Called by RenderBootstrap when skybox textures or enabled state change.
        /// </summary>
        public void SetSkyboxTextures(Texture2D? inner, Texture2D? outer, bool enabled)
        {
            _innerSkyboxTexture2D = inner;
            _outerSkyboxTexture2D = outer;
            _skyboxEnabled = enabled;
        }

        /// <summary>
        /// Update ambient term from sky colour. Intensity is a multiplier in 0..~2 range.
        /// </summary>
        public void SetAmbientFromSky(Vector3 ambientFromSky, float intensity = 1f)
        {
            var v = ambientFromSky * intensity;
            v.X = MathHelper.Clamp(v.X, 0f, 1f);
            v.Y = MathHelper.Clamp(v.Y, 0f, 1f);
            v.Z = MathHelper.Clamp(v.Z, 0f, 1f);

            _ambientLight = new Color(v);
            ApplyDirectionalLightingToEffect();
        }


        /// <summary>
        /// Called by hosts (RenderTest, WPF SceneView) each frame to update
        /// editor-specific behavior like gizmos, snapping, etc.
        /// </summary>
        public void UpdateEditor(EditorInputSnapshot input, EntityInfo? selectedEntity, GameTime gameTime)
        {
            if (_transformGizmo != null)
            {
                _transformGizmo.SetSelectedEntity(selectedEntity);
                _transformGizmo.Update(input, gameTime);
            }

            // Example grid snap hook once gizmo exposes final transform:
            //
            // if (EditorSettings.EnableGridSnap &&
            //     selectedEntity != null &&
            //     _transformGizmo?.CurrentMode == GizmoMode.Translate)
            // {
            //     selectedEntity.Position =
            //         ApplyGridSnap(selectedEntity.Position, EditorSettings.GridSize);
            // }
        }

        public void Draw(GameTime gameTime)
        {
            // Ensure we've been initialized (MonoGame calls Initialize via Game.Components,
            // but this protects manual usage).
            if (BasicEffect == null || AlphaTestEffect == null || RenderTarget == null)
            {
                Initialize();
                if (BasicEffect == null || AlphaTestEffect == null || RenderTarget == null)
                    return;
            }

            BasicEffect.View = Camera.ViewMatrix;
            BasicEffect.Projection = Camera.ProjectionMatrix;
            AlphaTestEffect.View = Camera.ViewMatrix;
            AlphaTestEffect.Projection = Camera.ProjectionMatrix;


            var prevRenderTargets = GraphicsDevice.GetRenderTargets();

            GraphicsDevice.SetRenderTarget(RenderTarget);

            // If skybox is enabled, clear to ambient-derived colour; otherwise classic CornflowerBlue.
            if (EditorSettings.ShowSkybox && _skyboxEnabled)
            {
                var c = _ambientLight;
                if (c.A == 0) c.A = 255;
                GraphicsDevice.Clear(c);

                // Placeholder hook: actual cube skybox rendering can be added here later.
                // DrawSkyboxIfEnabled();
            }
            else
            {
                GraphicsDevice.Clear(Color.CornflowerBlue);
            }

            // Level.Draw should honor EditorSettings + Level.Layers (geometry/props/collision/lights/triggers).
            Level?.Draw(BasicEffect, AlphaTestEffect, EditorSettings);
            ModelSelector?.Draw(BasicEffect);

            // Draw selection gizmo on top of scene
            if (_transformGizmo != null && EditorSettings.ShowSelectionGizmo)
            {
                _transformGizmo.Draw(BasicEffect);
            }

            // Restore previous render targets
            GraphicsDevice.SetRenderTargets(prevRenderTargets);

            // Blit the RenderTarget to backbuffer with AA mode
            switch (_antiAliasing)
            {
                case AntiAliasing.None:
                case AntiAliasing.MSAA:
                    SpriteBatch.Begin(SpriteSortMode.Immediate,
                                      BlendState.Opaque,
                                      SamplerState.PointClamp,
                                      depthStencilState: null,
                                      rasterizerState: null);
                    SpriteBatch.Draw(RenderTarget, Vector2.Zero, Color.White);
                    SpriteBatch.End();
                    break;

                case AntiAliasing.FXAA:
                    SpriteBatch.Begin(SpriteSortMode.Immediate,
                                      blendState: BlendState.Opaque,
                                      samplerState: SamplerState.PointClamp,
                                      depthStencilState: null,
                                      rasterizerState: null,
                                      effect: FxaaEffect);
                    SpriteBatch.Draw(RenderTarget,
                                     Vector2.Zero,
                                     null,
                                     Color.White,
                                     0f,
                                     Vector2.Zero,
                                     1f / Scale,
                                     SpriteEffects.None,
                                     0f);
                    SpriteBatch.End();
                    break;

                case AntiAliasing.SSAA:
                default:
                    SpriteBatch.Begin(SpriteSortMode.Immediate,
                                      BlendState.Opaque,
                                      SamplerState.PointClamp,
                                      depthStencilState: null,
                                      rasterizerState: null);
                    SpriteBatch.Draw(RenderTarget,
                                     Vector2.Zero,
                                     null,
                                     Color.White,
                                     0f,
                                     Vector2.Zero,
                                     1f / Scale,
                                     SpriteEffects.None,
                                     0f);
                    SpriteBatch.End();
                    break;
            }
        }

        protected virtual void OnVisibleChanged(object sender, EventArgs args)
            => VisibleChanged?.Invoke(this, args);

        protected virtual void OnDrawOrderChanged(object sender, EventArgs args)
            => DrawOrderChanged?.Invoke(this, args);

        // --------------------------------------------------------------------
        // Directional lighting helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Override the current directional light parameters from UI / tools.
        /// Call this from WPF or RenderTest to tweak lighting live.
        /// </summary>
        public void SetDirectionalLight(Vector3 direction, Color diffuse, Color ambient)
        {
            _dirLightDirection = direction;
            _dirLightDiffuse = diffuse;
            _ambientLight = ambient;

            ApplyDirectionalLightingToEffect();
        }

        /// <summary>
        /// Apply our current directional light state into BasicEffect.
        /// Safe to call multiple times.
        /// </summary>
        private void ApplyDirectionalLightingToEffect()
        {
            if (BasicEffect == null)
                return;

            BasicEffect.LightingEnabled = true;
            BasicEffect.PreferPerPixelLighting = true;

            var dir = _dirLightDirection;
            if (dir != Vector3.Zero)
                dir = Vector3.Normalize(dir);
            else
                dir = new Vector3(-0.3f, -1f, -0.4f);

            var diffuse = _dirLightDiffuse.ToVector3();
            var ambient = _ambientLight.ToVector3();

            var d0 = BasicEffect.DirectionalLight0;
            d0.Enabled = true;
            d0.Direction = dir;
            d0.DiffuseColor = diffuse;
            d0.SpecularColor = diffuse;

            BasicEffect.AmbientLightColor = ambient;
        }

        // --------------------------------------------------------------------
        // Grid snapping helper
        // --------------------------------------------------------------------
        /// <summary>
        /// Snap a position to a uniform grid size in all axes.
        /// Usage: var snapped = ApplyGridSnap(position, settings.GridSize);
        /// </summary>
        public static Vector3 ApplyGridSnap(Vector3 position, float gridSize)
        {
            if (gridSize <= 0f)
                gridSize = 1f;

            static float Snap(float value, float step)
                => (float)Math.Round(value / step) * step;

            return new Vector3(
                Snap(position.X, gridSize),
                Snap(position.Y, gridSize),
                Snap(position.Z, gridSize));
        }


        /// <summary>
        /// Placeholder for future skybox rendering (dual-cube, etc.).
        /// Currently unused; reserved for future implementation.
        /// </summary>
        private void DrawSkyboxIfEnabled()
        {
            // Intentionally left empty until TextureCube + effect are wired.
        }

        // --------------------------------------------------------------------
        // IDisposable
        // --------------------------------------------------------------------
        public void Dispose()
        {
            RenderTarget?.Dispose();
            SpriteBatch?.Dispose();
            BasicEffect?.Dispose();
            AlphaTestEffect?.Dispose();
            FxaaEffect?.Dispose();

            if (Level is IDisposable disposableLevel)
                disposableLevel.Dispose();
        }
    }
}