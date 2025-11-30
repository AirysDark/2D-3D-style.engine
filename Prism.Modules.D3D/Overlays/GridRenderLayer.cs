using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Prism.Modules.D3D.Cameras;

namespace Prism.Modules.D3D.Overlays
{
    /// <summary>
    /// Simple 2D grid render layer, intended for tile-map style editors.
    /// </summary>
    public class GridRenderLayer : Rendering.IRenderLayer
    {
        private BasicEffect _effect;
        private VertexPositionColor[] _vertices;

        public bool Enabled { get; set; } = true;
        public int Order { get; set; } = 0;

        /// <summary>
        /// World-space rectangle that defines the grid area.
        /// </summary>
        public Rectangle GridBounds { get; set; } = new Rectangle(-256, -256, 512, 512);

        /// <summary>
        /// Cell size in world units (e.g. tile size).
        /// </summary>
        public int CellSize { get; set; } = 32;

        public Color LineColor { get; set; } = new Color(64, 64, 64, 255);
        public Color AxisColor { get; set; } = new Color(128, 128, 128, 255);

        public void Draw(GraphicsDevice device, GameTime gameTime, ICamera camera)
        {
            if (!Enabled)
                return;

            if (CellSize <= 0 || GridBounds.Width <= 0 || GridBounds.Height <= 0)
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

            BuildVertices();

            _effect.World = Matrix.Identity;
            _effect.View = camera.View;
            _effect.Projection = camera.Projection;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, _vertices, 0, _vertices.Length / 2);
            }
        }

        private void BuildVertices()
        {
            var lines = new List<VertexPositionColor>();

            int left = GridBounds.Left;
            int right = GridBounds.Right;
            int top = GridBounds.Top;
            int bottom = GridBounds.Bottom;

            // Vertical lines.
            for (int x = left; x <= right; x += CellSize)
            {
                var color = x == 0 ? AxisColor : LineColor;
                lines.Add(new VertexPositionColor(new Vector3(x, top, 0f), color));
                lines.Add(new VertexPositionColor(new Vector3(x, bottom, 0f), color));
            }

            // Horizontal lines.
            for (int y = top; y <= bottom; y += CellSize)
            {
                var color = y == 0 ? AxisColor : LineColor;
                lines.Add(new VertexPositionColor(new Vector3(left, y, 0f), color));
                lines.Add(new VertexPositionColor(new Vector3(right, y, 0f), color));
            }

            _vertices = lines.ToArray();
        }
    }
}
