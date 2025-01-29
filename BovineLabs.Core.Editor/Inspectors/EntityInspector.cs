// <copyright file="EntityInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using Unity.Entities.Editor;
    using UnityEngine.UIElements;

    public abstract class EntityInspector<T> : VisualElement
    {
        protected EntityInspector(object inspector)
        {
            this.Context = ContextGetter.Create<T>(inspector);
        }

        internal IContextGetter Context { get; }

        public virtual bool IsValid()
        {
            return this.Context.World.IsCreated && this.Context.EntityManager.SafeExists(this.Context.Entity);
        }
    }
}
