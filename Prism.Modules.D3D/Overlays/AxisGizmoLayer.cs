using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Prism.Modules.D3D.Cameras;
using Prism.Modules.D3D.Rendering;

namespace Prism.Modules.D3D.Overlays
{
    /// <summary>
    /// Simple world-space axis gizmo (X/Y/Z lines) drawn at the origin.
    /// </summary>
    public class AxisGizmoLayer : IRenderLayer
    {
        private BasicEffect _effect;
        private VertexPositionColor[] _vertices;

        public bool Enabled { get; set; } = true;
        public int Order { get; set; } = 10;

        public float AxisLength { get; set; } = 64f;

        public Color XColor { get; set; } = Color.Red;
        public Color YColor { get; set; } = Color.Green;
        public Color ZColor { get; set; } = Color.Blue;

        public void Draw(GraphicsDevice device, GameTime gameTime, ICamera camera)
        {
            if (!Enabled || device == null || camera == null)
                return;

            if (_effect == null || _effect.GraphicsDevice != device)
            {
                _effect?.Dispose();
                _effect = new BasicEffect(device)
                {
                    VertexColorEnabled = true,
                    LightingEnabled = false
                };
            }

            if (_vertices == null || _vertices.Length != 6)
            {
                _vertices = new[]
                {
                    // X axis
                    new VertexPositionColor(Vector3.Zero, XColor),
                    new VertexPositionColor(new Vector3(AxisLength, 0, 0), XColor),

                    // Y axis
                    new VertexPositionColor(Vector3.Zero, YColor),
                    new VertexPositionColor(new Vector3(0, AxisLength, 0), YColor),

                    // Z axis
                    new VertexPositionColor(Vector3.Zero, ZColor),
                    new VertexPositionColor(new Vector3(0, 0, AxisLength), ZColor),
                };
            }

            _effect.World = Matrix.Identity;
            _effect.View = camera.View;
            _effect.Projection = camera.Projection;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, _vertices, 0, _vertices.Length / 2);
            }
        }
    }
}
