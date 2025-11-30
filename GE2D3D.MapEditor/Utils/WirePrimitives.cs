using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GE2D3D.MapEditor.Utils
{
    public static class WirePrimitives
    {
        public static void DrawWireSphere(BasicEffect effect, GraphicsDevice device, Vector3 center, float radius, Color color)
        {
            const int segments = 32;
            var verts = new VertexPositionColor[segments * 3];

            int i = 0;

            // XZ ring
            for (int s = 0; s < segments; s++)
            {
                float a0 = MathF.PI * 2f * (s / (float)segments);
                float a1 = MathF.PI * 2f * ((s + 1) / (float)segments);

                verts[i++] = new VertexPositionColor(center + new Vector3(MathF.Cos(a0) * radius, 0, MathF.Sin(a0) * radius), color);
                verts[i++] = new VertexPositionColor(center + new Vector3(MathF.Cos(a1) * radius, 0, MathF.Sin(a1) * radius), color);
            }

            // XY ring
            for (int s = 0; s < segments; s++)
            {
                float a0 = MathF.PI * 2f * (s / (float)segments);
                float a1 = MathF.PI * 2f * ((s + 1) / (float)segments);

                verts[i++] = new VertexPositionColor(center + new Vector3(MathF.Cos(a0) * radius, MathF.Sin(a0) * radius, 0), color);
                verts[i++] = new VertexPositionColor(center + new Vector3(MathF.Cos(a1) * radius, MathF.Sin(a1) * radius, 0), color);
            }

            // YZ ring
            for (int s = 0; s < segments; s++)
            {
                float a0 = MathF.PI * 2f * (s / (float)segments);
                float a1 = MathF.PI * 2f * ((s + 1) / (float)segments);

                verts[i++] = new VertexPositionColor(center + new Vector3(0, MathF.Cos(a0) * radius, MathF.Sin(a0) * radius), color);
                verts[i++] = new VertexPositionColor(center + new Vector3(0, MathF.Cos(a1) * radius, MathF.Sin(a1) * radius), color);
            }

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, verts, 0, verts.Length / 2);
            }
        }
    }
}