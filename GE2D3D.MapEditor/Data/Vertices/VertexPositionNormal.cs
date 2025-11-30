using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GE2D3D.MapEditor.Data.Vertices
{
    /// <summary>
    /// Custom vertex type: Position + Normal
    /// </summary>
    public struct VertexPositionNormal : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;

        public VertexPositionNormal(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }

        // 3 floats (pos) + 3 floats (normal) = 24 bytes
        public const int SizeInBytes = 24;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
        );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}