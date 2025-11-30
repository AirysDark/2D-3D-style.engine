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

        protected override Window CreateShell()
        {
            // If your Prism version doesn't expose a Container property,
            // just construct the shell directly.
            return new MainWindow();
            // If later you want DI, we can switch to a ContainerLocator-based resolve.
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register services, view models, etc. here when needed
            // e.g. containerRegistry.RegisterSingleton<IGraphicsDeviceService, GraphicsDeviceServiceDX>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }
    }
}