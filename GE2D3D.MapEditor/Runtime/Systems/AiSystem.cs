using System;
using Microsoft.Xna.Framework;
using GE2D3D.MapEditor.Runtime.Components;

namespace GE2D3D.MapEditor.Runtime.Systems
{
    /// <summary>
    /// Minimal AI system with Idle/Wander/Chase behaviours.
    /// </summary>
    public class AiSystem
    {
        public void Update(RuntimeScene scene, float dt)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));

            // Find player transform once.
            TransformComponent? playerTransform = null;
            foreach (var e in scene.Entities)
            {
                if (e.HasComponent<PlayerComponent>() && e.TryGetComponent(out TransformComponent tr))
                {
                    playerTransform = tr;
                    break;
                }
            }

            foreach (var entity in scene.Entities)
            {
                if (!entity.TryGetComponent(out AiComponent ai) ||
                    !entity.TryGetComponent(out TransformComponent transform))
                    continue;

                if (!ai.HomeInitialized)
                {
                    ai.HomePosition = transform.Position;
                    ai.HomeInitialized = true;
                }

                Vector3 target = transform.Position;

                switch (ai.Behaviour)
                {
                    case AiBehaviour.Idle:
                        // Stay at home position.
                        target = ai.HomePosition;
                        break;

                    case AiBehaviour.Wander:
                        // Simple wander: move towards home and hover around.
                        target = ai.HomePosition;
                        break;

                    case AiBehaviour.Chase:
                        if (playerTransform != null)
                            target = playerTransform.Position;
                        break;
                }

                var dir = target - transform.Position;
                if (dir.LengthSquared() > 0.001f)
                {
                    dir.Normalize();
                    transform.Position += dir * ai.MoveSpeed * dt;
                }
            }
        }
    }
}
