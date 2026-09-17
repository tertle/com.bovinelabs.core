namespace BovineLabs.Core.Keys
{
    using Unity.Collections;

    public struct FixedNameValue<T>
    {
        public FixedString32Bytes Name;
        public T Value;
    }
}
