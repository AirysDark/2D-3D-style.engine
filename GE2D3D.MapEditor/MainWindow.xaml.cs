using System.Windows;
using GE2D3D.MapEditor.Modules.SceneViewer.ViewModels;

namespace GE2D3D.MapEditor
{
    public partial class MainWindow : Window
    {
        public SceneViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();

            // Create VM once
            ViewModel = new SceneViewModel();

            // Set DataContext AFTER InitializeComponent  
            this.DataContext = ViewModel;

            // Hook Loaded so SceneViewControl is guaranteed to exist
            this.Loaded += OnMainWindowLoaded;
        }

        private Modules.SceneViewer.Views.SelectionWindow? _selectionWindow;
        private Modules.SceneViewer.Views.EntitiesWindow? _entitiesWindow;
        private Modules.SceneViewer.Views.CameraPathWindow? _cameraPathWindow;
        private Modules.SceneViewer.Views.AssetsWindow? _assetsWindow;

        private void EnsureToolWindowOwner(Window window)
        {
            if (window.Owner == null)
                window.Owner = this;
        }

        private void OnSelectionMenuClick(object sender, RoutedEventArgs e)
        {
            if (_selectionWindow == null || !_selectionWindow.IsLoaded)
            {
                _selectionWindow = new Modules.SceneViewer.Views.SelectionWindow
                {
                    DataContext = ViewModel
                };
                EnsureToolWindowOwner(_selectionWindow);
                _selectionWindow.Show();
            }
            else
            {
                _selectionWindow.Activate();
            }
        }

        private void OnEntitiesMenuClick(object sender, RoutedEventArgs e)
        {
            if (_entitiesWindow == null || !_entitiesWindow.IsLoaded)
            {
                _entitiesWindow = new Modules.SceneViewer.Views.EntitiesWindow
                {
                    DataContext = ViewModel
                };
                EnsureToolWindowOwner(_entitiesWindow);
                _entitiesWindow.Show();
            }
            else
            {
                _entitiesWindow.Activate();
            }
        }

        private void OnCameraPathMenuClick(object sender, RoutedEventArgs e)
        {
            if (_cameraPathWindow == null || !_cameraPathWindow.IsLoaded)
            {
                _cameraPathWindow = new Modules.SceneViewer.Views.CameraPathWindow
                {
                    DataContext = ViewModel
                };
                EnsureToolWindowOwner(_cameraPathWindow);
                _cameraPathWindow.Show();
            }
            else
            {
                _cameraPathWindow.Activate();
            }
        }

        private void OnAssetsMenuClick(object sender, RoutedEventArgs e)
        {
            if (_assetsWindow == null || !_assetsWindow.IsLoaded)
            {
                _assetsWindow = new Modules.SceneViewer.Views.AssetsWindow
                {
                    DataContext = ViewModel
                };
                EnsureToolWindowOwner(_assetsWindow);
                _assetsWindow.Show();
            }
            else
            {
                _assetsWindow.Activate();
            }
        }

        private void OnAAModeMenuClick(object sender, RoutedEventArgs e)
        {
            if (SceneViewControl == null)
                return;

            if (sender is not System.Windows.Controls.MenuItem menuItem)
                return;

            var tag = (menuItem.Tag as string) ?? menuItem.Header?.ToString();
            if (string.IsNullOrWhiteSpace(tag))
                return;

            if (System.Enum.TryParse<Components.Render.AntiAliasing>(tag, ignoreCase: true, out var mode))
            {
                SceneViewControl.SetAntiAliasingFromMenu(mode);
            }
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Now SceneViewControl is guaranteed to be constructed
            ViewModel.AttachView(SceneViewControl);
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}