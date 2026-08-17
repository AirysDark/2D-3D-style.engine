using System;
using GE2D3D.MapEditor.Data;
using GE2D3D.MapEditor.Runtime.Components;
using Microsoft.Xna.Framework;

namespace GE2D3D.MapEditor.Runtime
{
    /// <summary>
    /// Factory that constructs a RuntimeScene from a LevelInfo description.
    /// </summary>
    public static class RuntimeSceneBuilder
    {
        public static RuntimeScene FromLevel(LevelInfo levelInfo)
        {
            if (levelInfo == null) throw new ArgumentNullException(nameof(levelInfo));

            var scene = new RuntimeScene();

            foreach (var entityInfo in levelInfo.Entities)
            {
                // Name fallback: use EntityID when available.
                var name = string.IsNullOrWhiteSpace(entityInfo.EntityID)
                    ? $"Entity{entityInfo.ID}"
                    : entityInfo.EntityID;

                var entity = scene.CreateEntity(entityInfo.ID, name);

                // Transform
                var rotationVec = entityInfo.Rotation;
                var rotation = Quaternion.CreateFromYawPitchRoll(
                    rotationVec.Y,
                    rotationVec.X,
                    rotationVec.Z);

                var transform = new TransformComponent
                {
                    Position = entityInfo.Position,
                    Scale = entityInfo.Scale,
                    Rotation = rotation
                };
                entity.AddComponent(transform);

                // Basic render mapping
                var render = new RenderComponent
                {
                    ModelId = entityInfo.ModelID,
                    TexturePath = entityInfo.TexturePath,
                    Visible = entityInfo.Visible
                };
                entity.AddComponent(render);

                // Collider using the editor collision + size settings.
                if (entityInfo.Collision)
                {
                    var size = entityInfo.Size;
                    var half = size * 0.5f;
                    var min = entityInfo.Position - half;
                    var max = entityInfo.Position + half;

                    var collider = new ColliderComponent
                    {
                        Bounds = new BoundingBox(min, max),
                        IsTrigger = false
                    };
                    entity.AddComponent(collider);
                }

                // Player spawn heuristic: treat the first "PlayerSpawn" entity as the player.
                if (string.Equals(entityInfo.EntityID, "PlayerSpawn", StringComparison.OrdinalIgnoreCase))
                {
                    entity.AddComponent(new PlayerComponent());
                    entity.AddComponent(new CharacterControllerComponent());

                    // Attach a camera to follow the player
                    var camera = new CameraComponent
                    {
                        IsMainCamera = true,
                        Offset = new Vector3(0, 8, 16),
                        LookAtOffset = new Vector3(0, 2, 0)
                    };
                    entity.AddComponent(camera);
                }
            }

            return scene;
        }
    }
}
