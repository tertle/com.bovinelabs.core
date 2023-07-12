// <copyright file="Entropy.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#if !BL_DISABLE_ENTROPY
namespace BovineLabs.Core.Entropy
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    public struct Entropy : IComponentData
    {
        public NativeThreadRandom Random;
    }
}
#endif
