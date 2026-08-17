
using System;
using Microsoft.Xna.Framework;
using GE2D3D.MapEditor.Runtime.Components;

namespace GE2D3D.MapEditor.Runtime.Systems
{
    /// <summary>
    /// Applies high-level runtime input to the player entity.
    /// </summary>
    public class PlayerControllerSystem
    {
        public void Update(RuntimeScene scene, RuntimeInputSnapshot input, float dt)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));

            RuntimeEntity? player = null;
            foreach (var e in scene.Entities)
            {
                if (e.HasComponent<PlayerComponent>())
                {
                    player = e;
                    break;
                }
            }

            if (player == null)
                return;

            if (!player.TryGetComponent(out TransformComponent transform) ||
                !player.TryGetComponent(out CharacterControllerComponent controller))
                return;

            // Convert 2D input (XZ plane) into world space movement.
            var move = input.Move;
            var desired = new Vector3(move.X, 0, -move.Y); // screen forward = -Z

            if (desired.LengthSquared() > 0.0001f)
            {
                desired.Normalize();
                controller.Velocity.X = desired.X * controller.MoveSpeed;
                controller.Velocity.Z = desired.Z * controller.MoveSpeed;
            }
            else
            {
                controller.Velocity.X = 0;
                controller.Velocity.Z = 0;
            }

            if (input.Jump && controller.IsGrounded)
            {
                controller.Velocity.Y = 5.0f;
                controller.IsGrounded = false;
            }
        }
    }
}
