using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GE2D3D.MapEditor.Components.Camera;

// Alias to avoid conflict with your GE2D3D.MapEditor.Effect namespace
using XnaEffect = Microsoft.Xna.Framework.Graphics.Effect;

namespace GE2D3D.MapEditor.Components.Render
{
    public sealed class SkyboxRenderer : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly BaseCamera _camera;
        private readonly TextureCube _cubeTexture;
        private readonly XnaEffect _skyboxEffect;

        private readonly VertexBuffer _vertexBuffer;
        private readonly IndexBuffer _indexBuffer;

        public SkyboxRenderer(
            GraphicsDevice graphicsDevice,
            BaseCamera camera,
            TextureCube cubeTexture,
            XnaEffect skyboxEffect)
        {
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _cubeTexture = cubeTexture ?? throw new ArgumentNullException(nameof(cubeTexture));
            _skyboxEffect = skyboxEffect ?? throw new ArgumentNullException(nameof(skyboxEffect));

            (_vertexBuffer, _indexBuffer) = CreateCubeGeometry();
        }

        private (VertexBuffer vbuf, IndexBuffer ibuf) CreateCubeGeometry()
        {
            const float size = 1f;

            var verts = new[]
            {
                new VertexPosition(new Vector3(-size, -size, -size)),
                new VertexPosition(new Vector3(-size, -size,  size)),
                new VertexPosition(new Vector3(-size,  size, -size)),
                new VertexPosition(new Vector3(-size,  size,  size)),
                new VertexPosition(new Vector3( size, -size, -size)),
                new VertexPosition(new Vector3( size, -size,  size)),
                new VertexPosition(new Vector3( size,  size, -size)),
                new VertexPosition(new Vector3( size,  size,  size)),
            };

            short[] indices =
            {
                // -X
                0, 1, 2, 2, 1, 3,
                // +X
                4, 6, 5, 5, 6, 7,
                // -Z
                0, 4, 1, 1, 4, 5,
                // +Z
                2, 3, 6, 6, 3, 7,
                // +Y
                0, 2, 4, 4, 2, 6,
                // -Y
                1, 5, 3, 3, 5, 7
            };

            var vbuf = new VertexBuffer(
                _graphicsDevice,
                typeof(VertexPosition),
                verts.Length,
                BufferUsage.WriteOnly);
            vbuf.SetData(verts);

            var ibuf = new IndexBuffer(
                _graphicsDevice,
                IndexElementSize.SixteenBits,
                indices.Length,
                BufferUsage.WriteOnly);
            ibuf.SetData(indices);

            return (vbuf, ibuf);
        }

        public void Draw()
        {
            // View matrix with translation removed so the cube stays around the camera
            Matrix viewNoTranslation = _camera.ViewMatrix;
            viewNoTranslation.Translation = Vector3.Zero;

            var worldViewProj =
                Matrix.CreateScale(1000f) *
                viewNoTranslation *
                _camera.ProjectionMatrix;

            _skyboxEffect.Parameters["WorldViewProj"]?.SetValue(worldViewProj);
            _skyboxEffect.Parameters["CubeMap"]?.SetValue(_cubeTexture);

            _graphicsDevice.SetVertexBuffer(_vertexBuffer);
            _graphicsDevice.Indices = _indexBuffer;

            var oldRS = _graphicsDevice.RasterizerState;
            var oldDS = _graphicsDevice.DepthStencilState;

            _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            _graphicsDevice.RasterizerState = RasterizerState.CullClockwise;

            foreach (var pass in _skyboxEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    baseVertex: 0,
                    startIndex: 0,
                    primitiveCount: _indexBuffer.IndexCount / 3);
            }

            _graphicsDevice.DepthStencilState = oldDS;
            _graphicsDevice.RasterizerState = oldRS;
        }

        public void Dispose()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }
    }
}