// <copyright file="Ptr.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;

    public unsafe struct Ptr<T> : IEquatable<Ptr<T>>
        where T : unmanaged
    {
        public readonly T* Value;

        /// <summary> Initializes a new instance of the <see cref="Ptr{T}" /> struct. </summary>
        /// <param name="value"> The pointer to hold. </param>
        public Ptr(T* value)
        {
            this.Value = value;
        }

        public bool IsCreated => this.Value != null;

        public static implicit operator T*(Ptr<T> node)
        {
            return node.Value;
        }

        public static implicit operator Ptr<T>(T* ptr)
        {
            return new Ptr<T>(ptr);
        }

        /// <inheritdoc />
        public bool Equals(Ptr<T> other)
        {
            return this.Value == other.Value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return unchecked((int)(long)this.Value);
        }
    }
}
