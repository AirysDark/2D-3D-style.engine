using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GE2D3D.MapEditor.Components.Render;   // Render.StaticDrawCalls
using GE2D3D.MapEditor.Data;
using GE2D3D.MapEditor.Data.Models;
using GE2D3D.MapEditor.Data.World;
using GE2D3D.MapEditor.Renders;
using GE2D3D.MapEditor.Utils;

namespace GE2D3D.MapEditor.World
{
    public class Level
    {
        public bool IsDark { get; set; }

        private LevelInfo LevelInfo { get; }
        private LevelRenderer LevelRenderer { get; }

        public List<BaseModel> Models { get; } = new List<BaseModel>();

        public Texture2D? DayCycleTexture;

        private Color[]? DaycycleTextureData;
        private Color LastSkyColor;
        private Color LastEntityColor;

        /// <summary>
        /// Per-layer visibility (geometry / props / collision / lights / triggers).
        /// Exposed to Render via Level.Layers and then to WPF via RenderBootstrap.
        /// </summary>
        public LayerVisibility Layers { get; } = LayerVisibility.CreateDefault();

        // Combined entity list (base + structures), for debug visualization (triggers, etc.).
        private readonly List<EntityInfo> _allEntities = new List<EntityInfo>();

        /// <summary>
        /// All entities in the level, including base entities and resolved structures.
        /// Exposed for tooling / inspectors; modifying EntityInfo instances will affect the level.
        /// </summary>
        public IReadOnlyList<EntityInfo> AllEntities => _allEntities;

        // Debug-only representation of point lights for editor visualization.
        private struct PointLightDebug
        {
            public Vector3 Position;
            public float Radius;
            public Color Color;
        }

        private readonly List<PointLightDebug> _pointLights = new List<PointLightDebug>();

        public Level(LevelInfo levelInfo, GraphicsDevice graphicsDevice)
        {
            LevelInfo = levelInfo ?? throw new ArgumentNullException(nameof(levelInfo));
            if (graphicsDevice == null)
                throw new ArgumentNullException(nameof(graphicsDevice));

            LevelRenderer = new LevelRenderer();

            // ----------------------------------------------------------------
            // 1) Load daycycle.png
            // ----------------------------------------------------------------
            try
            {
                var baseDir = LevelInfo.DirectoryLocation ?? string.Empty;
                var daycyclePath = Path.Combine(baseDir, "textures", "daycycle.png");

                if (!string.IsNullOrWhiteSpace(daycyclePath) && File.Exists(daycyclePath))
                {
                    DayCycleTexture = TextureHandler.LoadTexture(graphicsDevice, daycyclePath);
                }
                else
                {
                    DayCycleTexture = null;
                }
            }
            catch
            {
                DayCycleTexture = null;
            }

            SetLastColor();

            // ----------------------------------------------------------------
            // 2) Structures
            // ----------------------------------------------------------------
            var structureEntities = new List<EntityInfo>();

            var structures = LevelInfo.Structures ?? Enumerable.Empty<dynamic>();
            foreach (var structure in structures)
            {
                try
                {
                    var directory = LevelInfo.DirectoryLocation ?? string.Empty;

                    string mapName = structure.Map;
                    if (!Path.HasExtension(mapName))
                        mapName += ".dat";

                    var structurePath = Path.Combine(directory, mapName);
                    if (!File.Exists(structurePath))
                        continue;

                    var file = File.ReadAllText(structurePath);
                    var structureData = LevelLoader.Load(file, structurePath);

                    if (structureData?.Entities == null)
                        continue;

                    foreach (var entityInfo in structureData.Entities)
                    {
                        entityInfo.Parent = levelInfo;
                        entityInfo.Position += structure.Offset;

                        var rot = Entity.GetRotationFromVector(entityInfo.Rotation) +
                                  (structure.Rotation == -1 ? 0 : structure.Rotation);

                        while (rot > 3) rot -= 4;
                        entityInfo.Rotation = Entity.GetRotationFromInteger(rot);
                    }

                    structureEntities.AddRange(structureData.Entities);
                }
                catch { }
            }

            // ----------------------------------------------------------------
            // 3) Combine entities
            // ----------------------------------------------------------------
            var baseEntities = LevelInfo.Entities ?? new List<EntityInfo>();
            var combined = baseEntities.Concat(structureEntities).ToList();

            _allEntities.Clear();
            _allEntities.AddRange(combined);
            _pointLights.Clear();

            foreach (var entity in combined)
            {
                try
                {
                    // Heuristic: treat entities whose ID contains "Light" as point lights for debug visualization.
                    // This does not affect actual rendering; it's purely used by DrawLightVolumes.
                    if (!string.IsNullOrWhiteSpace(entity.EntityID) &&
                        entity.EntityID.IndexOf("Light", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        float radius = entity.Size != Vector3.Zero
                            ? (entity.Size.X + entity.Size.Y + entity.Size.Z) / 3f
                            : 3f;

                        if (radius <= 0.1f)
                            radius = 3f;

                        _pointLights.Add(new PointLightDebug
                        {
                            Position = entity.Position,
                            Radius = radius,
                            Color = entity.Shader != default ? entity.Shader : Color.White
                        });
                    }

                    entity.Shader = GetDaytimeColor(shader: true);
                    var model = BaseModel.GetModelByEntityInfo(entity, graphicsDevice);
                    if (model != null)
                        Models.Add(model);
                }
                catch { }
            }

            LevelRenderer.HandleModels(Models);
            LevelRenderer.Setup(graphicsDevice);
        }

        // ======================================================
        // Lighting
        // ======================================================

        public void UpdateLighting(BasicEffect effect)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));

            effect.LightingEnabled = true;
            effect.PreferPerPixelLighting = true;
            effect.SpecularPower = 2000f;

            switch (GetLightingType())
            {
                case DayTime.Night:
                    effect.AmbientLightColor = new Vector3(0.8f);
                    effect.DirectionalLight0.DiffuseColor = new Vector3(0.4f, 0.4f, 0.6f);
                    effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1f, 0f, 1f));
                    effect.DirectionalLight0.Enabled = true;
                    break;

                case DayTime.Morning:
                    effect.AmbientLightColor = new Vector3(0.7f);
                    effect.DirectionalLight0.DiffuseColor = Color.Orange.ToVector3();
                    effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1f, -1f, 1f));
                    effect.DirectionalLight0.Enabled = true;
                    break;

                case DayTime.Day:
                    effect.AmbientLightColor = Vector3.One;
                    effect.DirectionalLight0.DiffuseColor = new Vector3(-0.3f);
                    effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1f, 1f, 1f));
                    effect.DirectionalLight0.Enabled = true;
                    break;

                case DayTime.Evening:
                    effect.AmbientLightColor = Vector3.One;
                    effect.DirectionalLight0.DiffuseColor = new Vector3(-0.45f);
                    effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1f, 0f, 1f));
                    effect.DirectionalLight0.Enabled = true;
                    break;

                default:
                    effect.LightingEnabled = false;
                    break;
            }
        }

        public DayTime GetLightingType()
        {
            var lightType = DayTime.Day;

            if (LevelInfo.LightingType == 1)
                return (DayTime)99;

            if (LevelInfo.LightingType > 1 & LevelInfo.LightingType < 6)
                lightType = (DayTime)(LevelInfo.LightingType - 2);

            return lightType;
        }

        public Color GetDaytimeColor(bool shader) => shader ? LastEntityColor : LastSkyColor;

        private void SetLastColor()
        {
            if (DayCycleTexture == null || DayCycleTexture.Width <= 0 || DayCycleTexture.Height <= 0)
            {
                LastSkyColor = Color.White;
                LastEntityColor = Color.White;
                return;
            }

            if (DaycycleTextureData == null)
            {
                var totalPixels = DayCycleTexture.Width * DayCycleTexture.Height;
                if (totalPixels <= 0)
                {
                    LastSkyColor = Color.White;
                    LastEntityColor = Color.White;
                    return;
                }

                DaycycleTextureData = new Color[totalPixels];
                DayCycleTexture.GetData(DaycycleTextureData);
            }

            var data = DaycycleTextureData;
            if (data == null || data.Length == 0)
                return;

            int timeMinutes = GetTimeValue();
            int pixelIndex = timeMinutes % data.Length;
            if (pixelIndex < 0) pixelIndex += data.Length;

            Color pixelColor = data[pixelIndex];
            if (pixelColor != LastSkyColor)
            {
                LastSkyColor = pixelColor;

                int entityIndexOffset = DayCycleTexture.Width;
                int entityIndex = (pixelIndex + entityIndexOffset) % data.Length;
                if (entityIndex < 0) entityIndex += data.Length;

                LastEntityColor = data[entityIndex];
            }
        }

        private int GetTimeValue()
        {
            var time = new DateTime(1, 1, 1, 12, 30, 0);
            return time.Hour * 60 + time.Minute;
        }

        // ======================================================
        // Rendering
        // ======================================================

        public BoundingBox GetEntityBounds(EntityInfo entity)
        {
            if (entity == null)
                return new BoundingBox();

            // Base size from entity's logical size, fall back to 1x1x1 if missing.
            var size = entity.Size;
            if (size == Vector3.Zero)
                size = Vector3.One;

            // Apply the entity's scale so the box reflects the rendered size.
            var scale = entity.Scale;
            if (scale == Vector3.Zero)
                scale = Vector3.One;

            var half = (size * scale) * 0.5f;
            var center = entity.Position;

            return new BoundingBox(center - half, center + half);
        }

        public void Draw(
            BasicEffect basicEffect,
            AlphaTestEffect alphaTestEffect,
            EditorRenderSettings? editorSettings = null)
        {
            if (basicEffect == null)
                throw new ArgumentNullException(nameof(basicEffect));
            if (alphaTestEffect == null)
                throw new ArgumentNullException(nameof(alphaTestEffect));

            Render.StaticDrawCalls = 0;

            // Lighting toggle
            bool lightingEnabled = editorSettings?.EnableLighting ?? true;
            if (!lightingEnabled)
            {
                basicEffect.LightingEnabled = false;
            }
            else
            {
                UpdateLighting(basicEffect);
            }

            // Draw core models (geometry + props for now)
            // Later we can pass Layers into LevelRenderer to split geometry/props
            LevelRenderer.Draw(this, basicEffect, alphaTestEffect);

            // Combined flags: editor settings AND layer visibility
            bool showCollision = (editorSettings?.ShowCollision ?? true) && Layers.ShowCollision;
            bool showLights = (editorSettings?.ShowLights ?? true) && Layers.ShowLights;
            bool showTriggers = (editorSettings?.ShowTriggers ?? true) && Layers.ShowTriggers;
            bool showGrid = (editorSettings?.ShowGrid ?? true); // grid is global, not a layer

            // ==========================================
            // COLLISION (currently stub)
            // ==========================================
            if (showCollision)
            {
                DrawCollisionDebug(basicEffect);
            }

            // ==========================================
            // LIGHT VOLUMES
            // ==========================================
            if (showLights)
            {
                DrawLightVolumes(basicEffect, editorSettings ?? new EditorRenderSettings());
            }

            // ==========================================
            // TRIGGERS / VOLUMES
            // ==========================================
            if (showTriggers)
            {
                DrawTriggerVolumes(basicEffect, editorSettings ?? new EditorRenderSettings());
            }

            // ==========================================
            // GRID
            // ==========================================
            if (showGrid)
            {
                DrawGrid(basicEffect);
            }

            // ==========================================
            // SELECTION OUTLINE
            // ==========================================
            var selected = editorSettings?.SelectedEntity;
            if (selected != null)
            {
                DrawSelectionHighlight(basicEffect, selected);
            }
        }

        // ======================================================
        // Debug: Collision volumes
        // ======================================================
        private void DrawCollisionDebug(BasicEffect effect)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));

            if (_allEntities.Count == 0)
                return;

            // Heuristic collision detection:
            // treat entities whose ID suggests collision / blocking as collision volumes.
            bool IsCollisionLike(EntityInfo e)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.EntityID))
                    return false;

                var id = e.EntityID;
                return id.IndexOf("Collision", StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("Collide", StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("Block", StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("Solid", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            var verts = new List<VertexPositionColor>();
            var color = new Color(255, 80, 80, 220); // reddish for collision

            void AddBox(BoundingBox box)
            {
                var c = box.GetCorners();

                void Edge(int a, int b)
                {
                    verts.Add(new VertexPositionColor(c[a], color));
                    verts.Add(new VertexPositionColor(c[b], color));
                }

                // 12 edges
                Edge(0, 1); Edge(1, 2); Edge(2, 3); Edge(3, 0);
                Edge(4, 5); Edge(5, 6); Edge(6, 7); Edge(7, 4);
                Edge(0, 4); Edge(1, 5); Edge(2, 6); Edge(3, 7);
            }

            foreach (var e in _allEntities)
            {
                if (!IsCollisionLike(e))
                    continue;

                Vector3 center = e.Position;
                Vector3 halfExtent;

                if (e.Size != Vector3.Zero)
                {
                    halfExtent = e.Size * 0.5f;
                }
                else
                {
                    // Fallback small box if no explicit size defined.
                    halfExtent = new Vector3(0.5f, 0.5f, 0.5f);
                }

                var bb = new BoundingBox(center - halfExtent, center + halfExtent);
                AddBox(bb);
            }

            if (verts.Count == 0)
                return;

            var graphics = effect.GraphicsDevice;
            if (graphics == null)
                return;

            var oldRasterizer = graphics.RasterizerState;
            graphics.RasterizerState = new RasterizerState { CullMode = CullMode.None };

            bool oldTex = effect.TextureEnabled;
            bool oldVc = effect.VertexColorEnabled;
            var oldWorld = effect.World;

            effect.TextureEnabled = false;
            effect.VertexColorEnabled = true;
            effect.World = Matrix.Identity;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphics.DrawUserPrimitives(
                    PrimitiveType.LineList,
                    verts.ToArray(),
                    0,
                    verts.Count / 2);
            }

            effect.TextureEnabled = oldTex;
            effect.VertexColorEnabled = oldVc;
            effect.World = oldWorld;
            graphics.RasterizerState = oldRasterizer;
        }

        // ======================================================
        // Debug: Light volumes
        // ======================================================
        
                private void DrawLightVolumes(BasicEffect effect, EditorRenderSettings settings)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));

            // ----------------------------------------------------------------
            // Directional light arrow
            // ----------------------------------------------------------------
            var dirLight = effect.DirectionalLight0;
            if (dirLight != null && effect.LightingEnabled)
            {
                var dir = dirLight.Direction;
                if (dir != Vector3.Zero)
                {
                    dir.Normalize();

                    // Choose an arbitrary origin for the debug arrow. In the future you might want to
                    // place this at the scene origin or average of light-relevant geometry.
                    var origin = Vector3.Zero;
                    float length = 4f;

                    var end = origin + dir * length;

                    // Build a simple arrow head
                    Vector3 up = Vector3.Up;
                    if (Math.Abs(Vector3.Dot(up, dir)) > 0.99f)
                        up = Vector3.Right;

                    var side = Vector3.Normalize(Vector3.Cross(dir, up));
                    float headSize = 0.75f;

                    var headBase = end - dir * headSize;
                    var headLeft = headBase + side * headSize;
                    var headRight = headBase - side * headSize;

                    var arrowVerts = new[]
                    {
                        // Shaft
                        new VertexPositionColor(origin, Color.Yellow),
                        new VertexPositionColor(end, Color.Yellow),

                        // Head
                        new VertexPositionColor(end, Color.Yellow),
                        new VertexPositionColor(headLeft, Color.Yellow),

                        new VertexPositionColor(end, Color.Yellow),
                        new VertexPositionColor(headRight, Color.Yellow),
                    };

                    bool prevTextureEnabled = effect.TextureEnabled;
                    bool prevVertexColorEnabled = effect.VertexColorEnabled;
                    var prevWorld = effect.World;

                    effect.TextureEnabled = false;
                    effect.VertexColorEnabled = true;
                    effect.World = Matrix.Identity;

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        effect.GraphicsDevice.DrawUserPrimitives(
                            PrimitiveType.LineList,
                            arrowVerts,
                            0,
                            3);
                    }

                    effect.TextureEnabled = prevTextureEnabled;
                    effect.VertexColorEnabled = prevVertexColorEnabled;
                    effect.World = prevWorld;
                }
            }

            // ----------------------------------------------------------------
            // Point lights (debug): simple wire spheres + ground radius rings
            // Built dynamically from the current entities so editor changes
            // (light radius / colour) are reflected immediately.
            // ----------------------------------------------------------------
            if (_allEntities.Count > 0)
            {
                foreach (var entity in _allEntities)
                {
                    if (entity == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(entity.EntityID) ||
                        entity.EntityID.IndexOf("Light", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    // Colour comes from the entity's Shader; fall back to white.
                    var col = entity.Shader != default ? entity.Shader : Color.White;

                    // Radius: prefer explicit Size, fall back to a small default.
                    var size = entity.Size;
                    float radius = size != Vector3.Zero
                        ? (size.X + size.Y + size.Z) / 3f
                        : 3f;

                    if (radius <= 0.1f)
                        radius = 3f;

                    var pl = new PointLightDebug
                    {
                        Position = entity.Position,
                        Radius = radius,
                        Color = col
                    };

                    DrawPointLightDebug(effect, pl);
                }
            }
        }

        /// <summary>
        /// Debug drawing for a point light: simple wire "sphere" (3 great circles)
        /// plus a ground-projected radius ring.
        /// </summary>
        private void DrawPointLightDebug(BasicEffect basicEffect, PointLightDebug pl)
        {
            if (basicEffect == null)
                throw new ArgumentNullException(nameof(basicEffect));

            var graphics = basicEffect.GraphicsDevice;
            if (graphics == null)
                return;

            const int segments = 32;
            float step = MathHelper.TwoPi / segments;

            var color = pl.Color;
            if (color == default)
                color = Color.White;

            var verts = new List<VertexPositionColor>();

            void AddRing(Func<float, Vector3> pointOnRing)
            {
                for (int i = 0; i < segments; i++)
                {
                    float a0 = step * i;
                    float a1 = step * ((i + 1) % segments);

                    var p0 = pointOnRing(a0);
                    var p1 = pointOnRing(a1);

                    verts.Add(new VertexPositionColor(p0, color));
                    verts.Add(new VertexPositionColor(p1, color));
                }
            }

            float r = pl.Radius;
            var center = pl.Position;

            // Three great circles to approximate a sphere.
            AddRing(a => new Vector3(
                center.X,
                center.Y + (float)Math.Cos(a) * r,
                center.Z + (float)Math.Sin(a) * r)); // YZ plane (around X)

            AddRing(a => new Vector3(
                center.X + (float)Math.Cos(a) * r,
                center.Y,
                center.Z + (float)Math.Sin(a) * r)); // XZ plane (around Y)

            AddRing(a => new Vector3(
                center.X + (float)Math.Cos(a) * r,
                center.Y + (float)Math.Sin(a) * r,
                center.Z)); // XY plane (around Z)

            // Ground-projected radius ring (XZ plane at the light's Y).
            var groundColor = new Color(color.R, color.G, color.B, (byte)160);
            for (int i = 0; i < segments; i++)
            {
                float a0 = step * i;
                float a1 = step * ((i + 1) % segments);

                var p0 = new Vector3(
                    center.X + (float)Math.Cos(a0) * r,
                    center.Y,
                    center.Z + (float)Math.Sin(a0) * r);

                var p1 = new Vector3(
                    center.X + (float)Math.Cos(a1) * r,
                    center.Y,
                    center.Z + (float)Math.Sin(a1) * r);

                verts.Add(new VertexPositionColor(p0, groundColor));
                verts.Add(new VertexPositionColor(p1, groundColor));
            }

            if (verts.Count == 0)
                return;

            var oldRasterizer = graphics.RasterizerState;
            graphics.RasterizerState = new RasterizerState { CullMode = CullMode.None };

            bool oldTex = basicEffect.TextureEnabled;
            bool oldVc = basicEffect.VertexColorEnabled;
            var oldWorld = basicEffect.World;

            basicEffect.TextureEnabled = false;
            basicEffect.VertexColorEnabled = true;
            basicEffect.World = Matrix.Identity;

            foreach (var pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphics.DrawUserPrimitives(
                    PrimitiveType.LineList,
                    verts.ToArray(),
                    0,
                    verts.Count / 2);
            }

            basicEffect.TextureEnabled = oldTex;
            basicEffect.VertexColorEnabled = oldVc;
            basicEffect.World = oldWorld;
            graphics.RasterizerState = oldRasterizer;
        }
        // ======================================================
        // Debug: Trigger volumes
        // ======================================================
        private void DrawTriggerVolumes(BasicEffect effect, EditorRenderSettings settings)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));

            if (_allEntities.Count == 0)
                return;

            // Heuristic trigger detection: treat entities whose ID suggests trigger-like behaviour as volumes.
            bool IsTriggerLike(EntityInfo e)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.EntityID))
                    return false;

                var id = e.EntityID;
                return id.IndexOf("Trigger", StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("Warp", StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("Area", StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("Zone", StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("Region", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            var verts = new List<VertexPositionColor>();
            var color = new Color(0, 255, 255, 200); // cyan-ish, slightly transparent

            void AddBox(BoundingBox box)
            {
                var c = box.GetCorners();

                void Edge(int a, int b)
                {
                    verts.Add(new VertexPositionColor(c[a], color));
                    verts.Add(new VertexPositionColor(c[b], color));
                }

                // 12 edges
                Edge(0, 1); Edge(1, 2); Edge(2, 3); Edge(3, 0);
                Edge(4, 5); Edge(5, 6); Edge(6, 7); Edge(7, 4);
                Edge(0, 4); Edge(1, 5); Edge(2, 6); Edge(3, 7);
            }

            foreach (var e in _allEntities)
            {
                if (!IsTriggerLike(e))
                    continue;

                // Use entity.Position and optionally entity.Size to build a bounding volume.
                Vector3 center = e.Position;
                Vector3 halfExtent;

                if (e.Size != Vector3.Zero)
                {
                    halfExtent = e.Size * 0.5f;
                }
                else
                {
                    // Fallback small box if no size defined.
                    halfExtent = new Vector3(0.5f, 0.5f, 0.5f);
                }

                var bb = new BoundingBox(center - halfExtent, center + halfExtent);
                AddBox(bb);
            }

            if (verts.Count == 0)
                return;

            var graphics = effect.GraphicsDevice;
            if (graphics == null)
                return;

            var oldRasterizer = graphics.RasterizerState;
            graphics.RasterizerState = new RasterizerState { CullMode = CullMode.None };

            bool oldTex = effect.TextureEnabled;
            bool oldVc = effect.VertexColorEnabled;
            var oldWorld = effect.World;

            effect.TextureEnabled = false;
            effect.VertexColorEnabled = true;
            effect.World = Matrix.Identity;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphics.DrawUserPrimitives(
                    PrimitiveType.LineList,
                    verts.ToArray(),
                    0,
                    verts.Count / 2);
            }

            effect.TextureEnabled = oldTex;
            effect.VertexColorEnabled = oldVc;
            effect.World = oldWorld;
            graphics.RasterizerState = oldRasterizer;
        }

        // ======================================================
        // Selection Highlight
        // ======================================================

        private void DrawSelectionHighlight(BasicEffect basicEffect, EntityInfo entity)
        {
            var bounds = GetEntityBounds(entity);
            var corners = bounds.GetCorners();

            var verts = new List<VertexPositionColor>();

            void Edge(int a, int b)
            {
                verts.Add(new VertexPositionColor(corners[a], Color.Yellow));
                verts.Add(new VertexPositionColor(corners[b], Color.Yellow));
            }

            // 12 edges
            Edge(0, 1); Edge(1, 2); Edge(2, 3); Edge(3, 0);
            Edge(4, 5); Edge(5, 6); Edge(6, 7); Edge(7, 4);
            Edge(0, 4); Edge(1, 5); Edge(2, 6); Edge(3, 7);

            if (verts.Count == 0)
                return;

            var graphics = basicEffect.GraphicsDevice;

            var oldRasterizer = graphics.RasterizerState;
            graphics.RasterizerState = new RasterizerState { CullMode = CullMode.None };

            var oldVc = basicEffect.VertexColorEnabled;
            basicEffect.VertexColorEnabled = true;

            foreach (var pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphics.DrawUserPrimitives(PrimitiveType.LineList, verts.ToArray(), 0, verts.Count / 2);
            }

            basicEffect.VertexColorEnabled = oldVc;
            graphics.RasterizerState = oldRasterizer;
        }

        // ======================================================
        // Grid
        // ======================================================

        private void DrawGrid(BasicEffect basicEffect)
        {
            const int halfLines = 32;
            const float spacing = 1f;

            var verts = new List<VertexPositionColor>();
            var gridColor = new Color(80, 80, 80, 255);

            // X-lines
            for (int i = -halfLines; i <= halfLines; i++)
            {
                float z = i * spacing;
                verts.Add(new VertexPositionColor(new Vector3(-halfLines, 0, z), gridColor));
                verts.Add(new VertexPositionColor(new Vector3(+halfLines, 0, z), gridColor));
            }

            // Z-lines
            for (int i = -halfLines; i <= halfLines; i++)
            {
                float x = i * spacing;
                verts.Add(new VertexPositionColor(new Vector3(x, 0, -halfLines), gridColor));
                verts.Add(new VertexPositionColor(new Vector3(x, 0, +halfLines), gridColor));
            }

            if (verts.Count == 0)
                return;

            var graphics = basicEffect.GraphicsDevice;

            var oldRasterizer = graphics.RasterizerState;
            graphics.RasterizerState = new RasterizerState { CullMode = CullMode.None };

            var oldVc = basicEffect.VertexColorEnabled;
            basicEffect.VertexColorEnabled = true;

            foreach (var pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphics.DrawUserPrimitives(PrimitiveType.LineList, verts.ToArray(), 0, verts.Count / 2);
            }

            basicEffect.VertexColorEnabled = oldVc;
            graphics.RasterizerState = oldRasterizer;
        }
    }
}