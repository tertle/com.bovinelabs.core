// <copyright file="DefaultWorldInitializationInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public static class DefaultWorldInitializationInternal
    {
        public static void DomainUnloadOrPlayModeChangeShutdown()
        {
            DefaultWorldInitialization.DomainUnloadOrPlayModeChangeShutdown();
        }
    }
}
