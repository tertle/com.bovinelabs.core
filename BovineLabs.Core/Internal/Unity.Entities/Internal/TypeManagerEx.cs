namespace BovineLabs.Core.Internal
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public abstract unsafe class TypeManagerEx
    {
        public static FixedString128Bytes GetComponentDebugName(TypeIndex typeIndex)
        {
            var fs = new FixedString128Bytes();
            fs.Append(TypeManager.GetTypeInfo(typeIndex).DebugTypeName);
            return fs;
        }

        public static FixedString128Bytes GetSystemDebugName(SystemTypeIndex systemIndex)
        {
            var unsafeText = TypeManager.GetSystemNameInternal(systemIndex);

            var fs = new FixedString128Bytes();
            fs.Append(unsafeText->GetUnsafePtr(), unsafeText->Length);
            return fs;
        }
    }
}
