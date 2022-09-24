// <copyright file="EntityManagerInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Collections;
    using Unity.Entities;

    public static class EntityManagerInternal
    {
        [NotBurstCompatible]
        public static unsafe object GetSharedComponentDataNonDefaultBoxed(this EntityManager entityManager, int sharedComponentIndex)
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            var mcs = access->ManagedComponentStore;
            return mcs.GetSharedComponentDataNonDefaultBoxed(sharedComponentIndex);
        }
    }
}
