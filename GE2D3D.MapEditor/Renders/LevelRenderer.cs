using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework.Graphics;

using GE2D3D.MapEditor.Data.Models;
using GE2D3D.MapEditor.Utils;
using GE2D3D.MapEditor.World;

namespace GE2D3D.MapEditor.Renders
{
    public class LevelRenderer
    {
        protected BaseModelListRenderer StaticRenderer;
        protected BaseModelListRenderer DynamicRenderer;

        public void HandleModels(List<BaseModel> models)
        {
            bool isStatic(BaseModel model)
            {
                if (model is BillModel)
                    return false;

                return true;
            }
            bool isDynamic(BaseModel model)
            {
                return !isStatic(model);
            }

            StaticRenderer = new StaticModelListRenderer();
            StaticRenderer.AddModels(models.Where(isStatic).ToList());

            DynamicRenderer = new DynamicModelListRenderer();
            DynamicRenderer.AddModels(models.Where(isDynamic).ToList());
        }

        public void Setup(GraphicsDevice graphicsDevice)
        {
            StaticRenderer.Setup(graphicsDevice);
            DynamicRenderer.Setup(graphicsDevice);
        }

        public void Draw(Level level, BasicEffect basicEffect, AlphaTestEffect alphaTestEffect)
        {
            StaticRenderer.Draw(level, basicEffect, alphaTestEffect);
            DynamicRenderer.Draw(level, basicEffect, alphaTestEffect);
        }

        public void Dispose()
        {
            TextureHandler.Dispose();
        }
    }
}