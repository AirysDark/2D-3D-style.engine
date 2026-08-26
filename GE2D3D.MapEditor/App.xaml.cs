using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using Gu.Localization;
using Prism.DryIoc;
using Prism.Ioc;

namespace GE2D3D.MapEditor
{
    /// <summary>
    /// WPF application entry point for the GE2D3D editor.
    /// </summary>
    public partial class App : PrismApplication
    {
        static App()
        {
            if (!Translator.Cultures.Any())
            {
                Translator.Culture = CultureInfo.CurrentUICulture;
                return;
            }

            var system = CultureInfo.CurrentUICulture;
            var match = Translator.Cultures.FirstOrDefault(c => c.Name == system.Name)
                        ?? Translator.Cultures.FirstOrDefault(c =>
                            c.TwoLetterISOLanguageName == system.TwoLetterISOLanguageName);
            var fallback = Translator.Cultures.FirstOrDefault(c => c.Name == "en-US")
                           ?? Translator.Cultures.First();

            Translator.Culture = match ?? fallback;
        }

        protected override Window CreateShell()
        {
            return new MainWindow();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        private static string GetContentRoot()
        {
            // During development, create Content in the repository root rather than
            // inside bin\Debug\net6.0-windows. Walk upward until the solution is found.
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "GE2D3D-MapEditor.sln")))
                    return Path.Combine(directory.FullName, "Content");

                directory = directory.Parent;
            }

            // Installed builds have no solution file, so use the executable folder.
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");
        }

        /// <summary>
        /// Creates the complete P3D-compatible Content layout used by the current map importer.
        /// Existing files and folders are never overwritten.
        /// </summary>
        private static void EnsureContentStructure()
        {
            var contentRoot = GetContentRoot();

            string[] folders =
            {
                "Data",
                "Data/maps",
                "Data/Scripts",
                "Data/Items",
                "Data/Moves",
                "Data/Types",
                "Effects",
                "GUI",
                "Items",
                "Localization",
                "Pokemon",
                "SkyDomeResource",
                "Songs",
                "Sounds",
                "Textures"
            };

            Directory.CreateDirectory(contentRoot);
            foreach (var folder in folders)
            {
                Directory.CreateDirectory(Path.Combine(contentRoot, folder));
            }
        }

        protected override void OnInitialized()
        {
            EnsureContentStructure();
            base.OnInitialized();
        }
    }
}
