
using System;
using System.Collections.Generic;

namespace GE2D3D.MapEditor.Runtime
{
    /// <summary>
    /// Simple component-based entity used by the runtime engine.
    /// </summary>
    public class RuntimeEntity
    {
        public int Id { get; }
        public string Name { get; }

        private readonly Dictionary<Type, IRuntimeComponent> _components = new();

        public RuntimeEntity(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public void AddComponent(IRuntimeComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            _components[component.GetType()] = component;
        }

        public bool TryGetComponent<T>(out T component) where T : class, IRuntimeComponent
        {
            if (_components.TryGetValue(typeof(T), out var value))
            {
                component = (T)value;
                return true;
            }

            component = null!;
            return false;
        }

        public T? GetComponent<T>() where T : class, IRuntimeComponent
        {
            return TryGetComponent<T>(out T component) ? component : null;
        }

        public bool HasComponent<T>() where T : class, IRuntimeComponent
        {
            return _components.ContainsKey(typeof(T));
        }
    }

    /// <summary>
    /// Marker interface for all runtime components.
    /// </summary>
    public interface IRuntimeComponent
    {
    }
}
