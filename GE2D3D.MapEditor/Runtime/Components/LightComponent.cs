
using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Runtime.Components
{
    public enum LightType
    {
        Directional,
        Point,
        Spot
    }

    /// <summary>
    /// Simple light component used by the runtime renderer.
    /// </summary>
    public class LightComponent : IRuntimeComponent
    {
        public LightType Type = LightType.Point;
        public Vector3 Color = Vector3.One;
        public float Intensity = 1.0f;
        public float Radius = 8.0f;
        public Vector3 Direction = -Vector3.UnitY;
    }
}
