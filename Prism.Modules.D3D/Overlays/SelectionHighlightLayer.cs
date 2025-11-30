using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Prism.Modules.D3D.Cameras;
using Prism.Modules.D3D.Rendering;

namespace Prism.Modules.D3D.Overlays
{
    /// <summary>
    /// Draws 2D and 3D selection highlights.
    /// The editor can supply a list of rectangles and/or bounding boxes.
    /// </summary>
    public class SelectionHighlightLayer : IRenderLayer
    {
        private BasicEffect _effect;

        public bool Enabled { get; set; } = true;
        public int Order { get; set; } = 5;

        /// <summary>
        /// Provider for 2D world-space rectangles (e.g. tiles).
        /// </summary>
        public Func<IEnumerable<Rectangle>> RectangleProvider { get; set; }

        /// <summary>
        /// Provider for 3D bounding boxes (e.g. props/entities).
        /// </summary>
        public Func<IEnumerable<BoundingBox>> BoundingBoxProvider { get; set; }

        public Color RectangleColor { get; set; } = new Color(255, 255, 0, 160);
        public Color BoundingBoxColor { get; set; } = new Color(0, 255, 255, 192);

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

            _effect.World = Matrix.Identity;
            _effect.View = camera.View;
            _effect.Projection = camera.Projection;

            var rects = RectangleProvider?.Invoke();
            var boxes = BoundingBoxProvider?.Invoke();

            // Draw rectangles as simple box outlines on Z=0.
            if (rects != null)
            {
                foreach (var r in rects)
                {
                    DrawRectangle(device, r, RectangleColor);
                }
            }

            // Draw 3D bounding boxes.
            if (boxes != null)
            {
                foreach (var b in boxes)
                {
                    DrawBoundingBox(device, b, BoundingBoxColor);
                }
            }
        }

        private void DrawRectangle(GraphicsDevice device, Rectangle rect, Color color)
        {
            var z = 0f;
            var tl = new Vector3(rect.Left, rect.Top, z);
            var tr = new Vector3(rect.Right, rect.Top, z);
            var br = new Vector3(rect.Right, rect.Bottom, z);
            var bl = new Vector3(rect.Left, rect.Bottom, z);

            var verts = new[]
            {
                new VertexPositionColor(tl, color), new VertexPositionColor(tr, color),
                new VertexPositionColor(tr, color), new VertexPositionColor(br, color),
                new VertexPositionColor(br, color), new VertexPositionColor(bl, color),
                new VertexPositionColor(bl, color), new VertexPositionColor(tl, color)
            };

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, verts, 0, verts.Length / 2);
            }
        }

        private void DrawBoundingBox(GraphicsDevice device, BoundingBox box, Color color)
        {
            var corners = box.GetCorners();

            int[] indices =
            {
                // bottom
                0, 1, 1, 2, 2, 3, 3, 0,
                // top
                4, 5, 5, 6, 6, 7, 7, 4,
                // verticals
                0, 4, 1, 5, 2, 6, 3, 7
            };

            var verts = new VertexPositionColor[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                verts[i] = new VertexPositionColor(corners[indices[i]], color);
            }

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, verts, 0, verts.Length / 2);
            }
        }
    }
}
