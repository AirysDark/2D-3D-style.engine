using System.Windows;
using Prism.Ioc;
using Prism.Modularity;

using GE2D3D.MapEditor.Modules.SceneViewer.ViewModels;

namespace GE2D3D.MapEditor.Modules.Startup
{
    /// <summary>
    /// Prism startup module ? replaces the old Gemini/MEF Module.
    /// Sets the main window title and ensures SceneViewModel is registered.
    /// </summary>
    public class Module : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Make sure SceneViewModel can be resolved by Prism
            containerRegistry.Register<SceneViewModel>();
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // Set the window title
            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow != null)
                mainWindow.Title = "P3D Legacy Map Editor";

            // If you want to grab the SceneViewModel at startup, you can:
            // var sceneVm = containerProvider.Resolve<SceneViewModel>();
            // Then either:
            // - assign it as DataContext of a view, or
            // - use regions to inject the view that uses this VM.
        }
    }
}