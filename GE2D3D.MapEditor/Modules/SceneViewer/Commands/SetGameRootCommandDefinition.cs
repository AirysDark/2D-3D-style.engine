using System;
using System.Windows.Forms;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using GE2D3D.MapEditor.Utils;

namespace GE2D3D.MapEditor.Modules.Startup.Commands
{
    /// <summary>
    /// Prism-style command host for "Set Game Root".
    /// Bind SetGameRootCommand in XAML instead of using Gemini's CommandDefinition/Handler.
    /// </summary>
    public class StartupCommands : BindableBase
    {
        public ICommand SetGameRootCommand { get; }

        public StartupCommands()
        {
            SetGameRootCommand = new DelegateCommand(ExecuteSetGameRoot);
        }

        private void ExecuteSetGameRoot()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the P3D game root (folder which contains Content/)";
                var result = dialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    EditorPaths.SetGameRoot(dialog.SelectedPath);
                }
            }
        }
    }
}