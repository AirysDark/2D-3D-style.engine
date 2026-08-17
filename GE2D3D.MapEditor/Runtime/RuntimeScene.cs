
using System;
using System.Collections.Generic;
using System.Linq;

namespace GE2D3D.MapEditor.Runtime
{
    /// <summary>
    /// Container for runtime entities and high-level scene metadata.
    /// </summary>
    public class RuntimeScene
    {
        private readonly List<RuntimeEntity> _entities = new();

        public IReadOnlyList<RuntimeEntity> Entities => _entities;

        public RuntimeEntity CreateEntity(int id, string name)
        {
            var entity = new RuntimeEntity(id, name);
            _entities.Add(entity);
            return entity;
        }

        public void AddEntity(RuntimeEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _entities.Add(entity);
        }

        public RuntimeEntity? FindById(int id)
        {
            return _entities.FirstOrDefault(e => e.Id == id);
        }
    }
}
