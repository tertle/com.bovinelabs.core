// <copyright file="EntityInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using Unity.Entities.Editor;
    using Unity.Entities.UI;
    using UnityEngine.UIElements;

    public abstract class EntityInspector<T> : VisualElement
    {
        protected EntityInspector(object inspector)
        {
            if (inspector is not InspectorBase<T> propertyInspector)
            {
                throw new ArgumentException($"Inspector is not {nameof(InspectorBase<T>)}", nameof(inspector));
            }

            this.Context = propertyInspector.GetContext<EntityInspectorContext>();
        }

        internal EntityInspectorContext Context { get; }

        public virtual bool IsValid()
        {
            return this.Context.World.IsCreated && this.Context.EntityManager.SafeExists(this.Context.Entity);
        }
    }
}
