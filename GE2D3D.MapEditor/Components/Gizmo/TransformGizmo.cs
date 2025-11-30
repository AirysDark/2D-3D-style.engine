using GE2D3D.MapEditor.Components.Camera;
using GE2D3D.MapEditor.Components.Input;
using GE2D3D.MapEditor.Components.Render;
using GE2D3D.MapEditor.Data;
using GE2D3D.MapEditor.Renders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GE2D3D.MapEditor.Components.Gizmo
{
    /// <summary>
    /// Selection transform gizmo:
    /// - Translate on X/Y/Z (left mouse, axis handles)
    /// - Rotate on X/Y/Z (middle mouse, rings)
    /// - Scale on X/Y/Z (Ctrl + left mouse, axis)
    /// - Uniform scale (Ctrl + left mouse near center)
    /// Uses EditorRenderSettings for snap/grid/rotation/scale options.
    /// Also draws a selection bounding box for the current entity (if it has Size).
    /// </summary>
    public class TransformGizmo
    {
        private enum GizmoAxis
        {
            None,
            X,
            Y,
            Z
        }

        private const float AxisHitThresholdPixels = 10f;

        // Last computed view-space depth per axis (used for depth-aware picking & debug)
        // Larger value = farther from camera (since we store positive distance along view direction).
        private float _axisDepthX;
        private float _axisDepthY;
        private float _axisDepthZ;


        private readonly GraphicsDevice _graphicsDevice;
        private readonly BaseCamera _camera;
        private readonly EditorRenderSettings _settings;

        private EntityInfo? _selectedEntity;

        private bool _isDragging;
        private bool _isRotating;
        private bool _isScaling;
        private bool _isUniformScaling;

        private GizmoAxis _activeAxis = GizmoAxis.None;

        // Axis hover state for visual highlighting
        private GizmoAxis _hoverAxis = GizmoAxis.None;

        private Vector3 _dragStartWorld;
        private Vector3 _entityStartPos;
        private Vector3 _entityStartScale;
        private float _uniformStartRadius;

        // Reusable line buffers for drawing gizmo + selection bbox
        private readonly VertexPositionColor[] _lineVertices;
        private readonly short[] _lineIndices;

        public TransformGizmo(GraphicsDevice graphicsDevice, BaseCamera camera, EditorRenderSettings settings)
        {
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _lineVertices = new VertexPositionColor[2048];
            _lineIndices = new short[4096];
        }

        public void SetSelectedEntity(EntityInfo? entity)
        {
            _selectedEntity = entity;
            _isDragging = false;
            _isRotating = false;
            _isScaling = false;
            _isUniformScaling = false;
            _activeAxis = GizmoAxis.None;
            _hoverAxis = GizmoAxis.None;
        }

        public void Update(EditorInputSnapshot input, GameTime gameTime)
        {
            if (_selectedEntity == null)
                return;

            var center = _selectedEntity.Position;
            const float gizmoSize = 0.75f;

            // ------------------------------------
            // HOVER HIGHLIGHT (axis under cursor)
            // ------------------------------------
            _hoverAxis = GizmoAxis.None;
            if (_settings.ShowSelectionGizmo)
            {
                if (!_isDragging && !_isScaling && !_isRotating)
                {
                    // Only compute hover when no active drag/rotate/scale is in progress
                    _hoverAxis = HitTestAxis(input.MousePosition, center, gizmoSize);
                }
                else if (_activeAxis != GizmoAxis.None)
                {
                    // While interacting, keep hover aligned to the active axis
                    _hoverAxis = _activeAxis;
                }
            }

            // ----------------------------
            // ROTATION (middle mouse) 
            // ----------------------------
            if (input.MiddleButtonDown && !_isRotating && !_isDragging && !_isScaling && _settings.ShowSelectionGizmo)
            {
                float rotationRadius = gizmoSize * 1.2f;
                var rotAxis = HitTestRotationAxis(input.MousePosition, center, rotationRadius);
                if (rotAxis != GizmoAxis.None)
                {
                    _isRotating = true;
                    _activeAxis = rotAxis;
                }
            }

            if (!input.MiddleButtonDown && _isRotating)
            {
                _isRotating = false;
                _activeAxis = GizmoAxis.None;
            }

            if (_isRotating && _selectedEntity != null)
            {
                HandleRotationDrag(input, _selectedEntity);
                // While rotating, we don't process move/scale in the same frame
                return;
            }

            // ----------------------------
            // TRANSLATION (left mouse) 
            // ----------------------------
            if (input.LeftButtonDown && !_isDragging && !_isScaling && !_isRotating &&
                _settings.ShowSelectionGizmo)
            {
                var hitAxis = HitTestAxis(input.MousePosition, center, gizmoSize);
                if (hitAxis != GizmoAxis.None)
                {
                    _isDragging = true;
                    _activeAxis = hitAxis;

                    _dragStartWorld = UnprojectOnHorizontalPlane(input.MousePosition, center.Y);
                    _entityStartPos = _selectedEntity.Position;
                }
            }

            if (!input.LeftButtonDown && _isDragging)
            {
                _isDragging = false;
                _activeAxis = GizmoAxis.None;
            }

            if (_isDragging && _selectedEntity != null)
            {
                HandleTranslationDrag(input, _selectedEntity, center);
            }

            // ----------------------------
            // SCALE (Ctrl + left mouse) 
            // ----------------------------
            if (input.KeyCtrl && input.LeftButtonDown && !_isScaling && !_isDragging &&
                !_isRotating && _settings.ShowSelectionGizmo)
            {
                var viewport = _graphicsDevice.Viewport;
                var centerScreen = viewport.Project(center, _camera.ProjectionMatrix, _camera.ViewMatrix, Matrix.Identity);
                var mousePos = input.MousePosition.ToVector2();
                var centerPos = new Vector2(centerScreen.X, centerScreen.Y);

                // Uniform scale: click near center
                float centerThreshold = 12f;
                if (Vector2.Distance(mousePos, centerPos) <= centerThreshold)
                {
                    _isScaling = true;
                    _isUniformScaling = true;
                    _entityStartScale = _selectedEntity.Scale;
                    _entityStartPos = _selectedEntity.Position;
                    _uniformStartRadius = (center - _entityStartPos).Length();
                }
                else
                {
                    // Axis scale: click on one of the axes
                    var axisForScale = HitTestAxis(input.MousePosition, center, gizmoSize);
                    if (axisForScale != GizmoAxis.None)
                    {
                        _isScaling = true;
                        _isUniformScaling = false;
                        _activeAxis = axisForScale;
                        _entityStartScale = _selectedEntity.Scale;
                        _entityStartPos = _selectedEntity.Position;
                        _dragStartWorld = UnprojectOnHorizontalPlane(input.MousePosition, _entityStartPos.Y);
                    }
                }
            }

            if (!input.LeftButtonDown && _isScaling)
            {
                _isScaling = false;
                _isUniformScaling = false;
                _activeAxis = GizmoAxis.None;
            }

            if (_isScaling && _selectedEntity != null)
            {
                HandleScaleDrag(input, _selectedEntity, center);
            }
        }

        // ------------------------------------------------------------
        // ROTATION
        // ------------------------------------------------------------

        private void HandleRotationDrag(EditorInputSnapshot input, EntityInfo entity)
        {
            var mouseDelta = input.MouseDelta;
            if (mouseDelta.X == 0 && mouseDelta.Y == 0)
                return;

            var rot = entity.Rotation;
            float dx = mouseDelta.X;
            float dy = mouseDelta.Y;

            const float radiansPerPixel = 0.01f;

            switch (_activeAxis)
            {
                case GizmoAxis.X:
                    {
                        float pitch = rot.X - dy * radiansPerPixel;

                        if (_settings.EnableRotationSnap && !input.KeySnapToggle)
                        {
                            float step = _settings.RotationSnapDegrees;
                            if (step <= 0f)
                                step = 1f;

                            float deg = MathHelper.ToDegrees(pitch);
                            float snappedDeg = (float)Math.Round(deg / step) * step;
                            pitch = MathHelper.ToRadians(snappedDeg);
                        }

                        rot.X = pitch;
                        break;
                    }

                case GizmoAxis.Y:
                    {
                        float yaw = rot.Y + dx * radiansPerPixel;

                        if (_settings.EnableRotationSnap && !input.KeySnapToggle)
                        {
                            float step = _settings.RotationSnapDegrees;
                            if (step <= 0f)
                                step = 1f;

                            float deg = MathHelper.ToDegrees(yaw);
                            float snappedDeg = (float)Math.Round(deg / step) * step;
                            yaw = MathHelper.ToRadians(snappedDeg);
                        }

                        rot.Y = yaw;
                        break;
                    }

                case GizmoAxis.Z:
                    {
                        float roll = rot.Z + dx * radiansPerPixel;

                        if (_settings.EnableRotationSnap && !input.KeySnapToggle)
                        {
                            float step = _settings.RotationSnapDegrees;
                            if (step <= 0f)
                                step = 1f;

                            float deg = MathHelper.ToDegrees(roll);
                            float snappedDeg = (float)Math.Round(deg / step) * step;
                            roll = MathHelper.ToRadians(snappedDeg);
                        }

                        rot.Z = roll;
                        break;
                    }
            }

            entity.Rotation = rot;
        }

        private GizmoAxis HitTestRotationAxis(
            Point mouse,
            Vector3 center,
            float radius,
            float thresholdPixels = 12f)
        {
            var viewport = _graphicsDevice.Viewport;
            var mousePos = mouse.ToVector2();

            const int segments = 64;
            float step = MathHelper.TwoPi / segments;

            GizmoAxis bestAxis = GizmoAxis.None;
            float bestDist = thresholdPixels;

            void TestAxis(GizmoAxis axis, Func<float, Vector3> pointOnRing)
            {
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * step;
                    var worldPos = pointOnRing(angle);
                    var screenPos = viewport.Project(worldPos, _camera.ProjectionMatrix, _camera.ViewMatrix, Matrix.Identity);
                    var ringPoint = new Vector2(screenPos.X, screenPos.Y);

                    float dist = Vector2.Distance(mousePos, ringPoint);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestAxis = axis;
                    }
                }
            }

            // X ring (YZ plane)
            TestAxis(GizmoAxis.X, a =>
            {
                float s = (float)Math.Sin(a);
                float c = (float)Math.Cos(a);
                return center + new Vector3(0f, c * radius, s * radius);
            });

            // Y ring (XZ plane)
            TestAxis(GizmoAxis.Y, a =>
            {
                float s = (float)Math.Sin(a);
                float c = (float)Math.Cos(a);
                return center + new Vector3(c * radius, 0f, s * radius);
            });

            // Z ring (XY plane)
            TestAxis(GizmoAxis.Z, a =>
            {
                float s = (float)Math.Sin(a);
                float c = (float)Math.Cos(a);
                return center + new Vector3(c * radius, s * radius, 0f);
            });

            return bestAxis;
        }

        // ------------------------------------------------------------
        // TRANSLATION
        // ------------------------------------------------------------

        private void HandleTranslationDrag(EditorInputSnapshot input, EntityInfo entity, Vector3 center)
        {
            var currentWorld = UnprojectOnHorizontalPlane(input.MousePosition, center.Y);
            var moveDelta = currentWorld - _dragStartWorld;

            switch (_activeAxis)
            {
                case GizmoAxis.X:
                    moveDelta = new Vector3(moveDelta.X, 0f, 0f);
                    break;
                case GizmoAxis.Y:
                    moveDelta = new Vector3(0f, moveDelta.Y, 0f);
                    break;
                case GizmoAxis.Z:
                    moveDelta = new Vector3(0f, 0f, moveDelta.Z);
                    break;
            }

            if (moveDelta == Vector3.Zero)
                return;

            var targetPos = _entityStartPos + moveDelta;

            if (_settings.EnableGridSnap && !input.KeySnapToggle)
                targetPos = SnapToGrid(targetPos, _settings.GridSize);

            entity.Position = targetPos;
        }

        private Vector3 UnprojectOnHorizontalPlane(Point mouse, float planeY)
        {
            var ray = UnprojectRay(mouse);
            var plane = new Plane(Vector3.Up, -planeY);

            float? dist = ray.Intersects(plane);
            if (!dist.HasValue)
                return _selectedEntity?.Position ?? Vector3.Zero;

            return ray.Position + ray.Direction * dist.Value;
        }

        private Ray UnprojectRay(Point mouse)
        {
            var viewport = _graphicsDevice.Viewport;

            Vector3 nearSource = new Vector3(mouse.X, mouse.Y, 0f);
            Vector3 farSource = new Vector3(mouse.X, mouse.Y, 1f);

            Vector3 nearPoint = viewport.Unproject(nearSource, _camera.ProjectionMatrix, _camera.ViewMatrix, Matrix.Identity);
            Vector3 farPoint = viewport.Unproject(farSource, _camera.ProjectionMatrix, _camera.ViewMatrix, Matrix.Identity);

            Vector3 direction = Vector3.Normalize(farPoint - nearPoint);
            return new Ray(nearPoint, direction);
        }

        private static Vector3 SnapToGrid(Vector3 position, Vector3 gridSize)
        {
            const float epsilon = 0.0001f;

            float gx = Math.Abs(gridSize.X) <= epsilon ? 0f : gridSize.X;
            float gy = Math.Abs(gridSize.Y) <= epsilon ? 0f : gridSize.Y;
            float gz = Math.Abs(gridSize.Z) <= epsilon ? 0f : gridSize.Z;

            var result = position;

            if (gx > epsilon)
                result.X = (float)Math.Round(position.X / gx) * gx;
            if (gy > epsilon)
                result.Y = (float)Math.Round(position.Y / gy) * gy;
            if (gz > epsilon)
                result.Z = (float)Math.Round(position.Z / gz) * gz;

            return result;
        }

        // ------------------------------------------------------------
        // SCALE
        // ------------------------------------------------------------

        private void HandleScaleDrag(EditorInputSnapshot input, EntityInfo entity, Vector3 center)
        {
            var ray = UnprojectRay(input.MousePosition);

            if (_isUniformScaling)
            {
                HandleUniformScale(ray, entity, center, input);
            }
            else
            {
                HandleAxisScale(ray, entity, input);
            }
        }

        private void HandleUniformScale(Ray ray, EntityInfo entity, Vector3 center, EditorInputSnapshot input)
        {
            // Uniform scale: base it on radial distance from center
            float newRadius = (center - _entityStartPos).Length();
            float factor;

            if (_uniformStartRadius > 0.01f)
            {
                factor = newRadius / _uniformStartRadius;
            }
            else
            {
                // Fallback: use camera-right plane
                var camRight = Vector3.Normalize(_camera.Right);
                var plane = new Plane(camRight, -Vector3.Dot(camRight, _entityStartPos));
                float? dist = ray.Intersects(plane);
                if (!dist.HasValue)
                    return;

                Vector3 hit = ray.Position + ray.Direction * dist.Value;
                float newDist = Vector3.Dot(hit - _entityStartPos, camRight);
                float startDist = 1f; // avoid 0
                factor = newDist / startDist;
            }

            Vector3 targetScale = _entityStartScale * factor;

            if (_settings.EnableScaleSnap && !input.KeySnapToggle)
            {
                float snap = _settings.ScaleSnapStep;
                if (snap > 0.0001f)
                {
                    targetScale = new Vector3(
                        (float)Math.Round(targetScale.X / snap) * snap,
                        (float)Math.Round(targetScale.Y / snap) * snap,
                        (float)Math.Round(targetScale.Z / snap) * snap);
                }
            }

            entity.Scale = targetScale;
        }

        private void HandleAxisScale(Ray ray, EntityInfo entity, EditorInputSnapshot input)
        {
            Plane plane;
            Vector3 axis;

            switch (_activeAxis)
            {
                case GizmoAxis.X:
                    axis = Vector3.Right;
                    plane = new Plane(Vector3.Normalize(_camera.Right), -Vector3.Dot(Vector3.Normalize(_camera.Right), _entityStartPos));
                    break;
                case GizmoAxis.Y:
                    axis = Vector3.Up;
                    plane = new Plane(Vector3.Up, -Vector3.Dot(Vector3.Up, _entityStartPos));
                    break;
                case GizmoAxis.Z:
                    axis = Vector3.Backward;
                    plane = new Plane(Vector3.Normalize(_camera.Forward), -Vector3.Dot(Vector3.Normalize(_camera.Forward), _entityStartPos));
                    break;
                default:
                    return;
            }

            float? planeDist = ray.Intersects(plane);
            if (!planeDist.HasValue)
                return;

            Vector3 planeHit = ray.Position + ray.Direction * planeDist.Value;

            // Compare movement along axis starting from the original drag point
            Vector3 startToHit = planeHit - _dragStartWorld;
            float alongAxis = Vector3.Dot(startToHit, axis);
            float startScaleAxis = Vector3.Dot(_entityStartScale, axis);
            if (Math.Abs(startScaleAxis) < 0.0001f)
                startScaleAxis = 1f;

            float factorAxis = 1f + (alongAxis / (Math.Abs(startScaleAxis) * 2f));
            Vector3 targetScale = _entityStartScale;

            switch (_activeAxis)
            {
                case GizmoAxis.X:
                    targetScale.X = _entityStartScale.X * factorAxis;
                    break;
                case GizmoAxis.Y:
                    targetScale.Y = _entityStartScale.Y * factorAxis;
                    break;
                case GizmoAxis.Z:
                    targetScale.Z = _entityStartScale.Z * factorAxis;
                    break;
            }

            if (_settings.EnableScaleSnap && !input.KeySnapToggle)
            {
                float snap = _settings.ScaleSnapStep;
                if (snap > 0.0001f)
                {
                    targetScale = new Vector3(
                        (float)Math.Round(targetScale.X / snap) * snap,
                        (float)Math.Round(targetScale.Y / snap) * snap,
                        (float)Math.Round(targetScale.Z / snap) * snap);
                }
            }

            entity.Scale = targetScale;
        }

        // ------------------------------------------------------------
        // HIT TEST: TRANSLATION AXES
        // ------------------------------------------------------------


        private GizmoAxis HitTestAxis(Point mouse, Vector3 centerWorld, float size)
        {
            var viewport = _graphicsDevice.Viewport;
            var mousePos = mouse.ToVector2();

            // Build axis endpoints in world space
            Vector3 axisEndX = centerWorld + Vector3.Right * size;
            Vector3 axisEndY = centerWorld + Vector3.Up * size;
            Vector3 axisEndZ = centerWorld + Vector3.Backward * size;

            // Project to screen space
            var centerScreen = viewport.Project(centerWorld, _camera.ProjectionMatrix, _camera.ViewMatrix, Matrix.Identity);
            var endXScreen = viewport.Project(axisEndX, _camera.ProjectionMatrix, _camera.ViewMatrix, Matrix.Identity);
            var endYScreen = viewport.Project(axisEndY, _camera.ProjectionMatrix, _camera.ViewMatrix, Matrix.Identity);
            var endZScreen = viewport.Project(axisEndZ, _camera.ProjectionMatrix, _camera.ViewMatrix, Matrix.Identity);

            var centerPos = new Vector2(centerScreen.X, centerScreen.Y);
            var endXPos = new Vector2(endXScreen.X, endXScreen.Y);
            var endYPos = new Vector2(endYScreen.X, endYScreen.Y);
            var endZPos = new Vector2(endZScreen.X, endZScreen.Y);

            // Compute view-space depth for each axis (positive distance along the view direction)
            float DepthOf(Vector3 world)
            {
                var viewSpace = Vector3.Transform(world, _camera.ViewMatrix);
                // Points in front of the camera typically have negative Z in view space,
                // so flip the sign so that "smaller" means closer to the camera.
                return -viewSpace.Z;
            }

            _axisDepthX = Math.Min(DepthOf(centerWorld), DepthOf(axisEndX));
            _axisDepthY = Math.Min(DepthOf(centerWorld), DepthOf(axisEndY));
            _axisDepthZ = Math.Min(DepthOf(centerWorld), DepthOf(axisEndZ));

            float distX = DistanceToSegment(mousePos, centerPos, endXPos);
            float distY = DistanceToSegment(mousePos, centerPos, endYPos);
            float distZ = DistanceToSegment(mousePos, centerPos, endZPos);

            GizmoAxis bestAxis = GizmoAxis.None;
            float bestDist = AxisHitThresholdPixels;
            float bestDepth = float.MaxValue;

            void ConsiderCandidate(GizmoAxis axis, float dist, float depth)
            {
                if (dist > AxisHitThresholdPixels)
                    return;

                // Prefer smaller screen distance; when similar, prefer the one closer to the camera.
                const float distanceEpsilon = 0.5f;

                if (dist < bestDist - distanceEpsilon ||
                    (Math.Abs(dist - bestDist) <= distanceEpsilon && depth < bestDepth))
                {
                    bestAxis = axis;
                    bestDist = dist;
                    bestDepth = depth;
                }
            }

            ConsiderCandidate(GizmoAxis.X, distX, _axisDepthX);
            ConsiderCandidate(GizmoAxis.Y, distY, _axisDepthY);
            ConsiderCandidate(GizmoAxis.Z, distZ, _axisDepthZ);

            return bestAxis;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var ap = p - a;
            float abLenSq = Vector2.Dot(ab, ab);
            if (abLenSq <= float.Epsilon)
                return ap.Length();

            float t = MathHelper.Clamp(Vector2.Dot(ap, ab) / abLenSq, 0f, 1f);
            var closest = a + ab * t;
            return Vector2.Distance(p, closest);
        }

        // ------------------------------------------------------------
        // DRAW
        // ------------------------------------------------------------

        public void Draw(BasicEffect effect)
        {
            if (_selectedEntity == null || !_settings.ShowSelectionGizmo)
                return;

            var center = _selectedEntity.Position;
            const float size = 0.75f;

            var device = _graphicsDevice;
            var prevDepth = device.DepthStencilState;
            device.DepthStencilState = DepthStencilState.None;

            try
            {
                // Axis lines
                DrawAxisCross(effect, center, size);

                // Rotation rings
                DrawRotationRing(effect, center, size * 1.2f);

                // Uniform scale handle
                DrawUniformScaleHandle(effect, center, size * 0.3f);

                // Selection bounding box (uses entity Size if non-zero)
                DrawSelectionBounds(effect);
            }
            finally
            {
                device.DepthStencilState = prevDepth;
            }
        }


        /// <summary>
        /// Computes the display color for a gizmo axis, including hover / active highlighting.
        /// </summary>
        private Color GetAxisColor(GizmoAxis axis, Color baseColor)
        {
            bool isActive = (_activeAxis == axis);
            bool isHover = (!isActive && _hoverAxis == axis);

            if (isActive)
            {
                // Strong highlight for the axis currently being manipulated
                return BrightenColor(baseColor, 0.65f);
            }

            if (isHover)
            {
                // Softer highlight when merely hovered
                return BrightenColor(baseColor, 0.35f);
            }

            return baseColor;
        }

        /// <summary>
        /// Computes the color for rotation rings, giving special emphasis to the active rotation axis.
        /// </summary>
        private Color GetRingColor(GizmoAxis axis, Color baseColor)
        {
            if (_isRotating && _activeAxis == axis)
            {
                // When rotating around this axis, make the ring much brighter
                return BrightenColor(baseColor, 0.75f);
            }

            return baseColor;
        }

        /// <summary>
        /// Returns a brighter version of the given color by lerping towards white.
        /// factor should be in [0,1].
        /// </summary>
        private static Color BrightenColor(Color baseColor, float factor)
        {
            factor = MathHelper.Clamp(factor, 0f, 1f);

            byte r = (byte)(baseColor.R + (255 - baseColor.R) * factor);
            byte g = (byte)(baseColor.G + (255 - baseColor.G) * factor);
            byte b = (byte)(baseColor.B + (255 - baseColor.B) * factor);

            return new Color(r, g, b, baseColor.A);
        }

        private void DrawAxisCross(BasicEffect effect, Vector3 center, float size)
        {
            int vertexCount = 0;
            int indexCount = 0;

            void AddLine(Vector3 start, Vector3 end, Color color)
            {
                short baseIndex = (short)vertexCount;
                _lineVertices[vertexCount++] = new VertexPositionColor(start, color);
                _lineVertices[vertexCount++] = new VertexPositionColor(end, color);
                _lineIndices[indexCount++] = baseIndex;
                _lineIndices[indexCount++] = (short)(baseIndex + 1);
            }

            // Compute per-axis colors with hover/active highlighting
            Color colorX = GetAxisColor(GizmoAxis.X, Color.Red);
            Color colorY = GetAxisColor(GizmoAxis.Y, Color.LimeGreen);
            Color colorZ = GetAxisColor(GizmoAxis.Z, Color.Blue);

            // Axis body lines
            Vector3 xEnd = center + Vector3.Right * size;
            Vector3 yEnd = center + Vector3.Up * size;
            Vector3 zEnd = center + Vector3.Backward * size;

            AddLine(center, xEnd, colorX);
            AddLine(center, yEnd, colorY);
            AddLine(center, zEnd, colorZ);

            // Add simple arrow heads for better visual affordance
            float arrowLength = size * 0.20f;
            float arrowWidth = size * 0.10f;

            // X axis arrow (in the XY plane)
            Vector3 xBase1 = xEnd + new Vector3(-arrowLength, arrowWidth, 0f);
            Vector3 xBase2 = xEnd + new Vector3(-arrowLength, -arrowWidth, 0f);
            AddLine(xEnd, xBase1, colorX);
            AddLine(xEnd, xBase2, colorX);
            AddLine(xBase1, xBase2, colorX);

            // Y axis arrow (in the YZ plane)
            Vector3 yBase1 = yEnd + new Vector3(0f, -arrowLength, arrowWidth);
            Vector3 yBase2 = yEnd + new Vector3(0f, -arrowLength, -arrowWidth);
            AddLine(yEnd, yBase1, colorY);
            AddLine(yEnd, yBase2, colorY);
            AddLine(yBase1, yBase2, colorY);

            // Z axis arrow (in the ZX plane)
            Vector3 zBase1 = zEnd + new Vector3(arrowWidth, 0f, arrowLength);
            Vector3 zBase2 = zEnd + new Vector3(-arrowWidth, 0f, arrowLength);
            AddLine(zEnd, zBase1, colorZ);
            AddLine(zEnd, zBase2, colorZ);
            AddLine(zBase1, zBase2, colorZ);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.LineList,
                    _lineVertices,
                    0,
                    vertexCount,
                    _lineIndices,
                    0,
                    indexCount / 2);
            }
        }

        private void DrawRotationRing(BasicEffect effect, Vector3 center, float radius)
        {
            int vertexCount = 0;
            int indexCount = 0;

            void AddLine(Vector3 start, Vector3 end, Color color)
            {
                short baseIndex = (short)vertexCount;
                _lineVertices[vertexCount++] = new VertexPositionColor(start, color);
                _lineVertices[vertexCount++] = new VertexPositionColor(end, color);
                _lineIndices[indexCount++] = baseIndex;
                _lineIndices[indexCount++] = (short)(baseIndex + 1);
            }

            const int segments = 64;
            float step = MathHelper.TwoPi / segments;

            // Choose base colors and apply selection highlighting
            Color colorX = GetRingColor(GizmoAxis.X, Color.Red);
            Color colorY = GetRingColor(GizmoAxis.Y, Color.LimeGreen);
            Color colorZ = GetRingColor(GizmoAxis.Z, Color.Blue);

            // Small radius offset used to fake thicker rings for the active axis
            float selectedOffset = radius * 0.03f;

            // X ring (YZ plane)
            bool xSelected = _isRotating && _activeAxis == GizmoAxis.X;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * step;
                float a1 = (i + 1) * step;

                float c0 = (float)Math.Cos(a0);
                float s0 = (float)Math.Sin(a0);
                float c1 = (float)Math.Cos(a1);
                float s1 = (float)Math.Sin(a1);

                var p0 = center + new Vector3(0f, c0 * radius, s0 * radius);
                var p1 = center + new Vector3(0f, c1 * radius, s1 * radius);
                AddLine(p0, p1, colorX);

                if (xSelected)
                {
                    // Second ring slightly offset outwards to give a thicker look
                    var p0b = center + new Vector3(0f, c0 * (radius + selectedOffset), s0 * (radius + selectedOffset));
                    var p1b = center + new Vector3(0f, c1 * (radius + selectedOffset), s1 * (radius + selectedOffset));
                    AddLine(p0b, p1b, colorX);
                }
            }

            // Y ring (XZ plane)
            bool ySelected = _isRotating && _activeAxis == GizmoAxis.Y;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * step;
                float a1 = (i + 1) * step;

                float c0 = (float)Math.Cos(a0);
                float s0 = (float)Math.Sin(a0);
                float c1 = (float)Math.Cos(a1);
                float s1 = (float)Math.Sin(a1);

                var p0 = center + new Vector3(c0 * radius, 0f, s0 * radius);
                var p1 = center + new Vector3(c1 * radius, 0f, s1 * radius);
                AddLine(p0, p1, colorY);

                if (ySelected)
                {
                    var p0b = center + new Vector3(c0 * (radius + selectedOffset), 0f, s0 * (radius + selectedOffset));
                    var p1b = center + new Vector3(c1 * (radius + selectedOffset), 0f, s1 * (radius + selectedOffset));
                    AddLine(p0b, p1b, colorY);
                }
            }

            // Z ring (XY plane)
            bool zSelected = _isRotating && _activeAxis == GizmoAxis.Z;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * step;
                float a1 = (i + 1) * step;

                float c0 = (float)Math.Cos(a0);
                float s0 = (float)Math.Sin(a0);
                float c1 = (float)Math.Cos(a1);
                float s1 = (float)Math.Sin(a1);

                var p0 = center + new Vector3(c0 * radius, s0 * radius, 0f);
                var p1 = center + new Vector3(c1 * radius, s1 * radius, 0f);
                AddLine(p0, p1, colorZ);

                if (zSelected)
                {
                    var p0b = center + new Vector3(c0 * (radius + selectedOffset), s0 * (radius + selectedOffset), 0f);
                    var p1b = center + new Vector3(c1 * (radius + selectedOffset), s1 * (radius + selectedOffset), 0f);
                    AddLine(p0b, p1b, colorZ);
                }
            }

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.LineList,
                    _lineVertices,
                    0,
                    vertexCount,
                    _lineIndices,
                    0,
                    indexCount / 2);
            }
        }

        private void DrawUniformScaleHandle(BasicEffect effect, Vector3 center, float radius)
        {
            int vertexCount = 0;
            int indexCount = 0;

            void AddLine(Vector3 start, Vector3 end, Color color)
            {
                short baseIndex = (short)vertexCount;
                _lineVertices[vertexCount++] = new VertexPositionColor(start, color);
                _lineVertices[vertexCount++] = new VertexPositionColor(end, color);
                _lineIndices[indexCount++] = baseIndex;
                _lineIndices[indexCount++] = (short)(baseIndex + 1);
            }

            const int segments = 32;
            float step = MathHelper.TwoPi / segments;

            for (int i = 0; i < segments; i++)
            {
                float a0 = i * step;
                float a1 = (i + 1) * step;
                var p0 = center + new Vector3((float)Math.Cos(a0) * radius, (float)Math.Sin(a0) * radius, 0f);
                var p1 = center + new Vector3((float)Math.Cos(a1) * radius, (float)Math.Sin(a1) * radius, 0f);
                AddLine(p0, p1, Color.White);
            }

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.LineList,
                    _lineVertices,
                    0,
                    vertexCount,
                    _lineIndices,
                    0,
                    indexCount / 2);
            }
        }

        // ------------------------------------------------------------
        // SELECTION BOUNDING BOX
        // ------------------------------------------------------------

        private void DrawSelectionBounds(BasicEffect effect)
        {
            if (_selectedEntity == null)
                return;

            // If Size is zero or near-zero, skip drawing.
            var size = _selectedEntity.Size;
            if (size.X == 0f && size.Y == 0f && size.Z == 0f)
                return;

            var center = _selectedEntity.Position;
            var half = size * 0.5f;

            var min = center - half;
            var max = center + half;

            // 8 corners of the AABB
            Vector3 c000 = new Vector3(min.X, min.Y, min.Z);
            Vector3 c001 = new Vector3(min.X, min.Y, max.Z);
            Vector3 c010 = new Vector3(min.X, max.Y, min.Z);
            Vector3 c011 = new Vector3(min.X, max.Y, max.Z);
            Vector3 c100 = new Vector3(max.X, min.Y, min.Z);
            Vector3 c101 = new Vector3(max.X, min.Y, max.Z);
            Vector3 c110 = new Vector3(max.X, max.Y, min.Z);
            Vector3 c111 = new Vector3(max.X, max.Y, max.Z);

            int vertexCount = 0;
            int indexCount = 0;

            void AddLine(Vector3 start, Vector3 end, Color color)
            {
                short baseIndex = (short)vertexCount;
                _lineVertices[vertexCount++] = new VertexPositionColor(start, color);
                _lineVertices[vertexCount++] = new VertexPositionColor(end, color);
                _lineIndices[indexCount++] = baseIndex;
                _lineIndices[indexCount++] = (short)(baseIndex + 1);
            }

            Color boxColor = Color.Yellow;

            // Bottom rectangle
            AddLine(c000, c001, boxColor);
            AddLine(c001, c101, boxColor);
            AddLine(c101, c100, boxColor);
            AddLine(c100, c000, boxColor);

            // Top rectangle
            AddLine(c010, c011, boxColor);
            AddLine(c011, c111, boxColor);
            AddLine(c111, c110, boxColor);
            AddLine(c110, c010, boxColor);

            // Vertical edges
            AddLine(c000, c010, boxColor);
            AddLine(c001, c011, boxColor);
            AddLine(c100, c110, boxColor);
            AddLine(c101, c111, boxColor);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.LineList,
                    _lineVertices,
                    0,
                    vertexCount,
                    _lineIndices,
                    0,
                    indexCount / 2);
            }
        }
    }
}