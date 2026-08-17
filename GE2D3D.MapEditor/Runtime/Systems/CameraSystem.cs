
using System;
using Microsoft.Xna.Framework;
using GE2D3D.MapEditor.Components.Camera;
using GE2D3D.MapEditor.Runtime.Components;

namespace GE2D3D.MapEditor.Runtime.Systems
{
    /// <summary>
    /// Chooses an active camera entity and writes its transform into the shared editor camera.
    /// </summary>
    public class CameraSystem
    {
        private readonly BaseCamera _camera;

        public CameraSystem(BaseCamera camera)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        }

        public void Update(RuntimeScene scene, float dt)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));

            RuntimeEntity? cameraEntity = null;
            foreach (var e in scene.Entities)
            {
                if (e.TryGetComponent(out CameraComponent cameraComp) && cameraComp.IsMainCamera)
                {
                    cameraEntity = e;
                    break;
                }
            }

            if (cameraEntity == null)
            {
                // No camera entity; do nothing and leave the editor camera alone.
                return;
            }

            if (!cameraEntity.TryGetComponent(out TransformComponent transform) ||
                !cameraEntity.TryGetComponent(out CameraComponent cam))
                return;

            var position = transform.Position + cam.Offset;
            var target = transform.Position + cam.LookAtOffset;

            _camera.Position = Vector3.Lerp(_camera.Position, position, MathHelper.Clamp(dt * 10f, 0f, 1f));
            _camera.Target = Vector3.Lerp(_camera.Target, target, MathHelper.Clamp(dt * 10f, 0f, 1f));
        }
    }
}
