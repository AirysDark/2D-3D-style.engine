using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GE2D3D.MapEditor.Runtime.Components;

namespace GE2D3D.MapEditor.Runtime.Systems
{
    /// <summary>
    /// Extremely lightweight placeholder physics system.
    /// It integrates simple velocities and does AABB vs AABB overlap checks.
    /// </summary>
    public class PhysicsSystem
    {
        // Tracks active trigger overlaps so we can generate enter/exit events.
        private readonly Dictionary<(RuntimeEntity trigger, RuntimeEntity other), bool> _triggerOverlaps = new();

        public void Update(RuntimeScene scene, float dt, ScriptSystem scriptSystem)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            if (scriptSystem == null) throw new ArgumentNullException(nameof(scriptSystem));

            // Basic kinematic integration for entities with a character controller.
            foreach (var entity in scene.Entities)
            {
                if (!entity.TryGetComponent(out CharacterControllerComponent controller) ||
                    !entity.TryGetComponent(out TransformComponent transform))
                    continue;

                // Apply gravity
                controller.Velocity += new Vector3(0, controller.Gravity * dt, 0);

                // Integrate position
                transform.Position += controller.Velocity * dt;

                // Extremely naive ground plane at Y = 0
                if (transform.Position.Y <= 0)
                {
                    transform.Position = new Vector3(transform.Position.X, 0, transform.Position.Z);
                    controller.Velocity = new Vector3(controller.Velocity.X, 0, controller.Velocity.Z);
                    controller.IsGrounded = true;
                }
                else
                {
                    controller.IsGrounded = false;
                }
            }

            // Trigger / collider overlap checks: fire script events on enter/exit.
            HandleTriggers(scene, scriptSystem);
        }

        private void HandleTriggers(RuntimeScene scene, ScriptSystem scriptSystem)
        {
            var current = new Dictionary<(RuntimeEntity trigger, RuntimeEntity other), bool>();

            // Collect all triggers and colliders.
            var colliders = new List<(RuntimeEntity entity, ColliderComponent collider)>();
            var triggers = new List<(RuntimeEntity entity, ColliderComponent collider)>();

            foreach (var entity in scene.Entities)
            {
                if (!entity.TryGetComponent(out ColliderComponent collider))
                    continue;

                if (collider.IsTrigger)
                    triggers.Add((entity, collider));
                else
                    colliders.Add((entity, collider));
            }

            // For each trigger, test overlap with non-trigger colliders.
            foreach (var (triggerEntity, triggerCollider) in triggers)
            {
                foreach (var (otherEntity, otherCollider) in colliders)
                {
                    if (triggerEntity == otherEntity)
                        continue;

                    bool overlapping = triggerCollider.Bounds.Intersects(otherCollider.Bounds);
                    var key = (triggerEntity, otherEntity);

                    if (overlapping)
                    {
                        current[key] = true;

                        if (!_triggerOverlaps.ContainsKey(key))
                        {
                            // New overlap: enter.
                            scriptSystem.RaiseTriggerEnter(triggerEntity, otherEntity, scene);
                        }
                    }
                }
            }

            // Any key previously tracked but not present now has exited.
            foreach (var kvp in _triggerOverlaps)
            {
                if (!current.ContainsKey(kvp.Key))
                {
                    var (trigger, other) = kvp.Key;
                    scriptSystem.RaiseTriggerExit(trigger, other, scene);
                }
            }

            _triggerOverlaps.Clear();
            foreach (var kvp in current)
            {
                _triggerOverlaps[kvp.Key] = kvp.Value;
            }
        }
    }
}
