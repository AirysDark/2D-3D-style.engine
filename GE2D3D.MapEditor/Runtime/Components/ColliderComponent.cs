
using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Runtime.Components
{
    /// <summary>
    /// Axis-aligned bounding box collider.
    /// </summary>
    public class ColliderComponent : IRuntimeComponent
    {
        public BoundingBox Bounds;
        public bool IsTrigger;
    }
}
