using GE2D3D.MapEditor.Components.Camera;
using GE2D3D.MapEditor.Components.Tools;
using GE2D3D.MapEditor.Data;
using GE2D3D.MapEditor.Modules.SceneViewer.Views;
using GE2D3D.MapEditor.Properties;
using GE2D3D.MapEditor.Utils;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace GE2D3D.MapEditor.Modules.SceneViewer.ViewModels
{
    /// <summary>
    /// Prism scene document/view model.
    /// </summary>
    public class SceneViewModel : BindableBase, INotifyPropertyChanged
    {
        // --------------------------------------------------------------------
        // Core document / view references
        // --------------------------------------------------------------------

        private SceneView? _sceneView;
        private readonly DispatcherTimer _selectionTimer;

        private string? _filePath;
        private string? _displayName;
        private string _originalText = string.Empty;

        // Status bar fields (Track I)
        private string _statusCameraText = string.Empty;
        private string _statusSelectionText = string.Empty;
        private string _statusMessage = string.Empty;

        // Current active editor tool mode (Select / Place Prefab, etc.).
        private EditorToolMode _currentToolMode = EditorToolMode.Select;

        public string? FilePath
        {
            get => _filePath;
            private set
            {
                if (SetProperty(ref _filePath, value))
                    RaisePropertyChanged(nameof(FileName));
            }
        }

        public string? FileName =>
            string.IsNullOrWhiteSpace(FilePath)
                ? null
                : Path.GetFileName(FilePath);

        public string? DisplayName
        {
            get => _displayName;
            private set => SetProperty(ref _displayName, value);
        }

        /// <summary>
        /// Status text showing camera position and heading, used by the status bar.
        /// </summary>
        public string StatusCameraText
        {
            get => _statusCameraText;
            private set => SetProperty(ref _statusCameraText, value);
        }

        /// <summary>
        /// Status text describing the current selection (entity id/type).
        /// </summary>
        public string StatusSelectionText
        {
            get => _statusSelectionText;
            private set => SetProperty(ref _statusSelectionText, value);
        }

        /// <summary>
        /// General status / hint text area.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public LevelInfo? LevelInfo { get; private set; }

        private FileSystemWatcher? _mapWatcher;
        private string? _currentMapPath;

        // -------------------------------------------------------
        // Layer visibility flags for WPF binding
        // -------------------------------------------------------

        private bool _showGeometry = true;
        private bool _showProps = true;
        private bool _showCollision = true;
        private bool _showLights = true;
        private bool _showTriggers = true;
        private bool _showGrid = true;

        public bool ShowGeometry
        {
            get => _showGeometry;
            set { if (SetProperty(ref _showGeometry, value)) PushLayerStateToRender(); }
        }

        public bool ShowProps
        {
            get => _showProps;
            set { if (SetProperty(ref _showProps, value)) PushLayerStateToRender(); }
        }

        public bool ShowCollision
        {
            get => _showCollision;
            set { if (SetProperty(ref _showCollision, value)) PushLayerStateToRender(); }
        }

        public bool ShowLights
        {
            get => _showLights;
            set { if (SetProperty(ref _showLights, value)) PushLayerStateToRender(); }
        }

        public bool ShowTriggers
        {
            get => _showTriggers;
            set { if (SetProperty(ref _showTriggers, value)) PushLayerStateToRender(); }
        }

        public bool ShowGrid
        {
            get => _showGrid;
            set { if (SetProperty(ref _showGrid, value)) PushLayerStateToRender(); }
        }

        // -------------------------------------------------------
        // Grid + rotation snapping
        // -------------------------------------------------------

        private bool _useGridSnap;
        public bool UseGridSnap
        {
            get => _useGridSnap;
            set { if (SetProperty(ref _useGridSnap, value)) PushGridSnapStateToRender(); }
        }

        private float _gridSize = 1.0f;
        public float GridSize
        {
            get => _gridSize;
            set { if (SetProperty(ref _gridSize, value)) PushGridSnapStateToRender(); }
        }

        private bool _rotationSnapEnabled;
        public bool RotationSnapEnabled
        {
            get => _rotationSnapEnabled;
            set { if (SetProperty(ref _rotationSnapEnabled, value)) PushRotationSnapStateToRender(); }
        }

        private float _rotationSnapStep = 15.0f;
        public float RotationSnapStep
        {
            get => _rotationSnapStep;
            set { if (SetProperty(ref _rotationSnapStep, value)) PushRotationSnapStateToRender(); }
        }

        // -----------------------------------------
        // Simple transform undo/redo (position + size + rotation, inspector-driven)
        // -----------------------------------------

        private struct TransformUndoItem
        {
            public EntityInfo Entity;
            public Vector3 OldPosition;
            public Vector3 OldSize;
            public Vector3 OldRotation;
            public Vector3 NewPosition;
            public Vector3 NewSize;
            public Vector3 NewRotation;
        }

        private readonly Stack<TransformUndoItem> _undoStack = new Stack<TransformUndoItem>();
        private readonly Stack<TransformUndoItem> _redoStack = new Stack<TransformUndoItem>();
        private bool _isUndoRedoActive;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        // -----------------------------------------
        // Selection inspector backing fields
        // -----------------------------------------

        private string? _selectedEntityId;
        private int _selectedEntityNumericId;
        private string? _selectedEntityKind;
        private string? _selectedEntityPosition;
        private string? _selectedEntitySize;
        private string? _selectedEntityRotation;

        private float _selectedEntityRotX;
        private float _selectedEntityRotY;
        private float _selectedEntityRotZ;

        public string? SelectedEntityId
        {
            get => _selectedEntityId;
            private set => SetProperty(ref _selectedEntityId, value);
        }

        public int SelectedEntityNumericId
        {
            get => _selectedEntityNumericId;
            private set => SetProperty(ref _selectedEntityNumericId, value);
        }

        public string? SelectedEntityKind
        {
            get => _selectedEntityKind;
            private set => SetProperty(ref _selectedEntityKind, value);
        }

        public string? SelectedEntityPosition
        {
            get => _selectedEntityPosition;
            private set => SetProperty(ref _selectedEntityPosition, value);
        }

        public string? SelectedEntitySize
        {
            get => _selectedEntitySize;
            private set => SetProperty(ref _selectedEntitySize, value);
        }

        public string? SelectedEntityRotation
        {
            get => _selectedEntityRotation;
            private set => SetProperty(ref _selectedEntityRotation, value);
        }

        public float SelectedEntityRotX
        {
            get => _selectedEntityRotX;
            set => SetProperty(ref _selectedEntityRotX, value);
        }

        public float SelectedEntityRotY
        {
            get => _selectedEntityRotY;
            set => SetProperty(ref _selectedEntityRotY, value);
        }

        public float SelectedEntityRotZ
        {
            get => _selectedEntityRotZ;
            set => SetProperty(ref _selectedEntityRotZ, value);
        }

        /// <summary>
        /// Light-specific inspector fields (for entities classified as 'Light').
        /// </summary>
        private int _selectedLightColorR;
        private int _selectedLightColorG;
        private int _selectedLightColorB;
        private float _selectedLightRadius;

        public int SelectedLightColorR
        {
            get => _selectedLightColorR;
            set => SetProperty(ref _selectedLightColorR, value);
        }

        public int SelectedLightColorG
        {
            get => _selectedLightColorG;
            set => SetProperty(ref _selectedLightColorG, value);
        }

        public int SelectedLightColorB
        {
            get => _selectedLightColorB;
            set => SetProperty(ref _selectedLightColorB, value);
        }

        public float SelectedLightRadius
        {
            get => _selectedLightRadius;
            set => SetProperty(ref _selectedLightRadius, value);
        }

        // Editable position components for the selected entity.
        private float _selectedEntityPosX;
        private float _selectedEntityPosY;
        private float _selectedEntityPosZ;

        public float SelectedEntityPosX
        {
            get => _selectedEntityPosX;
            set => SetProperty(ref _selectedEntityPosX, value);
        }

        public float SelectedEntityPosY
        {
            get => _selectedEntityPosY;
            set => SetProperty(ref _selectedEntityPosY, value);
        }

        public float SelectedEntityPosZ
        {
            get => _selectedEntityPosZ;
            set => SetProperty(ref _selectedEntityPosZ, value);
        }

        // Editable size components for the selected entity (if applicable).
        private float _selectedEntitySizeX;
        private float _selectedEntitySizeY;
        private float _selectedEntitySizeZ;

        public float SelectedEntitySizeX
        {
            get => _selectedEntitySizeX;
            set => SetProperty(ref _selectedEntitySizeX, value);
        }

        public float SelectedEntitySizeY
        {
            get => _selectedEntitySizeY;
            set => SetProperty(ref _selectedEntitySizeY, value);
        }

        public float SelectedEntitySizeZ
        {
            get => _selectedEntitySizeZ;
            set => SetProperty(ref _selectedEntitySizeZ, value);
        }

        // -------------------------------------------------------
        // Skybox / Environment
        // -------------------------------------------------------

        private bool _skyboxEnabled;
        public bool SkyboxEnabled
        {
            get => _skyboxEnabled;
            set
            {
                if (SetProperty(ref _skyboxEnabled, value))
                {
                    PushSkyboxStateToRender();
                }
            }
        }

        private string? _skyboxInnerTexturePath;
        public string? SkyboxInnerTexturePath
        {
            get => _skyboxInnerTexturePath;
            set
            {
                if (SetProperty(ref _skyboxInnerTexturePath, value))
                {
                    PushSkyboxStateToRender();
                }
            }
        }

        private string? _skyboxOuterTexturePath;
        public string? SkyboxOuterTexturePath
        {
            get => _skyboxOuterTexturePath;
            set
            {
                if (SetProperty(ref _skyboxOuterTexturePath, value))
                {
                    PushSkyboxStateToRender();
                }
            }
        }

        // -------------------------------------------------------
        // Prefab / Assets browser (for AssetsWindow & asset panel)
        // -------------------------------------------------------

        public class PrefabListItem
        {
            public int ModelId { get; }
            public string Name { get; }
            public string Category { get; }

            public PrefabListItem(int modelId, string name, string category)
            {
                ModelId = modelId;
                Name = name;
                Category = category;
            }

            public override string ToString()
            {
                return $"{Name} ({Category})";
            }
        }

        public ObservableCollection<PrefabListItem> Prefabs { get; } =
            new ObservableCollection<PrefabListItem>();

        private PrefabListItem? _selectedPrefab;
        public PrefabListItem? SelectedPrefab
        {
            get => _selectedPrefab;
            set => SetProperty(ref _selectedPrefab, value);
        }

        // -----------------------------------------
        // Entity browser (debug / tooling)
        // -----------------------------------------

        public class EntityListItem
        {
            public int Id { get; }
            public string? EntityId { get; }
            public string Kind { get; }
            public string Position { get; }

            public EntityListItem(int id, string? entityId, string kind, string position)
            {
                Id = id;
                EntityId = entityId;
                Kind = kind;
                Position = position;
            }

            public override string ToString()
            {
                return $"{Id}: {EntityId} ({Kind})";
            }
        }

        private readonly ObservableCollection<EntityListItem> _entities = new ObservableCollection<EntityListItem>();
        public ReadOnlyObservableCollection<EntityListItem> Entities { get; }

        private EntityListItem? _selectedEntityListItem;
        public EntityListItem? SelectedEntityListItem
        {
            get => _selectedEntityListItem;
            set
            {
                if (SetProperty(ref _selectedEntityListItem, value))
                    OnSelectedEntityListItemChanged(value);
            }
        }

        private string? _entityFilterText;
        public string? EntityFilterText
        {
            get => _entityFilterText;
            set
            {
                if (SetProperty(ref _entityFilterText, value))
                {
                    RefreshEntityList();
                }
            }
        }

        private string? _entityKindFilter;
        public string? EntityKindFilter
        {
            get => _entityKindFilter;
            set
            {
                string? normalized = value;

                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    normalized = normalized.Trim();

                    // Any bracketed option such as "(All)" is considered "no filter".
                    if (normalized.StartsWith("(", StringComparison.Ordinal))
                    {
                        normalized = null;
                    }
                }
                else
                {
                    normalized = null;
                }

                if (SetProperty(ref _entityKindFilter, normalized))
                {
                    RefreshEntityList();
                }
            }
        }

        // -------------------------------------------------------
        // Camera path UI state
        // -------------------------------------------------------

        private ObservableCollection<CameraKeyframe> _cameraPathKeyframes = new ObservableCollection<CameraKeyframe>();
        public ObservableCollection<CameraKeyframe> CameraPathKeyframes
        {
            get => _cameraPathKeyframes;
            private set => SetProperty(ref _cameraPathKeyframes, value);
        }

        private bool _isCameraPathRecording;
        public bool IsCameraPathRecording
        {
            get => _isCameraPathRecording;
            private set => SetProperty(ref _isCameraPathRecording, value);
        }

        private bool _isCameraPathPlaying;
        public bool IsCameraPathPlaying
        {
            get => _isCameraPathPlaying;
            private set => SetProperty(ref _isCameraPathPlaying, value);
        }

        private float _cameraPathDuration;
        public float CameraPathDuration
        {
            get => _cameraPathDuration;
            private set => SetProperty(ref _cameraPathDuration, value);
        }

        private float _cameraPathCurrentTime;
        public float CameraPathCurrentTime
        {
            get => _cameraPathCurrentTime;
            set
            {
                if (SetProperty(ref _cameraPathCurrentTime, value))
                {
                    var recorder = GetCameraPathRecorder();
                    recorder?.ApplyAtTime(_cameraPathCurrentTime);
                }
            }
        }

        private CameraKeyframe? _selectedCameraKeyframe;
        public CameraKeyframe? SelectedCameraKeyframe
        {
            get => _selectedCameraKeyframe;
            set => SetProperty(ref _selectedCameraKeyframe, value);
        }

        // -------------------------------------------------------
        // Commands
        // -------------------------------------------------------

        public ICommand NewLevelCommand { get; }
        public ICommand OpenLevelCommand { get; }
        public ICommand SaveLevelCommand { get; }
        public ICommand ExportLevelToJsonCommand { get; }

        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        public ICommand StartCameraPathRecordingCommand { get; }
        public ICommand StopCameraPathRecordingCommand { get; }
        public ICommand PlayCameraPathCommand { get; }
        public ICommand StopCameraPathCommand { get; }
        public ICommand ClearCameraPathCommand { get; }
        public ICommand ExportCameraPathCommand { get; }
        public ICommand ImportCameraPathCommand { get; }
        public ICommand PreviewSelectedCameraKeyframeCommand { get; }

        public ICommand FocusSelectionCommand { get; }

        public ICommand ApplySelectionPositionCommand { get; }
        public ICommand ApplySelectionSizeCommand { get; }
        public ICommand ApplySelectionRotationCommand { get; }
        public ICommand ResetSelectionSizeCommand { get; }
        public ICommand ApplySelectionLightCommand { get; }
        public ICommand ResetSelectionLightCommand { get; }

        public ICommand ToggleSkyboxCommand { get; }
        public ICommand ChooseSkyboxInnerTextureCommand { get; }
        public ICommand ChooseSkyboxOuterTextureCommand { get; }

        // -------------------------------------------------------
        // Constructor
        // -------------------------------------------------------

        public SceneViewModel()
        {
            NewLevelCommand = new DelegateCommand(OnNewLevel);
            OpenLevelCommand = new DelegateCommand(OnOpenLevel);
            SaveLevelCommand = new DelegateCommand(OnSaveLevel);
            ExportLevelToJsonCommand = new DelegateCommand(OnExportLevelToJson);

            UndoCommand = new DelegateCommand(OnUndo);
            RedoCommand = new DelegateCommand(OnRedo);

            StartCameraPathRecordingCommand = new DelegateCommand(OnStartCameraPathRecording);
            StopCameraPathRecordingCommand = new DelegateCommand(OnStopCameraPathRecording);
            PlayCameraPathCommand = new DelegateCommand(OnPlayCameraPath);
            StopCameraPathCommand = new DelegateCommand(OnStopCameraPath);
            ClearCameraPathCommand = new DelegateCommand(OnClearCameraPath);
            ExportCameraPathCommand = new DelegateCommand(OnExportCameraPath);
            ImportCameraPathCommand = new DelegateCommand(OnImportCameraPath);
            PreviewSelectedCameraKeyframeCommand = new DelegateCommand(OnPreviewSelectedCameraKeyframe);

            FocusSelectionCommand = new DelegateCommand(OnFocusSelection);
            ApplySelectionPositionCommand = new DelegateCommand(OnApplySelectionPosition);
            ApplySelectionSizeCommand = new DelegateCommand(OnApplySelectionSize);
            ApplySelectionRotationCommand = new DelegateCommand(OnApplySelectionRotation);
            ResetSelectionSizeCommand = new DelegateCommand(OnResetSelectionSize);
            ApplySelectionLightCommand = new DelegateCommand(OnApplySelectionLight);
            ResetSelectionLightCommand = new DelegateCommand(OnResetSelectionLight);

            ToggleSkyboxCommand = new DelegateCommand(OnToggleSkybox);
            ChooseSkyboxInnerTextureCommand = new DelegateCommand(OnChooseSkyboxInnerTexture);
            ChooseSkyboxOuterTextureCommand = new DelegateCommand(OnChooseSkyboxOuterTexture);

            Entities = new ReadOnlyObservableCollection<EntityListItem>(_entities);

            // Initial prefab population so Assets window is never blank
            RefreshPrefabList();

            // Poll selection state periodically so the inspector + status bar stay in sync
            _selectionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _selectionTimer.Tick += (_, __) =>
            {
                RefreshSelectionInspector();
                RefreshStatusBar();
            };
            _selectionTimer.Start();

            UpdateDisplayName();
        }

        // -------------------------------------------------------
        // View attachment
        // -------------------------------------------------------

        public void AttachView(SceneView view)
        {
            _sceneView = view;

            if (LevelInfo != null)
            {
                // Ensure the runtime builds the level
                _sceneView.RefreshFromLevel(LevelInfo);
            }

            // Once the bootstrap exists, rebuild entities + prefabs
            RefreshEntityList();
            RefreshPrefabList();
            RefreshStatusBar();

            PushLayerStateToRender();
            PushGridSnapStateToRender();
            PushRotationSnapStateToRender();
            PushSkyboxStateToRender();
        }

        public void DetachView()
        {
            _sceneView = null;
        }

        // -------------------------------------------------------
        // Command Handlers ? file
        // -------------------------------------------------------

        private async void OnNewLevel()
        {
            try { await NewAsync(); }
            catch (Exception ex) { ShowError("Failed to create new level.", ex); }
        }

        private async void OnOpenLevel()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    InitialDirectory = EditorPaths.GetMapsFolder(),
                    Filter = "Map files (*.dat)|*.dat|All files (*.*)|*.*",
                    Title = "Open Map"
                };

                if (dialog.ShowDialog() == true)
                    await LoadAsync(dialog.FileName);
            }
            catch (Exception ex)
            {
                ShowError("Failed to open map file.", ex);
            }
        }

        private async void OnSaveLevel()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(FilePath))
                {
                    await SaveAsync(FilePath!);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    InitialDirectory = EditorPaths.GetMapsFolder(),
                    FileName = FileName ?? "NewMap.dat",
                    Filter = "Map files (*.dat)|*.dat|All files (*.*)|*.*",
                    DefaultExt = ".dat",
                    AddExtension = true
                };

                if (dialog.ShowDialog() == true)
                    await SaveAsync(dialog.FileName);
            }
            catch (Exception ex)
            {
                ShowError("Failed to save map file.", ex);
            }
        }

        private void OnExportLevelToJson()
        {
            try
            {
                if (LevelInfo == null)
                {
                    ShowError("There is no map loaded to export.", null!);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "Export Map as JSON",
                    InitialDirectory = EditorPaths.GetMapsFolder(),
                    FileName = string.IsNullOrWhiteSpace(FileName)
                        ? "MapExport.json"
                        : Path.ChangeExtension(FileName, ".json"),
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    AddExtension = true
                };

                if (dialog.ShowDialog() != true)
                    return;

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(LevelInfo, options);
                File.WriteAllText(dialog.FileName, json);
            }
            catch (Exception ex)
            {
                ShowError("Failed to export map as JSON.", ex);
            }
        }

        // -------------------------------------------------------
        // Camera path operations
        // -------------------------------------------------------

        private Components.Camera.CameraPathRecorder? GetCameraPathRecorder()
        {
            return _sceneView?.Bootstrap?.CameraPathRecorder;
        }

        private void RefreshCameraPathState()
        {
            var recorder = GetCameraPathRecorder();
            _cameraPathKeyframes.Clear();

            if (recorder != null)
            {
                foreach (var kf in recorder.Keyframes)
                    _cameraPathKeyframes.Add(kf);

                CameraPathDuration = recorder.Duration;
            }
            else
            {
                CameraPathDuration = 0f;
            }
        }

        // Preview selected camera keyframe
        private void OnPreviewSelectedCameraKeyframe()
        {
            var recorder = GetCameraPathRecorder();
            var kf = SelectedCameraKeyframe;

            if (recorder == null || kf == null)
                return;

            recorder.ApplyAtTime(kf.Time);
            CameraPathCurrentTime = kf.Time;
            IsCameraPathPlaying = false;

            RefreshStatusBar();
        }

        // -------------------------------------------------------
        // Selection helpers
        // -------------------------------------------------------

        private EntityInfo? GetSelectedEntity()
        {
            return _sceneView?.Bootstrap?.Settings?.SelectedEntity;
        }

        private static string ClassifyEntityKind(EntityInfo entity)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.EntityID))
                return "None";

            var id = entity.EntityID;

            if (id.IndexOf("Light", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Light";

            if (id.IndexOf("Trigger", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("Warp", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("Area", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("Zone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("Region", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Trigger";

            if (id.IndexOf("Collision", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("Collide", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("Block", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("Solid", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Collision";

            return "Entity";
        }

        private void RefreshSelectionInspector()
        {
            var e = GetSelectedEntity();

            if (e == null)
            {
                SelectedEntityId = null;
                SelectedEntityNumericId = 0;
                SelectedEntityKind = null;
                SelectedEntityPosition = null;
                SelectedEntitySize = null;
                SelectedEntityRotation = null;

                SelectedEntityPosX = 0f;
                SelectedEntityPosY = 0f;
                SelectedEntityPosZ = 0f;

                SelectedEntitySizeX = 0f;
                SelectedEntitySizeY = 0f;
                SelectedEntitySizeZ = 0f;

                SelectedEntityRotX = 0f;
                SelectedEntityRotY = 0f;
                SelectedEntityRotZ = 0f;

                SelectedLightColorR = 0;
                SelectedLightColorG = 0;
                SelectedLightColorB = 0;
                SelectedLightRadius = 0f;

                return;
            }

            SelectedEntityId = e.EntityID;
            SelectedEntityNumericId = e.ID;

            var kind = ClassifyEntityKind(e);
            SelectedEntityKind = kind;

            var pos = e.Position;
            SelectedEntityPosition = string.Format("X={0:F2}, Y={1:F2}, Z={2:F2}", pos.X, pos.Y, pos.Z);

            SelectedEntityPosX = pos.X;
            SelectedEntityPosY = pos.Y;
            SelectedEntityPosZ = pos.Z;

            var size = e.Size;
            if (size != Vector3.Zero)
            {
                SelectedEntitySize = string.Format("X={0:F2}, Y={1:F2}, Z={2:F2}", size.X, size.Y, size.Z);

                SelectedEntitySizeX = size.X;
                SelectedEntitySizeY = size.Y;
                SelectedEntitySizeZ = size.Z;
            }
            else
            {
                SelectedEntitySize = "(implicit / model bounds)";
                SelectedEntitySizeX = 0f;
                SelectedEntitySizeY = 0f;
                SelectedEntitySizeZ = 0f;
            }

            var rot = e.Rotation;

            SelectedEntityRotation = string.Format(
                "X={0:F1}°, Y={1:F1}°, Z={2:F1}°",
                MathHelper.ToDegrees(rot.X),
                MathHelper.ToDegrees(rot.Y),
                MathHelper.ToDegrees(rot.Z));

            SelectedEntityRotX = MathHelper.ToDegrees(rot.X);
            SelectedEntityRotY = MathHelper.ToDegrees(rot.Y);
            SelectedEntityRotZ = MathHelper.ToDegrees(rot.Z);

            if (string.Equals(kind, "Light", StringComparison.OrdinalIgnoreCase))
            {
                var col = e.Shader;
                SelectedLightColorR = col.R;
                SelectedLightColorG = col.G;
                SelectedLightColorB = col.B;

                float radius = size != Vector3.Zero ? size.X : 3f;
                if (radius < 0f)
                    radius = 0f;

                SelectedLightRadius = radius;
            }
            else
            {
                SelectedLightColorR = 0;
                SelectedLightColorG = 0;
                SelectedLightColorB = 0;
                SelectedLightRadius = 0f;
            }

            if (_entities.Count > 0)
            {
                foreach (var item in _entities)
                {
                    if (item.Id == e.ID)
                    {
                        SelectedEntityListItem = item;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the status bar strings from the current camera and selection.
        /// </summary>
        private void RefreshStatusBar()
        {
            var view = _sceneView;
            var bootstrap = view?.Bootstrap;

            if (bootstrap != null)
            {
                var cam = bootstrap.Camera;
                var pos = cam.Position;
                StatusCameraText = string.Format(
                    "Cam: X={0:F1}, Y={1:F1}, Z={2:F1}",
                    pos.X, pos.Y, pos.Z);
            }
            else
            {
                StatusCameraText = string.Empty;
            }

            var e = GetSelectedEntity();
            if (e != null)
            {
                StatusSelectionText = string.Format(
                    "Selected: #{0} ({1})",
                    e.ID,
                    string.IsNullOrWhiteSpace(e.EntityID) ? "Entity" : e.EntityID);
            }
            else
            {
                StatusSelectionText = "Selected: none";
            }

            if (string.IsNullOrWhiteSpace(StatusMessage))
            {
                StatusMessage = "Ready";
            }
        }

        // -------------------------------------------------------
        // Entity list / prefab list
        // -------------------------------------------------------

        private void RefreshEntityList()
        {
            var view = _sceneView;
            var bootstrap = view?.Bootstrap;
            if (bootstrap == null)
            {
                _entities.Clear();
                return;
            }

            var entities = bootstrap.GetAllEntities();
            _entities.Clear();

            if (entities == null || entities.Count == 0)
                return;

            string? filter = EntityFilterText;
            bool hasFilter = !string.IsNullOrWhiteSpace(filter);
            if (hasFilter)
                filter = filter!.Trim();

            string? kindFilter = EntityKindFilter;
            bool hasKindFilter = !string.IsNullOrWhiteSpace(kindFilter);
            if (hasKindFilter)
                kindFilter = kindFilter!.Trim();

            foreach (var e in entities)
            {
                if (e == null)
                    continue;

                var kind = ClassifyEntityKind(e);

                if (hasKindFilter && !kind.Equals(kindFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (hasFilter)
                {
                    var id = e.EntityID ?? string.Empty;
                    if (id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                        kind.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }

                var pos = e.Position;
                string posText = string.Format("X={0:F1}, Y={1:F1}, Z={2:F1}", pos.X, pos.Y, pos.Z);

                var item = new EntityListItem(e.ID, e.EntityID, kind, posText);
                _entities.Add(item);
            }
        }

        private void RefreshPrefabList()
        {
            Prefabs.Clear();

            bool addedFromLevel = false;

            if (LevelInfo != null && LevelInfo.Entities != null && LevelInfo.Entities.Count > 0)
            {
                var seen = new HashSet<string>();

                foreach (var e in LevelInfo.Entities)
                {
                    if (e == null)
                        continue;

                    var kind = ClassifyEntityKind(e);
                    string id = e.EntityID ?? string.Empty;
                    string key = $"{id}|{kind}";

                    if (!seen.Add(key))
                        continue;

                    int modelId = e.ID;

                    string name = string.IsNullOrWhiteSpace(e.EntityID)
                        ? $"Entity {e.ID}"
                        : e.EntityID!;

                    string category = kind;

                    Prefabs.Add(new PrefabListItem(modelId, name, category));
                    addedFromLevel = true;
                }
            }

            if (!addedFromLevel)
            {
                Prefabs.Add(new PrefabListItem(1, "Debug Cube", "Geometry"));
                Prefabs.Add(new PrefabListItem(2, "Debug Plane", "Geometry"));
                Prefabs.Add(new PrefabListItem(3, "Point Light", "Light"));
                Prefabs.Add(new PrefabListItem(4, "Spawn Point", "Entity"));
            }

            if (Prefabs.Count > 0 && SelectedPrefab == null)
                SelectedPrefab = Prefabs[0];
        }

        // -------------------------------------------------------
        // Small math helpers
        // -------------------------------------------------------

        private static float SnapToGridComponent(float value, float gridSize)
        {
            if (gridSize <= 0f)
                return value;

            return (float)Math.Round(value / gridSize) * gridSize;
        }

        private static float SnapAngleComponent(float angleDegrees, float stepDegrees)
        {
            if (stepDegrees <= 0f)
                return angleDegrees;

            return (float)Math.Round(angleDegrees / stepDegrees) * stepDegrees;
        }

        // -------------------------------------------------------
        // List selection -> scene selection
        // -------------------------------------------------------

        private void OnSelectedEntityListItemChanged(EntityListItem? item)
        {
            if (item == null)
                return;

            var view = _sceneView;
            var bootstrap = view?.Bootstrap;
            if (bootstrap == null)
                return;

            bootstrap.SelectEntityById(item.Id);
            bootstrap.FocusOnSelection();

            RefreshSelectionInspector();
            RefreshStatusBar();
        }

        // -------------------------------------------------------
        // Skybox command handlers
        // -------------------------------------------------------

        private void OnToggleSkybox()
        {
            SkyboxEnabled = !SkyboxEnabled;
        }

        private void OnChooseSkyboxInnerTexture()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Choose Skybox Inner Texture",
                    Filter = "Image files|*.png;*.jpg;*.jpeg;*.dds;*.bmp|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    SkyboxInnerTexturePath = dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                ShowError("Failed to choose inner skybox texture.", ex);
            }
        }

        private void OnChooseSkyboxOuterTexture()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Choose Skybox Outer Texture",
                    Filter = "Image files|*.png;*.jpg;*.jpeg;*.dds;*.bmp|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    SkyboxOuterTexturePath = dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                ShowError("Failed to choose outer skybox texture.", ex);
            }
        }

        // -------------------------------------------------------
        // Camera path command handlers
        // -------------------------------------------------------

        private void OnStartCameraPathRecording()
        {
            var recorder = GetCameraPathRecorder();
            if (recorder == null)
                return;

            recorder.Clear();
            recorder.ToggleRecording();

            IsCameraPathRecording = true;
            IsCameraPathPlaying = false;
            CameraPathCurrentTime = 0f;

            RefreshCameraPathState();
        }

        private void OnStopCameraPathRecording()
        {
            var recorder = GetCameraPathRecorder();
            if (recorder == null)
                return;

            if (IsCameraPathRecording)
            {
                recorder.ToggleRecording();
                IsCameraPathRecording = false;
                RefreshCameraPathState();
            }
        }

        private void OnPlayCameraPath()
        {
            var recorder = GetCameraPathRecorder();
            if (recorder == null)
                return;

            recorder.StartPlayback();
            IsCameraPathPlaying = true;
            IsCameraPathRecording = false;
            CameraPathCurrentTime = 0f;
        }

        private void OnStopCameraPath()
        {
            var recorder = GetCameraPathRecorder();
            if (recorder == null)
                return;

            CameraPathCurrentTime = 0f;
            if (recorder.Duration > 0f)
            {
                recorder.ApplyAtTime(0f);
            }

            IsCameraPathPlaying = false;
        }

        private void OnClearCameraPath()
        {
            var recorder = GetCameraPathRecorder();
            if (recorder == null)
                return;

            recorder.Clear();
            CameraPathCurrentTime = 0f;
            IsCameraPathRecording = false;
            IsCameraPathPlaying = false;
            RefreshCameraPathState();
        }

        private sealed class CameraPathJsonKeyframe
        {
            public float Time { get; set; }
            public float[]? Position { get; set; }
            public float[]? Target { get; set; }
        }

        private sealed class CameraPathJsonModel
        {
            public List<CameraPathJsonKeyframe>? Keyframes { get; set; }
        }

        private void OnExportCameraPath()
        {
            var recorder = GetCameraPathRecorder();
            if (recorder == null || recorder.Keyframes.Count == 0)
                return;

            try
            {
                var list = new List<CameraPathJsonKeyframe>();
                foreach (var kf in recorder.Keyframes)
                {
                    list.Add(new CameraPathJsonKeyframe
                    {
                        Time = kf.Time,
                        Position = new[] { kf.Position.X, kf.Position.Y, kf.Position.Z },
                        Target = new[] { kf.Target.X, kf.Target.Y, kf.Target.Z }
                    });
                }

                var model = new CameraPathJsonModel { Keyframes = list };

                var dialog = new SaveFileDialog
                {
                    Title = "Export Camera Path",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    AddExtension = true,
                    FileName = "camera_path.json",
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = JsonSerializer.Serialize(model, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    File.WriteAllText(dialog.FileName, json);
                }
            }
            catch (Exception ex)
            {
                ShowError("Failed to export camera path.", ex);
            }
        }

        private void OnImportCameraPath()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Import Camera Path",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                };

                if (dialog.ShowDialog() != true)
                    return;

                var json = File.ReadAllText(dialog.FileName);
                var model = JsonSerializer.Deserialize<CameraPathJsonModel>(json);
                if (model?.Keyframes == null || model.Keyframes.Count == 0)
                    return;

                var frames = new List<CameraKeyframe>();
                foreach (var kf in model.Keyframes)
                {
                    if (kf.Position == null || kf.Position.Length != 3 ||
                        kf.Target == null || kf.Target.Length != 3)
                    {
                        continue;
                    }

                    frames.Add(new CameraKeyframe
                    {
                        Time = kf.Time,
                        Position = new Vector3(kf.Position[0], kf.Position[1], kf.Position[2]),
                        Target = new Vector3(kf.Target[0], kf.Target[1], kf.Target[2]),
                    });
                }

                var recorder = GetCameraPathRecorder();
                if (recorder == null)
                    return;

                recorder.SetKeyframes(frames);
                CameraPathCurrentTime = 0f;
                IsCameraPathRecording = false;
                IsCameraPathPlaying = false;
                RefreshCameraPathState();
            }
            catch (Exception ex)
            {
                ShowError("Failed to import camera path.", ex);
            }
        }

        // -------------------------------------------------------
        // Core document logic
        // -------------------------------------------------------

        private void OnFocusSelection()
        {
            var view = _sceneView;
            var bootstrap = view?.Bootstrap;
            if (bootstrap == null)
                return;

            bootstrap.FocusOnSelection();
        }

        private void OnApplySelectionPosition()
        {
            var e = GetSelectedEntity();
            if (e == null)
                return;

            var oldPos = e.Position;
            var oldSize = e.Size;
            var oldRot = e.Rotation;

            var target = new Vector3(SelectedEntityPosX, SelectedEntityPosY, SelectedEntityPosZ);

            var bootstrap = _sceneView?.Bootstrap;
            if (bootstrap?.Settings != null && bootstrap.Settings.EnableGridSnap)
            {
                var gs = bootstrap.Settings.GridSize;
                target = new Vector3(
                    SnapToGridComponent(target.X, gs.X),
                    SnapToGridComponent(target.Y, gs.Y),
                    SnapToGridComponent(target.Z, gs.Z));
            }

            e.Position = target;

            PushTransformUndo(e, oldPos, oldSize, oldRot, e.Position, e.Size, e.Rotation);

            RefreshSelectionInspector();
            RefreshEntityList();
            RefreshStatusBar();
        }

        private void OnApplySelectionSize()
        {
            var e = GetSelectedEntity();
            if (e == null)
                return;

            var oldPos = e.Position;
            var oldSize = e.Size;
            var oldRot = e.Rotation;

            var size = new Vector3(SelectedEntitySizeX, SelectedEntitySizeY, SelectedEntitySizeZ);

            size.X = Math.Max(0f, size.X);
            size.Y = Math.Max(0f, size.Y);
            size.Z = Math.Max(0f, size.Z);

            e.Size = size;

            PushTransformUndo(e, oldPos, oldSize, oldRot, e.Position, e.Size, e.Rotation);

            RefreshSelectionInspector();
            RefreshStatusBar();
        }

        private void OnApplySelectionRotation()
        {
            var e = GetSelectedEntity();
            if (e == null)
                return;

            var oldPos = e.Position;
            var oldSize = e.Size;
            var oldRot = e.Rotation;

            float degX = SelectedEntityRotX;
            float degY = SelectedEntityRotY;
            float degZ = SelectedEntityRotZ;

            var bootstrap = _sceneView?.Bootstrap;
            var settings = bootstrap?.Settings;
            if (settings != null && settings.EnableRotationSnap)
            {
                float step = settings.RotationSnapDegrees;
                if (step <= 0f)
                    step = 1f;

                degX = SnapAngleComponent(degX, step);
                degY = SnapAngleComponent(degY, step);
                degZ = SnapAngleComponent(degZ, step);

                SelectedEntityRotX = degX;
                SelectedEntityRotY = degY;
                SelectedEntityRotZ = degZ;
            }

            var newRot = new Vector3(
                MathHelper.ToRadians(degX),
                MathHelper.ToRadians(degY),
                MathHelper.ToRadians(degZ));

            e.Rotation = newRot;

            PushTransformUndo(e, oldPos, oldSize, oldRot, e.Position, e.Size, e.Rotation);

            RefreshSelectionInspector();
            RefreshEntityList();
            RefreshStatusBar();
        }

        private void OnResetSelectionSize()
        {
            var e = GetSelectedEntity();
            if (e == null)
                return;

            var oldPos = e.Position;
            var oldSize = e.Size;
            var oldRot = e.Rotation;

            e.Size = Vector3.Zero;

            PushTransformUndo(e, oldPos, oldSize, oldRot, e.Position, e.Size, e.Rotation);

            RefreshSelectionInspector();
            RefreshStatusBar();
        }

        private void OnApplySelectionLight()
        {
            var e = GetSelectedEntity();
            if (e == null)
                return;

            var kind = ClassifyEntityKind(e);
            if (!string.Equals(kind, "Light", StringComparison.OrdinalIgnoreCase))
                return;

            var oldPos = e.Position;
            var oldSize = e.Size;
            var oldRot = e.Rotation;

            int r = Math.Clamp(SelectedLightColorR, 0, 255);
            int g = Math.Clamp(SelectedLightColorG, 0, 255);
            int b = Math.Clamp(SelectedLightColorB, 0, 255);

            e.Shader = new Color(r, g, b);

            float radius = Math.Max(0f, SelectedLightRadius);
            var newSize = radius > 0f ? new Vector3(radius, radius, radius) : Vector3.Zero;
            e.Size = newSize;

            PushTransformUndo(e, oldPos, oldSize, oldRot, e.Position, e.Size, e.Rotation);

            RefreshSelectionInspector();
            RefreshEntityList();
            RefreshStatusBar();
        }

        private void OnResetSelectionLight()
        {
            var e = GetSelectedEntity();
            if (e == null)
                return;

            var kind = ClassifyEntityKind(e);
            if (!string.Equals(kind, "Light", StringComparison.OrdinalIgnoreCase))
                return;

            var oldPos = e.Position;
            var oldSize = e.Size;
            var oldRot = e.Rotation;

            e.Shader = Color.White;
            e.Size = Vector3.Zero;

            PushTransformUndo(e, oldPos, oldSize, oldRot, e.Position, e.Size, e.Rotation);

            RefreshSelectionInspector();
            RefreshStatusBar();
        }

        // -------------------------------------------------------
        // Undo / Redo
        // -------------------------------------------------------

        private void OnUndo()
        {
            if (_undoStack.Count == 0)
                return;

            var item = _undoStack.Pop();
            _isUndoRedoActive = true;
            try
            {
                item.Entity.Position = item.OldPosition;
                item.Entity.Size = item.OldSize;
                item.Entity.Rotation = item.OldRotation;

                _redoStack.Push(item);

                RefreshSelectionInspector();
                RefreshEntityList();
                RefreshStatusBar();
            }
            finally
            {
                _isUndoRedoActive = false;
                NotifyUndoRedoStacksChanged();
            }
        }

        private void OnRedo()
        {
            if (_redoStack.Count == 0)
                return;

            var item = _redoStack.Pop();
            _isUndoRedoActive = true;
            try
            {
                item.Entity.Position = item.NewPosition;
                item.Entity.Size = item.NewSize;
                item.Entity.Rotation = item.NewRotation;

                _undoStack.Push(item);

                RefreshSelectionInspector();
                RefreshEntityList();
                RefreshStatusBar();
            }
            finally
            {
                _isUndoRedoActive = false;
                NotifyUndoRedoStacksChanged();
            }
        }

        private void PushTransformUndo(
            EntityInfo entity,
            Vector3 oldPosition,
            Vector3 oldSize,
            Vector3 oldRotation,
            Vector3 newPosition,
            Vector3 newSize,
            Vector3 newRotation)
        {
            if (_isUndoRedoActive)
                return;

            var item = new TransformUndoItem
            {
                Entity = entity,
                OldPosition = oldPosition,
                OldSize = oldSize,
                OldRotation = oldRotation,
                NewPosition = newPosition,
                NewSize = newSize,
                NewRotation = newRotation
            };

            _undoStack.Push(item);
            _redoStack.Clear();

            NotifyUndoRedoStacksChanged();
        }

        private void NotifyUndoRedoStacksChanged()
        {
            RaisePropertyChanged(nameof(CanUndo));
            RaisePropertyChanged(nameof(CanRedo));
        }

        // -------------------------------------------------------
        // New / Load / Save
        // -------------------------------------------------------

        public Task NewAsync()
        {
            FilePath = null;
            _originalText = string.Empty;

            var level = new LevelInfo(
                new LevelTags(),
                string.Empty,
                new LevelTags(),
                new List<EntityInfo>(),
                new List<StructureInfo>(),
                new List<OffsetMapInfo>(),
                new ShaderInfo(),
                new BackdropInfo()
            );

            LevelInfo = level;
            RaisePropertyChanged(nameof(LevelInfo));

            UpdateDisplayName();
            _sceneView?.RefreshFromLevel(level);

            RefreshEntityList();
            RefreshPrefabList();

            _undoStack.Clear();
            _redoStack.Clear();
            NotifyUndoRedoStacksChanged();
            RefreshStatusBar();

            return Task.CompletedTask;
        }

        private static void SaveLastMapPath(string path)
        {
            try
            {
                var root = EditorPaths.GetProjectRoot();
                var file = Path.Combine(root, "last_map.txt");

                if (!Directory.Exists(root))
                    Directory.CreateDirectory(root);

                File.WriteAllText(file, path ?? string.Empty);
                Debug.WriteLine($"[SceneViewModel] Saved last_map.txt: {path}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SceneViewModel] Failed to save last_map.txt: {ex.Message}");
            }
        }

        public async Task LoadAsync(string filePath)
        {
            Debug.WriteLine($"[SceneViewModel] LoadAsync: {filePath}");

            // -------------------------------------------------------
            // Basic validation
            // -------------------------------------------------------
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                ShowError("Map file not found.", null!);
                return;
            }

            try
            {
                // Status so you can see it's doing work
                StatusMessage = $"Loading map: {Path.GetFileName(filePath)}...";

                // 1) Read raw text
                string text = await File.ReadAllTextAsync(filePath);

                // 2) Parse .dat into LevelInfo
                LevelInfo loadedLevel;
                try
                {
                    loadedLevel = LevelLoader.Load(text, filePath);
                }
                catch (Exception ex)
                {
                    ShowError("LevelLoader failed to parse this map file.", ex);
                    return;
                }

                if (loadedLevel == null)
                {
                    ShowError("LevelLoader returned no level data.", null!);
                    return;
                }

                // NOTE: Do NOT try to assign Entities/Structures/OffsetMaps here.
                // They are read-only properties on LevelInfo and are already created
                // by the LevelInfo constructor that LevelLoader uses.

                // 3) Apply to view model state
                LevelInfo = loadedLevel;
                _originalText = text;
                FilePath = filePath;
                _currentMapPath = filePath;

                // Save last map for RenderTest / reload helpers
                SaveLastMapPath(filePath);

                // Optional: file watcher so external edits reload
                SetupMapWatcher(filePath);

                RaisePropertyChanged(nameof(LevelInfo));
                UpdateDisplayName(); // makes the tab / window show the map filename

                // 4) Push the new level into the SceneView / engine bootstrap
                if (_sceneView != null && LevelInfo != null)
                {
                    _sceneView.RefreshFromLevel(LevelInfo);
                }

                // 5) Rebuild UI lists
                RefreshEntityList();
                RefreshPrefabList();

                // 6) Reset undo/redo
                _undoStack.Clear();
                _redoStack.Clear();
                NotifyUndoRedoStacksChanged();

                RefreshStatusBar();

                // 7) Final status + simple debug info so we know what got parsed
                var entityCount = LevelInfo.Entities?.Count ?? 0;
                var structureCount = LevelInfo.Structures?.Count ?? 0;
                var offsetMapCount = LevelInfo.OffsetMaps?.Count ?? 0;

                StatusMessage = $"Loaded map: {FileName}  " +
                                $"(Entities: {entityCount}, Structures: {structureCount}, OffsetMaps: {offsetMapCount})";

                MessageBox.Show(
                    $"Loaded map '{FileName}'\n\n" +
                    $"Entities:   {entityCount}\n" +
                    $"Structures: {structureCount}\n" +
                    $"OffsetMaps: {offsetMapCount}",
                    "Map Load Debug",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError("Failed to load map file.", ex);
            }
        }

        public async Task SaveAsync(string filePath)
        {
            var newText = _originalText ?? string.Empty;

            await File.WriteAllTextAsync(filePath, newText);

            FilePath = filePath;
            _originalText = newText;

            SaveLastMapPath(filePath);
            UpdateDisplayName();

            try
            {
                var reloadText = await File.ReadAllTextAsync(filePath);
                LevelInfo = LevelLoader.Load(reloadText, filePath);
                RaisePropertyChanged(nameof(LevelInfo));

                if (_sceneView != null && LevelInfo != null)
                {
                    _sceneView.RefreshFromLevel(LevelInfo);
                }

                RefreshEntityList();
                RefreshPrefabList();
            }
            catch
            {
                if (_sceneView != null && LevelInfo != null)
                {
                    _sceneView.RefreshFromLevel(LevelInfo);
                }
            }

            RefreshStatusBar();
        }

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------

        private void UpdateDisplayName()
        {
            DisplayName = string.IsNullOrEmpty(FileName)
                ? Resources.MapFile
                : FileName;
        }

        private static void ShowError(string message, Exception ex)
        {
            var msg = ex == null ? message : $"{message}\n\n{ex.Message}";
            MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PushLayerStateToRender()
        {
            var bootstrap = _sceneView?.Bootstrap;
            if (bootstrap == null) return;

            bootstrap.SetLayerVisibility(
                _showGeometry,
                _showProps,
                _showCollision,
                _showLights,
                _showTriggers,
                _showGrid);
        }

        private void PushGridSnapStateToRender()
        {
            var bootstrap = _sceneView?.Bootstrap;
            if (bootstrap == null) return;

            bootstrap.SetGridSnap(_useGridSnap, _gridSize);
        }

        private void PushRotationSnapStateToRender()
        {
            var bootstrap = _sceneView?.Bootstrap;
            if (bootstrap == null) return;

            bootstrap.SetRotationSnap(_rotationSnapEnabled, _rotationSnapStep);
        }

        private void PushSkyboxStateToRender()
        {
            var bootstrap = _sceneView?.Bootstrap;
            if (bootstrap == null) return;

            bootstrap.SetSkyboxSettings(
                SkyboxEnabled,
                SkyboxInnerTexturePath,
                SkyboxOuterTexturePath);
        }

        // -------------------------------------------------------
        // Map file watcher (reload on save)
        // -------------------------------------------------------

        private void SetupMapWatcher(string path)
        {
            try
            {
                _mapWatcher?.Dispose();
                _mapWatcher = null;

                var directory = Path.GetDirectoryName(path);
                var fileName = Path.GetFileName(path);

                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                    return;

                _mapWatcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };

                _mapWatcher.Changed += OnMapFileChanged;
                _mapWatcher.EnableRaisingEvents = true;
            }
            catch
            {
                // watcher is best-effort only
            }
        }

        private async void OnMapFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentMapPath) &&
                    !string.Equals(e.FullPath, _currentMapPath, StringComparison.OrdinalIgnoreCase))
                    return;

                await Application.Current.Dispatcher.InvokeAsync(
                    async () =>
                    {
                        try
                        {
                            if (!File.Exists(e.FullPath))
                                return;

                            await LoadAsync(e.FullPath);
                        }
                        catch (Exception ex)
                        {
                            ShowError("Failed to reload map after external change.", ex);
                        }
                    },
                    DispatcherPriority.Background);
            }
            catch
            {
                // swallow watcher exceptions
            }
        }
    }
}