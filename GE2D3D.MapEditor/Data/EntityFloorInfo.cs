using System.Drawing;

namespace GE2D3D.MapEditor.Data
{
    public class EntityFloorInfo : EntityInfo
    {
        public new Size Size { get; set; }   // hide base class property
        public bool RemoveFloor { get; set; }
        public bool HasSnow { get; set; } = true;
        public bool HasSand { get; set; } = true;
        public bool HasIce { get; set; }

        public EntityFloorInfo()
        {
            EntityID = "Floor";
        }
    }
}