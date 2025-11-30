using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Prism.Modules.D3D.Cameras;

namespace Prism.Modules.D3D.Rendering
{
    /// <summary>
    /// Manages a collection of render layers and draws them in order.
    /// </summary>
    public class RenderLayerCollection
    {
        private readonly List<IRenderLayer> _layers = new List<IRenderLayer>();

        public IReadOnlyList<IRenderLayer> Layers => _layers;

        public void AddLayer(IRenderLayer layer)
        {
            if (layer == null || _layers.Contains(layer))
                return;

            _layers.Add(layer);
        }

        public bool RemoveLayer(IRenderLayer layer)
        {
            if (layer == null)
                return false;

            return _layers.Remove(layer);
        }

        public void Clear() => _layers.Clear();

        public void Draw(GraphicsDevice device, GameTime gameTime, ICamera camera)
        {
            foreach (var layer in _layers.Where(l => l.Enabled).OrderBy(l => l.Order))
            {
                layer.Draw(device, gameTime, camera);
            }
        }
    }
}
