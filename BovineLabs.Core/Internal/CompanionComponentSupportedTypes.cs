// <copyright file="CompanionComponentSupportedTypes.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public static class CompanionComponentSupportedTypes
    {
        public static ComponentType[] Types
        {
            get => Unity.Entities.Conversion.CompanionComponentSupportedTypes.Types;
            set => Unity.Entities.Conversion.CompanionComponentSupportedTypes.Types = value;
        }
    }
}
