// <copyright file="VirtualChunkAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

// Not wrapped in BL_ENABLE_LINKED_CHUNKS to allow it to be used conditionally
namespace BovineLabs.Core.Chunks
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly)]
    public class VirtualChunkAttribute : Attribute
    {
        public string? Group;

        public VirtualChunkAttribute(string group)
        {
            this.Group = group;
        }
    }
}
