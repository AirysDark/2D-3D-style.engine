using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Utils
{
    public static class GridSnap
    {
        public static Vector3 Snap(Vector3 position, float gridSize)
        {
            if (gridSize <= 0f)
                return position;

            float SnapCoord(float v)
                => (float)System.Math.Round(v / gridSize) * gridSize;

            return new Vector3(
                SnapCoord(position.X),
                SnapCoord(position.Y),
                SnapCoord(position.Z)
            );
        }
    }
}