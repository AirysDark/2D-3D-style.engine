using System;

namespace GE2D3D.MapEditor.World
{
    /// <summary>
    /// Flags controlling what parts of the level are drawn.
    /// This is editor-only, not game logic.
    /// </summary>
    public sealed class LayerVisibility
    {
        public bool ShowGeometry { get; set; } = true;
        public bool ShowProps { get; set; } = true;
        public bool ShowCollision { get; set; } = true;
        public bool ShowLights { get; set; } = true;
        public bool ShowTriggers { get; set; } = true;
        public bool ShowSkybox { get; set; } = true;
        public bool ShowGrid { get; set; } = true;

        public static LayerVisibility CreateDefault()
        {
            return new LayerVisibility
            {
                ShowGeometry = true,
                ShowProps = true,
                ShowCollision = true,
                ShowLights = true,
                ShowTriggers = true,
                ShowSkybox = true,
                ShowGrid = true
            };
        }

        public LayerVisibility Clone()
        {
            return (LayerVisibility)MemberwiseClone();
        }
    }
}