using System;

namespace GE2D3D.MapEditor.Runtime.Scripts
{
    /// <summary>
    /// Base interface implemented by all runtime scripts.
    /// Scripts are plain C# classes that live in any loaded assembly.
    /// </summary>
    public interface IGameScript
    {
        void OnStart(RuntimeEntity entity, RuntimeScene scene);
        void OnUpdate(RuntimeEntity entity, RuntimeScene scene, float dt);
        void OnTriggerEnter(RuntimeEntity entity, RuntimeEntity other, RuntimeScene scene);
        void OnTriggerExit(RuntimeEntity entity, RuntimeEntity other, RuntimeScene scene);
        void OnInteract(RuntimeEntity entity, RuntimeScene scene);
    }

    /// <summary>
    /// Script base with empty virtual implementations for convenience.
    /// </summary>
    public abstract class GameScriptBase : IGameScript
    {
        public virtual void OnStart(RuntimeEntity entity, RuntimeScene scene) { }
        public virtual void OnUpdate(RuntimeEntity entity, RuntimeScene scene, float dt) { }
        public virtual void OnTriggerEnter(RuntimeEntity entity, RuntimeEntity other, RuntimeScene scene) { }
        public virtual void OnTriggerExit(RuntimeEntity entity, RuntimeEntity other, RuntimeScene scene) { }
        public virtual void OnInteract(RuntimeEntity entity, RuntimeScene scene) { }
    }
}
