using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;

using GE2D3D.MapEditor.Modules.SceneViewer.ViewModels;
using GE2D3D.MapEditor.Modules.SceneViewer.Views;

namespace GE2D3D.MapEditor.Modules.SceneViewer
{
    /// <summary>
    /// Prism module for the Scene Viewer.
    /// Replaces the old Gemini module/MEF export.
    /// </summary>
    public class SceneViewerModule : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register the view model so Prism can resolve it
            containerRegistry.Register<SceneViewModel>();

            // If you are using Prism navigation, you can also register the view for navigation:
            // containerRegistry.RegisterForNavigation<SceneView, SceneViewModel>("SceneView");
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // If you use regions in your shell, you can attach SceneView to a region here.
            // Comment this out if you don't have regions yet.

            var regionManager = containerProvider.Resolve<IRegionManager>();

            // "MainRegion" should match the region name you define in your MainWindow.xaml
            // <ContentControl prism:RegionManager.RegionName="MainRegion" ... />
            regionManager.RegisterViewWithRegion("MainRegion", typeof(SceneView));

            // NOTE:
            // The old Gemini inspector integration (DefaultPropertyInspectors, FloatEditorViewModel, etc.)
            // has no direct Prism equivalent. Once you build your own inspector system or tools panel,
            // you can hook it up here in OnInitialized.
        }
    }
}