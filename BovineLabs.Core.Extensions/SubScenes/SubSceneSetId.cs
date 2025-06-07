// <copyright file="SubSceneSetId.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using System;

    public readonly struct SubSceneSetId : IEquatable<SubSceneSetId>
    {
        public readonly int Value;

        public SubSceneSetId(int value)
        {
            this.Value = value;
        }

        public bool Equals(SubSceneSetId other)
        {
            return this.Value == other.Value;
        }

        public override int GetHashCode()
        {
            return this.Value;
        }
    }
}
#endif