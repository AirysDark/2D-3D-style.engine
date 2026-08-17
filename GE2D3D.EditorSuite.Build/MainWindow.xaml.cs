using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace GE2D3D.EditorSuite
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private string OutputDirectory => AppContext.BaseDirectory;

        private void OpenMapEditor_Click(object sender, RoutedEventArgs e)
        {
            LaunchSibling("GE2D3D.MapEditor.exe");
        }

        private void OpenRenderTest_Click(object sender, RoutedEventArgs e)
        {
            LaunchSibling("RenderTest.exe");
        }

        private void OpenProjectFolder_Click(object sender, RoutedEventArgs e)
        {
            string projectRoot = FindProjectRoot();
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{projectRoot}\"")
            {
                UseShellExecute = true
            });
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LaunchSibling(string executable)
        {
            string path = Path.Combine(OutputDirectory, executable);

            if (!File.Exists(path))
            {
                MessageBox.Show(
                    $"Could not find {executable}.\n\nExpected:\n{path}\n\nBuild the full solution first so the referenced executable is copied to the EditorSuite output folder.",
                    "GE2D3D Editor Suite",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                WorkingDirectory = OutputDirectory
            });
        }

        private string FindProjectRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(OutputDirectory);

            for (int i = 0; i < 6 && directory != null; i++)
            {
                if (File.Exists(Path.Combine(directory.FullName, "GE2D3D-MapEditor.sln")))
                    return directory.FullName;

                directory = directory.Parent;
            }

            return OutputDirectory;
        }
    }
}
