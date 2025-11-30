using Microsoft.Xna.Framework;

namespace Prism.Modules.D3D.Settings
{
    /// <summary>
    /// Default implementation of IRenderSettings.
    /// </summary>
    public class RenderSettings : IRenderSettings
    {
        public Color ClearColor { get; set; } = new Color(32, 32, 32, 255);
        public bool EnableWireframe { get; set; }
        public bool VSync { get; set; } = true;
    }
}
