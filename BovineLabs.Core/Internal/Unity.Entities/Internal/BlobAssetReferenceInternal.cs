namespace BovineLabs.Core.Internal
{
    using BovineLabs.Core.Collections;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    public static class BlobAssetReferenceInternal
    {
        public static unsafe int GetLength<T>(this BlobAssetReference<T> reference)
            where T : unmanaged
        {
            return reference.m_data.Header->Length;
        }

        // The returned reference aliases the existing allocation; ownership does not change.
        public static unsafe BlobAssetReference<T> FromPointer<T>(void* pointer)
            where T : unmanaged
        {
            return new BlobAssetReference<T> { m_data = new BlobAssetReferenceData { m_Ptr = (byte*)pointer } };
        }

        public static unsafe byte* GetDataPointer(void* reference)
        {
            return ((BlobAssetReferenceData*)reference)->m_Ptr;
        }

        // headerOffset is a relative pointer stored in the containing blob, not a copied offset value.
        public static unsafe BlobAssetReference<T> FromHeaderOffset<T>(ref int headerOffset)
            where T : unmanaged
        {
            ref var offset = ref UnsafeUtility.As<int, BlobPtr<BlobAssetHeader>>(ref headerOffset);
            var header = (BlobAssetHeader*)offset.GetUnsafePtr();
            var pointer = (byte*)(header + 1);
            header->ValidationPtr = pointer;
            return FromPointer<T>(pointer);
        }

        // Allocate with the engine header's alignment and copy both the header and payload.
        public static unsafe void CopyTo<T>(this BlobAssetReference<T> reference, ref BlobBuilder builder, ref int headerOffset)
            where T : unmanaged
        {
            ref var header = ref UnsafeUtility.As<int, BlobPtr<BlobAssetHeader>>(ref headerOffset);
            var size = sizeof(BlobAssetHeader) + reference.m_data.Header->Length;
            var destination = builder.Allocate(ref header, size);
            UnsafeUtility.MemCpy(destination, reference.m_data.Header, size);
        }

        public static unsafe BlobAssetReference<T> Create<T>(void* headerPtr, int headerLength, void* dataPtr, int dataLength)
            where T : unmanaged
        {
            var buffer = (byte*)CollectionMemory.Allocate(sizeof(BlobAssetHeader) + headerLength + dataLength, 16, Allocator.Persistent);
            UnsafeUtility.MemCpy(buffer + sizeof(BlobAssetHeader), headerPtr, headerLength);
            UnsafeUtility.MemCpy(buffer + sizeof(BlobAssetHeader) + headerLength, dataPtr, dataLength);

            var header = (BlobAssetHeader*)buffer;
            *header = default;
            header->Length = headerLength + dataLength;
            header->Allocator = Allocator.Persistent;

            var hash1 = math.hash(headerPtr, headerLength);
            var hash2 = math.hash(dataPtr, dataLength);
            header->Hash = ((ulong)hash1 << 32) | hash2;

            BlobAssetReference<T> blobAssetReference;
            blobAssetReference.m_data.m_Align8Union = 0;
            header->ValidationPtr = blobAssetReference.m_data.m_Ptr = buffer + sizeof(BlobAssetHeader);

            return blobAssetReference;
        }

        public static long GetHash<T>(this BlobAssetReference<T> blobAssetReference)
            where T : unmanaged
        {
            return blobAssetReference.m_data.m_Align8Union;
        }

        public static unsafe void GetPayloadRange<T>(this BlobAssetReference<T> blobAssetReference, out byte* start, out int length)
            where T : unmanaged
        {
            if (!blobAssetReference.IsCreated)
            {
                throw new System.InvalidOperationException("The blob asset reference is not created.");
            }

            start = (byte*)blobAssetReference.GetUnsafePtr();
            length = ((BlobAssetHeader*)start - 1)->Length;
        }
    }
}
