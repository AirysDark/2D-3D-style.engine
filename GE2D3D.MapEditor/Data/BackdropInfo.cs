using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Data
{
    public class BackdropInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public string BackdropType { get; set; }
        public string TexturePath { get; set; }
        public Rectangle TextureRectangle { get; set; }
        public string Trigger { get; set; } = "";
    }
}