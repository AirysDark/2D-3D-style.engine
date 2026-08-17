using System;
using System.Collections.Generic;
using System.Reflection;
using GE2D3D.MapEditor.Runtime.Components;
using GE2D3D.MapEditor.Runtime.Scripts;

namespace GE2D3D.MapEditor.Runtime.Systems
{
    /// <summary>
    /// Manages runtime scripts and dispatches events (OnStart, OnUpdate, triggers, interact).
    /// </summary>
    public class ScriptSystem
    {
        private readonly HashSet<ScriptComponent> _started = new();

        public void Update(RuntimeScene scene, float dt)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));

            foreach (var entity in scene.Entities)
            {
                if (!entity.TryGetComponent(out ScriptComponent scriptComp))
                    continue;

                EnsureInstance(scriptComp);

                if (scriptComp.Instance == null)
                    continue;

                if (!_started.Contains(scriptComp))
                {
                    scriptComp.Instance.OnStart(entity, scene);
                    _started.Add(scriptComp);
                }

                scriptComp.Instance.OnUpdate(entity, scene, dt);
            }
        }

        /// <summary>
        /// Called by other systems when a trigger begins overlapping.
        /// </summary>
        public void RaiseTriggerEnter(RuntimeEntity triggerEntity, RuntimeEntity other, RuntimeScene scene)
        {
            if (triggerEntity.TryGetComponent(out ScriptComponent script) && script.Instance != null)
            {
                script.Instance.OnTriggerEnter(triggerEntity, other, scene);
            }
        }

        /// <summary>
        /// Called by other systems when a trigger stops overlapping.
        /// </summary>
        public void RaiseTriggerExit(RuntimeEntity triggerEntity, RuntimeEntity other, RuntimeScene scene)
        {
            if (triggerEntity.TryGetComponent(out ScriptComponent script) && script.Instance != null)
            {
                script.Instance.OnTriggerExit(triggerEntity, other, scene);
            }
        }

        /// <summary>
        /// Called when a player activates an interact action for an entity.
        /// </summary>
        public void RaiseInteract(RuntimeEntity entity, RuntimeScene scene)
        {
            if (entity.TryGetComponent(out ScriptComponent script) && script.Instance != null)
            {
                script.Instance.OnInteract(entity, scene);
            }
        }

        private static void EnsureInstance(ScriptComponent script)
        {
            if (script.Instance != null)
                return;

            if (string.IsNullOrWhiteSpace(script.ScriptName))
                return;

            var type = Type.GetType(script.ScriptName, throwOnError: false);
            if (type == null)
                return;

            if (!typeof(IGameScript).IsAssignableFrom(type))
                return;

            script.Instance = (IGameScript?)Activator.CreateInstance(type);
        }
    }
}
