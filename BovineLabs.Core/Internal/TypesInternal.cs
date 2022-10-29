// <copyright file="TypesInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>


namespace BovineLabs.Core.Internal
{
    using System;
    using Unity.Scenes;

    public static class TypesInternal
    {
#if !UNITY_DISABLE_MANAGED_COMPONENTS
        public static readonly Type CompanionLink = typeof(Unity.Entities.CompanionLink);
#endif

        public static readonly Type PublicEntityRefType = typeof(PublicEntityRef);
    }
}
