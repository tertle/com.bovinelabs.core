// <copyright file="Entropy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_ENTROPY
namespace BovineLabs.Core.Entropy
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    public struct Entropy : IComponentData
    {
        public ThreadRandom Random;
    }
}
#endif
