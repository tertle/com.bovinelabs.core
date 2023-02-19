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
        /// Checks whether the <see cref="IComponentData" /> of type T is enabled on the specified <see cref="Entity" />.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent" /> interface.
        /// </summary>
        /// <param name="entity"> The entity whose component should be checked. </param>
        /// <returns> True if the specified component is enabled, or false if it is disabled. </returns>
        /// <seealso cref="SetComponentEnabled" />
        public bool IsComponentEnabled(Entity entity, ComponentType componentType)
        {
            return this.access->IsComponentEnabled(entity, componentType.TypeIndex);
        }

        /// <summary>
        /// Enable or disable the <see cref="IComponentData" /> of type T on the specified <see cref="Entity" />. This operation
        /// does not cause a structural change (even if it occurs on a worker thread), or affect the value of the component.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent" /> interface.
        /// </summary>
        /// <exception cref="ArgumentException"> The <see cref="Entity" /> does not exist. </exception>
        /// <param name="entity"> The entity whose component should be enabled or disabled. </param>
        /// <param name="value"> True if the specified component should be enabled, or false if it should be disabled. </param>
        /// <seealso cref="IsComponentEnabled" />
        public void SetComponentEnabled(Entity entity, ComponentType componentType, bool value)
        {
            this.access->SetComponentEnabled(entity, componentType.TypeIndex, value);
        }
    }
}
