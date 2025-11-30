using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GE2D3D.MapEditor.Components.Debug
{
    /// <summary>
    /// Simple debug batch to draw axis-aligned bounding boxes as wireframe.
    /// </summary>
    public sealed class DebugVolumeBatch : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly BasicEffect _effect;

        private readonly List<VertexPositionColor> _vertices = new();
        private readonly List<short> _indices = new();

        public DebugVolumeBatch(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));

            _effect = new BasicEffect(_graphicsDevice)
            {
                VertexColorEnabled = true,
                TextureEnabled = false,
                LightingEnabled = false
            };
        }

        public void Begin(Matrix view, Matrix projection)
        {
            _vertices.Clear();
            _indices.Clear();

            _effect.View = view;
            _effect.Projection = projection;
            _effect.World = Matrix.Identity;
        }

        public void AddBox(BoundingBox box, Color color)
        {
            // 8 corners of the AABB
            var corners = box.GetCorners(); // Vector3[8]

            short baseIndex = (short)_vertices.Count;

            for (int i = 0; i < 8; i++)
                _vertices.Add(new VertexPositionColor(corners[i], color));

            // 12 edges (line list)
            AddEdge((short)(baseIndex + 0), (short)(baseIndex + 1));
            AddEdge((short)(baseIndex + 1), (short)(baseIndex + 2));
            AddEdge((short)(baseIndex + 2), (short)(baseIndex + 3));
            AddEdge((short)(baseIndex + 3), (short)(baseIndex + 0));

            AddEdge((short)(baseIndex + 4), (short)(baseIndex + 5));
            AddEdge((short)(baseIndex + 5), (short)(baseIndex + 6));
            AddEdge((short)(baseIndex + 6), (short)(baseIndex + 7));
            AddEdge((short)(baseIndex + 7), (short)(baseIndex + 4));

            AddEdge((short)(baseIndex + 0), (short)(baseIndex + 4));
            AddEdge((short)(baseIndex + 1), (short)(baseIndex + 5));
            AddEdge((short)(baseIndex + 2), (short)(baseIndex + 6));
            AddEdge((short)(baseIndex + 3), (short)(baseIndex + 7));
        }

        private void AddEdge(short a, short b)
        {
            _indices.Add(a);
            _indices.Add(b);
        }

        public void Draw()
        {
            if (_vertices.Count == 0 || _indices.Count == 0)
                return;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                _graphicsDevice.DrawUserIndexedPrimitives(
                    primitiveType: PrimitiveType.LineList,
                    vertexData: _vertices.ToArray(),
                    vertexOffset: 0,
                    numVertices: _vertices.Count,
                    indexData: _indices.ToArray(),
                    indexOffset: 0,
                    primitiveCount: _indices.Count / 2);
            }
        }

        public void Dispose()
        {
            _effect.Dispose();
        }
    }
}