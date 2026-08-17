using System;
using System.Diagnostics;
using System.IO;

namespace GE2D3D.EditorSuite
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Look for the editor exe in its normal bin folder
            string solutionRoot = Directory.GetParent(
                AppDomain.CurrentDomain.BaseDirectory
            )?.Parent?.Parent?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;

            string editorPath = Path.Combine(
                solutionRoot,
                "GE2D3D.MapEditor",
                "bin",
                "Debug",   // or "Release" if you prefer
                "GE2D3D.MapEditor.exe"
            );

            if (!File.Exists(editorPath))
            {
                Console.WriteLine("Could not find GE2D3D.MapEditor.exe at:");
                Console.WriteLine(editorPath);
                return;
            }

            Process.Start(new ProcessStartInfo(editorPath)
            {
                UseShellExecute = true
            });
        }
    }
}
