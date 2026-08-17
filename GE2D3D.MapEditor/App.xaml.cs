using System.Globalization;
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
        // Static constructor runs once before anything else
        static App()
        {
            // If Gu.Localization has no registered cultures, just use system UI culture
            if (!Translator.Cultures.Any())
            {
                Translator.Culture = CultureInfo.CurrentUICulture;
                return;
            }

            var system = CultureInfo.CurrentUICulture;

            // Try exact match first
            var match = Translator.Cultures
                                  .FirstOrDefault(c => c.Name == system.Name);

            // Then try same language (en, de, etc.)
            if (match == null)
            {
                match = Translator.Cultures
                                  .FirstOrDefault(c =>
                                      c.TwoLetterISOLanguageName ==
                                      system.TwoLetterISOLanguageName);
            }

            // Fallback: en-US if available, otherwise first culture
            var fallback = Translator.Cultures.FirstOrDefault(c => c.Name == "en-US")
                           ?? Translator.Cultures.First();

            Translator.Culture = match ?? fallback;
        }

        /// <summary>
        /// Creates the main shell window for the editor.
        /// </summary>
        protected override Window CreateShell()
        {
            // For now, construct the shell directly.
            // If you later want DI-based construction, we can switch to resolving via the container.
            return new MainWindow();
        }

        /// <summary>
        /// Register services, singletons and other dependencies here.
        /// </summary>
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Example for later:
            // containerRegistry.RegisterSingleton<IMyService, MyService>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            // Place any startup logic here if needed (e.g., reading last project root, etc.)
        }
    }
}
