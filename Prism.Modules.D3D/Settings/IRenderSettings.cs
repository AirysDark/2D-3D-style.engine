using Microsoft.Xna.Framework;

namespace Prism.Modules.D3D.Settings
{
    /// <summary>
    /// Simple rendering settings abstraction for the editor.
    /// </summary>
    public interface IRenderSettings
    {
        Color ClearColor { get; set; }
        bool EnableWireframe { get; set; }
        bool VSync { get; set; }
    }
}
