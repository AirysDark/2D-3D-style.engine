using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Components.Render
{
    /// <summary>
    /// Simple directional light settings for the editor renderer.
    /// Can be driven from map data or editor UI.
    /// </summary>
    public sealed class DirectionalLightSettings
    {
        /// <summary>
        /// Master enable for directional lighting.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Light direction in world space (points FROM light TOWARD scene).
        /// </summary>
        public Vector3 Direction { get; set; } = new Vector3(0f, -1f, 0.5f);

        /// <summary>
        /// Diffuse light color.
        /// </summary>
        public Color DiffuseColor { get; set; } = Color.White;

        /// <summary>
        /// Specular light color.
        /// </summary>
        public Color SpecularColor { get; set; } = Color.White;

        /// <summary>
        /// Ambient light color for the scene.
        /// </summary>
        public Color AmbientColor { get; set; } = new Color(40, 40, 40);

        /// <summary>
        /// Overall intensity multiplier (0..something).
        /// </summary>
        public float Intensity { get; set; } = 1.0f;
    }
}