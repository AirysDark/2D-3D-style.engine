using System;
using GE2D3D.MapEditor.Runtime.Components;

namespace GE2D3D.MapEditor.Runtime.Systems
{
    /// <summary>
    /// Drives simple animation state timers.
    /// Rendering systems can query AnimationComponent to decide what to draw.
    /// </summary>
    public class AnimationSystem
    {
        public void Update(RuntimeScene scene, float dt)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));

            foreach (var entity in scene.Entities)
            {
                if (!entity.TryGetComponent(out AnimationComponent anim))
                    continue;

                anim.Time += dt * anim.Speed;
            }
        }
    }
}
