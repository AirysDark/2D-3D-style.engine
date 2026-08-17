
using System;
using GE2D3D.MapEditor.Runtime.Components;

namespace GE2D3D.MapEditor.Runtime.Systems
{
    /// <summary>
    /// Collects light information from the runtime scene.
    /// For now this just exists as a placeholder so Track L5 has a concrete hook.
    /// </summary>
    public class LightSystem
    {
        public void Update(RuntimeScene scene, float dt)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));

            // In the future this could compute aggregate light data or feed into Render.
            // For now we simply iterate to ensure the scene is wired correctly.
            foreach (var e in scene.Entities)
            {
                if (e.TryGetComponent(out LightComponent light))
                {
                    _ = light;
                }
            }
        }
    }
}
