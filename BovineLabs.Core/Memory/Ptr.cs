namespace BovineLabs.Core.Memory
{
    using System;

    public readonly unsafe struct Ptr : IEquatable<Ptr>
    {
        public readonly void* Value;

        public static readonly Ptr Zero;

        public Ptr(void* value)
        {
            Value = value;
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
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return unchecked((int)(long)Value);
        }
    }
}
