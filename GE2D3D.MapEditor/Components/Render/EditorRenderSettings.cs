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