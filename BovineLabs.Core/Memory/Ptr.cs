// <copyright file="Ptr.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Memory
{
    using System;

    /// <summary> This is used because in 2020.3 IntPtr isn't IEquatable and in 2021.3 Equality isn't burstable. </summary>
    public readonly unsafe struct Ptr : IEquatable<Ptr>
    {
        public readonly void* Value;

        public static readonly Ptr Zero;

        public Ptr(void* value)
        {
            this.Value = value;
        }

        public static implicit operator void*(Ptr ptr)
        {
            return ptr.Value;
        }

        public static implicit operator Ptr(void* ptr)
        {
            return new Ptr(ptr);
        }

        public bool Equals(Ptr other)
        {
            return this.Value == other.Value;
        }

        public override int GetHashCode()
        {
            return unchecked((int)(long)this.Value);
        }
    }
}
