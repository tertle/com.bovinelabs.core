// <copyright file="EnableMaskCreator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using Unity.Entities;

    public static unsafe class EnableMaskCreator
    {
        public static EnabledMask Create(SafeBitRef enableBitRef, int* ptrChunkDisabledCount)
        {
            return new EnabledMask(enableBitRef, ptrChunkDisabledCount);
        }
    }
}
