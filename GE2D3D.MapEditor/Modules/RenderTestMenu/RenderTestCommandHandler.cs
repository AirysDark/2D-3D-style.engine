using System;
using System.Diagnostics;
using System.IO;
using GE2D3D.MapEditor.Utils;

namespace GE2D3D.MapEditor.Modules.RenderTestMenu
{
    /// <summary>
    /// Minimal helper for launching RenderTest.exe from anywhere in the editor,
    /// without relying on Gemini command infrastructure.
    /// 
    /// NOTE:
    /// Right now your main integration for RenderTest lives in MainWindow.xaml.cs
    /// (OnRenderTestRunLastMapClick / OnRenderTestOpenAndRunClick).
    /// This class is just a utility you can call from other code later if you want.
    /// </summary>
    public static class RenderTestLauncher
    {
        private static string GetLastMapPathFile()
        {
            var root = EditorPaths.GetProjectRoot();
            return Path.Combine(root, "last_map.txt");
        }

        public static void SaveLastMapPath(string path)
        {
            try
            {
                var file = GetLastMapPathFile();
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, path ?? string.Empty);
                Debug.WriteLine($"[RenderTestLauncher] Saved last map path: {path}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RenderTestLauncher] Failed to save last_map.txt: {ex.Message}");
            }
        }

        public static void LaunchRenderTest()
        {
            try
            {
                var exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RenderTest.exe");
                if (!File.Exists(exe))
                {
                    Debug.WriteLine("[RenderTestLauncher] RenderTest.exe not found next to the editor executable.");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RenderTestLauncher] Failed to launch RenderTest.exe: {ex.Message}");
            }
        }
    }
}
