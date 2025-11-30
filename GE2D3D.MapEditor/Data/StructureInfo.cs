using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Data
{
    public class StructureInfo
    {
        public string Map { get; set; }
        public Vector3 Offset { get; set; }
        public int Rotation { get; set; }
        public bool AddNPC { get; set; }
    }
}