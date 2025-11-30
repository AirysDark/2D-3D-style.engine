using Prism.Commands;
using Prism.Mvvm;
using GE2D3D.MapEditor.Modules.SceneViewer.ViewModels;

namespace GE2D3D.MapEditor.Modules.SceneViewer.Commands
{
    /// <summary>
    /// Super-simple Prism "New file" command wrapper.
    /// No Gemini, no EditorFileType, just calls SceneViewModel.NewAsync().
    /// </summary>
    public class NewFileCommandHandler : BindableBase
    {
        private readonly SceneViewModel _sceneViewModel;

        /// <summary>
        /// Command you can bind to from the UI.
        /// </summary>
        public DelegateCommand NewFileCommand { get; }

        public NewFileCommandHandler(SceneViewModel sceneViewModel)
        {
            _sceneViewModel = sceneViewModel;
            NewFileCommand = new DelegateCommand(ExecuteNewFile);
        }

        private async void ExecuteNewFile()
        {
            await _sceneViewModel.NewAsync();
        }
    }
}