// <copyright file="DefaultWorldInitializationInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using System;
    using Unity.Entities;

    public static class DefaultWorldInitializationInternal
    {
        /// <summary>
        /// Invoked after the Worlds are destroyed.
        /// </summary>
        public static event Action DefaultWorldDestroyed
        {
            add => DefaultWorldInitialization.DefaultWorldDestroyed += value;
            remove => DefaultWorldInitialization.DefaultWorldDestroyed -= value;
        }

        public static void DomainUnloadOrPlayModeChangeShutdown()
        {
            DefaultWorldInitialization.DomainUnloadOrPlayModeChangeShutdown();
        }
    }
}
