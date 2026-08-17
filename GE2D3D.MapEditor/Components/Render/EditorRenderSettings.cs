using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Microsoft.Xna.Framework;
using GE2D3D.MapEditor.Data;

namespace GE2D3D.MapEditor.Components.Render
{
    /// <summary>
    /// Central editor configuration for what the renderer draws.
    /// These flags are intended to be bound to WPF UI (checkboxes, menu items, toolbar),
    /// and queried from the MonoGame render / gizmo code.
    /// </summary>
    public class EditorRenderSettings : INotifyPropertyChanged
    {
        // -------------------------------
        // Layer visibility
        // -------------------------------
        private bool _showTerrain = true;
        public bool ShowTerrain
        {
            get => _showTerrain;
            set
            {
                if (value == _showTerrain) return;
                _showTerrain = value;
                OnPropertyChanged();
            }
        }

        private bool _showProps = true;
        public bool ShowProps
        {
            get => _showProps;
            set
            {
                if (value == _showProps) return;
                _showProps = value;
                OnPropertyChanged();
            }
        }

        private bool _showCollision = false;
        public bool ShowCollision
        {
            get => _showCollision;
            set
            {
                if (value == _showCollision) return;
                _showCollision = value;
                OnPropertyChanged();
            }
        }

        private bool _showLights = true;
        public bool ShowLights
        {
            get => _showLights;
            set
            {
                if (value == _showLights) return;
                _showLights = value;
                OnPropertyChanged();
            }
        }

        private bool _showTriggers = true;
        public bool ShowTriggers
        {
            get => _showTriggers;
            set
            {
                if (value == _showTriggers) return;
                _showTriggers = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Master toggle for generic "volumes" (trigger volumes, etc).
        /// Used by Level.Draw(...) as a coarse switch.
        /// </summary>
        private bool _showVolumes = true;
        public bool ShowVolumes
        {
            get => _showVolumes;
            set
            {
                if (value == _showVolumes) return;
                _showVolumes = value;
                OnPropertyChanged();
            }
        }

        // -------------------------------
        // Gizmos / debug elements
        // -------------------------------
        private bool _showGrid = true;
        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                if (value == _showGrid) return;
                _showGrid = value;
                OnPropertyChanged();
            }
        }

        private bool _showSelectionGizmo = true;
        public bool ShowSelectionGizmo
        {
            get => _showSelectionGizmo;
            set
            {
                if (value == _showSelectionGizmo) return;
                _showSelectionGizmo = value;
                OnPropertyChanged();
            }
        }

        private bool _showLightVolumes = true;
        public bool ShowLightVolumes
        {
            get => _showLightVolumes;
            set
            {
                if (value == _showLightVolumes) return;
                _showLightVolumes = value;
                OnPropertyChanged();
            }
        }


        private bool _showTriggerVolumes = true;
        public bool ShowTriggerVolumes
        {
            get => _showTriggerVolumes;
            set
            {
                if (value == _showTriggerVolumes) return;
                _showTriggerVolumes = value;
                OnPropertyChanged();
            }
        }


        // -------------------------------
        // Scene view mode & environment
        // -------------------------------

        /// <summary>
        /// Render mode for the main scene view.
        /// Shaded is the default; other modes are debug / visualization helpers.
        /// </summary>
        public enum EditorViewMode
        {
            Shaded = 0,
            Unlit = 1,
            Wireframe = 2,
            CollisionOnly = 3,
            TriggerOnly = 4
        }

        private EditorViewMode _viewMode = EditorViewMode.Shaded;
        public EditorViewMode ViewMode
        {
            get => _viewMode;
            set
            {
                if (value == _viewMode) return;
                _viewMode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Additional ambient light multiplier applied on top of level lighting.
        /// 1.0 keeps the default look; lower = darker, higher = brighter.
        /// </summary>
        private float _ambientIntensity = 1.0f;
        public float AmbientIntensity
        {
            get => _ambientIntensity;
            set
            {
                if (Math.Abs(value - _ambientIntensity) < 0.0001f) return;
                _ambientIntensity = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Optional fog toggle and parameters used by the renderer.
        /// </summary>
        private bool _enableFog;
        public bool EnableFog
        {
            get => _enableFog;
            set
            {
                if (value == _enableFog) return;
                _enableFog = value;
                OnPropertyChanged();
            }
        }

        private Color _fogColor = Color.CornflowerBlue;
        public Color FogColor
        {
            get => _fogColor;
            set
            {
                if (value == _fogColor) return;
                _fogColor = value;
                OnPropertyChanged();
            }
        }

        private float _fogStart = 32f;
        public float FogStart
        {
            get => _fogStart;
            set
            {
                if (Math.Abs(value - _fogStart) < 0.0001f) return;
                _fogStart = value;
                OnPropertyChanged();
            }
        }

        private float _fogEnd = 256f;
        public float FogEnd
        {
            get => _fogEnd;
            set
            {
                if (Math.Abs(value - _fogEnd) < 0.0001f) return;
                _fogEnd = value;
                OnPropertyChanged();
            }
        }

        // -------------------------------
        // Advanced render toggles (Track H)
        // -------------------------------

        private bool _enableShadows;
        /// <summary>
        /// Enables simple distance-based shadow/fog behaviour in the renderer.
        /// This is a coarse toggle; the exact implementation lives in Render.cs.
        /// </summary>
        public bool EnableShadows
        {
            get => _enableShadows;
            set
            {
                if (value == _enableShadows) return;
                _enableShadows = value;
                OnPropertyChanged();
            }
        }

        private bool _enableBloom;
        /// <summary>
        /// When true, the renderer slightly boosts ambient light to simulate bloom.
        /// This is a stylistic post-process toggle, not a full HDR pipeline.
        /// </summary>
        public bool EnableBloom
        {
            get => _enableBloom;
            set
            {
                if (value == _enableBloom) return;
                _enableBloom = value;
                OnPropertyChanged();
            }
        }

        private bool _enableSsao;
        /// <summary>
        /// When true, the renderer darkens ambient light slightly to simulate SSAO.
        /// This is a cheap approximation applied in directional lighting.
        /// </summary>
        public bool EnableSsao
        {
            get => _enableSsao;
            set
            {
                if (value == _enableSsao) return;
                _enableSsao = value;
                OnPropertyChanged();
            }
        }

        private bool _enableDepthOfField;
        /// <summary>
        /// Enables distance-based fog to approximate depth-of-field feel.
        /// Implemented via BasicEffect.Fog in Render.cs.
        /// </summary>
        public bool EnableDepthOfField
        {
            get => _enableDepthOfField;
            set
            {
                if (value == _enableDepthOfField) return;
                _enableDepthOfField = value;
                OnPropertyChanged();
            }
        }


        // -------------------------------
        // Lighting / Environment
        // -------------------------------
        private bool _enableLighting = true;
        public bool EnableLighting
        {
            get => _enableLighting;
            set
            {
                if (value == _enableLighting) return;
                _enableLighting = value;
                OnPropertyChanged();
            }
        }

        private bool _showSkybox = false;
        public bool ShowSkybox
        {
            get => _showSkybox;
            set
            {
                if (value == _showSkybox) return;
                _showSkybox = value;
                OnPropertyChanged();
            }
        }

        private AntiAliasing _antiAliasing = AntiAliasing.MSAA;
        public AntiAliasing AntiAliasing
        {
            get => _antiAliasing;
            set
            {
                if (value == _antiAliasing) return;
                _antiAliasing = value;
                OnPropertyChanged();
            }
        }

        // -------------------------------
        // Snapping
        // -------------------------------
        private bool _enableGridSnap = true;
        public bool EnableGridSnap
        {
            get => _enableGridSnap;
            set
            {
                if (value == _enableGridSnap) return;
                _enableGridSnap = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Backwards-compat alias (older code may still refer to UseGridSnap).
        /// </summary>
        public bool UseGridSnap
        {
            get => EnableGridSnap;
            set => EnableGridSnap = value;
        }

        /// <summary>
        /// The snap spacing for Position X/Y/Z movement.
        /// </summary>
        private Vector3 _gridSize = new Vector3(1f, 1f, 1f);
        public Vector3 GridSize
        {
            get => _gridSize;
            set
            {
                if (value == _gridSize) return;
                _gridSize = value;
                OnPropertyChanged();
            }
        }

        private bool _enableRotationSnap = false;
        public bool EnableRotationSnap
        {
            get => _enableRotationSnap;
            set
            {
                if (value == _enableRotationSnap) return;
                _enableRotationSnap = value;
                OnPropertyChanged();
            }
        }

        private float _rotationSnapDegrees = 15f;
        /// <summary>
        /// Rotation snap step in degrees (for yaw/pitch/roll).
        /// </summary>
        public float RotationSnapDegrees
        {
            get => _rotationSnapDegrees;
            set
            {
                if (Math.Abs(value - _rotationSnapDegrees) < float.Epsilon) return;
                _rotationSnapDegrees = value;
                OnPropertyChanged();
            }
        }

        private bool _enableScaleSnap = false;
        public bool EnableScaleSnap
        {
            get => _enableScaleSnap;
            set
            {
                if (value == _enableScaleSnap) return;
                _enableScaleSnap = value;
                OnPropertyChanged();
            }
        }

        private float _scaleSnapStep = 0.1f;
        /// <summary>
        /// Uniform scale snap step.
        /// </summary>
        public float ScaleSnapStep
        {
            get => _scaleSnapStep;
            set
            {
                if (Math.Abs(value - _scaleSnapStep) < float.Epsilon) return;
                _scaleSnapStep = value;
                OnPropertyChanged();
            }
        }

        // -------------------------------
        // Camera tools
        // -------------------------------
        private bool _showCameraPath = true;
        public bool ShowCameraPath
        {
            get => _showCameraPath;
            set
            {
                if (value == _showCameraPath) return;
                _showCameraPath = value;
                OnPropertyChanged();
            }
        }

        // -------------------------------
        // Selection (for highlight / gizmos)
        // -------------------------------
        private EntityInfo? _selectedEntity;
        public EntityInfo? SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                if (ReferenceEquals(value, _selectedEntity)) return;
                _selectedEntity = value;
                OnPropertyChanged();
            }
        }

        // -------------------------------
        // Snapping helpers (for gizmos)
        // -------------------------------

        /// <summary>
        /// Apply grid snapping to a position if EnableGridSnap is on.
        /// Called from gizmo / drag handlers in MonoGame.
        /// </summary>
        public Vector3 ApplyPositionSnap(Vector3 position)
        {
            if (!EnableGridSnap)
                return position;

            Vector3 step = GridSize;

            float Snap(float v, float s)
            {
                if (s <= 0.0001f) return v;
                return (float)Math.Round(v / s) * s;
            }

            return new Vector3(
                Snap(position.X, step.X),
                Snap(position.Y, step.Y),
                Snap(position.Z, step.Z));
        }

        /// <summary>
        /// Apply rotation snapping in degrees if EnableRotationSnap is on.
        /// </summary>
        public float ApplyRotationSnap(float angleDegrees)
        {
            if (!EnableRotationSnap || RotationSnapDegrees <= 0.0001f)
                return angleDegrees;

            return (float)Math.Round(angleDegrees / RotationSnapDegrees) * RotationSnapDegrees;
        }

        /// <summary>
        /// Apply uniform scale snapping if EnableScaleSnap is on.
        /// </summary>
        public float ApplyScaleSnap(float scale)
        {
            if (!EnableScaleSnap || ScaleSnapStep <= 0.0001f)
                return scale;

            return (float)Math.Round(scale / ScaleSnapStep) * ScaleSnapStep;
        }

        // -------------------------------
        // INotifyPropertyChanged
        // -------------------------------
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}