namespace BovineLabs.Core.Utility
{
    using System;
    using Unity.Collections.LowLevel.Unsafe;

    public readonly unsafe struct Ptr<T> : IEquatable<Ptr<T>>
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        public readonly T* Value;

        public Ptr(T* value)
        {
            this.Value = value;
        }

        public Ptr(ref T value)
        {
            this.Value = (T*)UnsafeUtility.AddressOf(ref value);
        }

        public bool IsCreated => this.Value != null;

        public ref T Ref => ref UnsafeUtility.AsRef<T>(this.Value);

        public static implicit operator T*(Ptr<T> node)
        {
            return node.Value;
        }

        public static implicit operator Ptr<T>(T* ptr)
        {
            return new Ptr<T>(ptr);
        }

        public static bool operator ==(Ptr<T> left, Ptr<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Ptr<T> left, Ptr<T> right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Ptr<T> other)
        {
            return this.Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is Ptr<T> other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)(long)this.Value);
        }
    }
}
