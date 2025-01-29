// <copyright file="UnsafeEnableableLookup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct UnsafeEnableableLookup
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* access;

        internal UnsafeEnableableLookup(EntityDataAccess* access)
        {
            this.access = access;
        }

        /// <summary>
        /// Reports whether the specified <see cref="Entity" /> instance still refers to a valid entity and that it has a
        /// component of <see cref="componentType" />.
        /// </summary>
        /// <param name="entity"> The entity. </param>
        /// <param name="componentType"> The the component type to check. </param>
        /// <returns>
        /// True if the entity has a component of type T, and false if it does not. Also returns false if
        /// the Entity instance refers to an entity that has been destroyed.
        /// </returns>
        public bool HasComponent(Entity entity, ComponentType componentType)
        {
            return this.access->HasComponent(entity, componentType);
        }

        /// <summary>
        /// Checks whether the <see cref="IComponentData" /> of <see cref="componentType" /> is enabled on the specified <see cref="Entity" />.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The <see cref="componentType" /> must implement the <see cref="IEnableableComponent" /> interface.
        /// </summary>
        /// <param name="entity"> The entity whose component should be checked. </param>
        /// <param name="componentType"> The the component type to enabled. </param>
        /// <returns> True if the specified component is enabled, or false if it is disabled. </returns>
        /// <seealso cref="SetComponentEnabled" />
        public bool IsComponentEnabled(Entity entity, ComponentType componentType)
        {
            return this.access->IsComponentEnabled(entity, componentType.TypeIndex);
        }

        /// <summary>
        /// Enable or disable the <see cref="IComponentData" /> of <see cref="componentType" /> on the specified <see cref="Entity" />. This operation
        /// does not cause a structural change (even if it occurs on a worker thread), or affect the value of the component.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The <see cref="componentType" /> must implement the <see cref="IEnableableComponent" /> interface.
        /// </summary>
        /// <param name="entity"> The entity whose component should be enabled or disabled. </param>
        /// <param name="componentType"> The the component type to enabled. </param>
        /// <param name="value"> True if the specified component should be enabled, or false if it should be disabled. </param>
        /// <seealso cref="IsComponentEnabled" />
        public void SetComponentEnabled(Entity entity, ComponentType componentType, bool value)
        {
            this.access->SetComponentEnabled(entity, componentType.TypeIndex, value);
        }
    }
}
