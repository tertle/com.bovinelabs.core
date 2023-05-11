// <copyright file="VirtualChunkAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

// Not wrapped in !BL_DISABLE_LINKED_CHUNKS to allow it to be used conditionally
namespace BovineLabs.Core.Chunks
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly)]
    public class VirtualChunkAttribute : Attribute
    {
        public byte Group;

        public VirtualChunkAttribute(byte group)
        {
            this.Group = group;
        }
    }
}
