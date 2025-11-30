using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GE2D3D.MapEditor.Components.Render;
using GE2D3D.MapEditor.Data.Vertices;
using GE2D3D.MapEditor.World;

namespace GE2D3D.MapEditor.Renders
{
    // NOTE: the space here is important: "public abstract", not "publicabstract"
    public abstract class BaseVertexRenderer
    {
        public List<VertexPositionNormalColorTexture> Vertices { get; }
        public List<int> Indices { get; }

        public Texture2D Atlas { get; }

        public VertexBuffer StaticVertexBuffer;
        public IndexBuffer StaticIndexBuffer;

        protected BaseVertexRenderer(
            List<VertexPositionNormalColorTexture> vertices,
            List<int> indices,
            Texture2D atlas)
        {
            Vertices = vertices;
            Indices = indices;
            Atlas = atlas;
        }

        public void Setup(GraphicsDevice graphicsDevice)
        {
            StaticVertexBuffer = new VertexBuffer(
                graphicsDevice,
                typeof(VertexPositionNormalColorTexture),
                Vertices.Count,
                BufferUsage.WriteOnly);

            StaticVertexBuffer.SetData(Vertices.ToArray());

            // if you really want to use indexed drawing later, you should use Indices.Count here
            StaticIndexBuffer = new IndexBuffer(
                graphicsDevice,
                typeof(int),
                Vertices.Count,
                BufferUsage.WriteOnly);

            StaticIndexBuffer.SetData(Indices.ToArray());
        }
    }

    public class OpaqueVertexRenderer : BaseVertexRenderer
    {
        public OpaqueVertexRenderer(
            List<VertexPositionNormalColorTexture> vertices,
            List<int> indices,
            Texture2D atlas)
            : base(vertices, indices, atlas)
        {
        }

        public void Draw(Level level, BasicEffect basicEffect, CullMode cullMode = CullMode.CullClockwiseFace)
        {
            var graphicsDevice = basicEffect.GraphicsDevice;

            var rasterizerState = graphicsDevice.RasterizerState;
            var blendState = graphicsDevice.BlendState;
            var depthStencilState = graphicsDevice.DepthStencilState;

            graphicsDevice.SetVertexBuffer(StaticVertexBuffer);
            graphicsDevice.Indices = StaticIndexBuffer;
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;

            switch (cullMode)
            {
                case CullMode.None:
                    graphicsDevice.RasterizerState = RasterizerState.CullNone;
                    break;

                case CullMode.CullClockwiseFace:
                    graphicsDevice.RasterizerState = RasterizerState.CullClockwise;
                    break;

                case CullMode.CullCounterClockwiseFace:
                    graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                    break;
            }

            basicEffect.Texture = Atlas;
            basicEffect.TextureEnabled = true;

            foreach (var effectPass in basicEffect.CurrentTechnique.Passes)
            {
                effectPass.Apply();

                graphicsDevice.DrawPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    StaticVertexBuffer.VertexCount / 3);

                // If you want to use indexed drawing instead:
                // graphicsDevice.DrawIndexedPrimitives(
                //     PrimitiveType.TriangleList,
                //     0,
                //     0,
                //     Vertices.Count,
                //     0,
                //     Indices.Count / 3);

                Render.StaticDrawCalls++;
            }

            graphicsDevice.RasterizerState = rasterizerState;
            graphicsDevice.BlendState = blendState;
            graphicsDevice.DepthStencilState = depthStencilState;
        }
    }

    public class TransparentVertexRenderer : BaseVertexRenderer
    {
        private DepthStencilState StencilWriteOnly { get; } = new DepthStencilState
        {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true,
            StencilFunction = CompareFunction.Always,
            StencilPass = StencilOperation.Replace,
            ReferenceStencil = 0,
            StencilEnable = true
        };

        private DepthStencilState StencilReadOnly { get; } = new DepthStencilState
        {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = false,
            StencilFunction = CompareFunction.Equal,
            StencilPass = StencilOperation.Keep,
            ReferenceStencil = 0,
            StencilEnable = true
        };

        public TransparentVertexRenderer(
            List<VertexPositionNormalColorTexture> vertices,
            List<int> indices,
            Texture2D atlas)
            : base(vertices, indices, atlas)
        {
        }

        public void Draw(
            Level level,
            BasicEffect basicEffect,
            AlphaTestEffect alphaEffect,
            CullMode cullMode = CullMode.CullClockwiseFace)
        {
            // Two-pass stencil-based rendering idea:
            // 1) AlphaTestEffect with color writes limited to alpha ? writes stencil mask
            // 2) BasicEffect with Color writes, stencil test enabled

            var graphicsDevice = basicEffect.GraphicsDevice;

            var rasterizerState = graphicsDevice.RasterizerState;
            var blendState = graphicsDevice.BlendState;
            var depthStencilState = graphicsDevice.DepthStencilState;

            graphicsDevice.SetVertexBuffer(StaticVertexBuffer);
            graphicsDevice.Indices = StaticIndexBuffer;

            switch (cullMode)
            {
                case CullMode.None:
                    graphicsDevice.RasterizerState = RasterizerState.CullNone;
                    break;

                case CullMode.CullClockwiseFace:
                    graphicsDevice.RasterizerState = RasterizerState.CullClockwise;
                    break;

                case CullMode.CullCounterClockwiseFace:
                    graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                    break;
            }

            basicEffect.Texture = Atlas;
            alphaEffect.Texture = Atlas;

            // First pass: write stencil from alpha test
            graphicsDevice.DepthStencilState = StencilWriteOnly;
            graphicsDevice.BlendState = new BlendState
            {
                ColorWriteChannels = ColorWriteChannels.Alpha,
                ColorWriteChannels1 = ColorWriteChannels.Alpha,
                ColorWriteChannels2 = ColorWriteChannels.Alpha,
                ColorWriteChannels3 = ColorWriteChannels.Alpha
            };

            foreach (var effectPass in alphaEffect.CurrentTechnique.Passes)
            {
                effectPass.Apply();
                graphicsDevice.DrawPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    StaticVertexBuffer.VertexCount / 3);

                Render.StaticDrawCalls++;
            }

            // Second pass: read stencil, draw color
            graphicsDevice.DepthStencilState = StencilReadOnly;
            graphicsDevice.BlendState = BlendState.AlphaBlend;

            foreach (var effectPass in basicEffect.CurrentTechnique.Passes)
            {
                effectPass.Apply();
                graphicsDevice.DrawPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    StaticVertexBuffer.VertexCount / 3);

                Render.StaticDrawCalls++;
            }

            graphicsDevice.RasterizerState = rasterizerState;
            graphicsDevice.BlendState = blendState;
            graphicsDevice.DepthStencilState = depthStencilState;
        }
    }
}