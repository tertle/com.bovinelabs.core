namespace BovineLabs.Core.Internal
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    public static class BlobAssetReferenceInternal
    {
        public static unsafe BlobAssetReference<T> Create<T>(void* headerPtr, int headerLength, void* dataPtr, int dataLength)
            where T : struct
        {
            byte* buffer = (byte*)Memory.Unmanaged.Allocate(sizeof(BlobAssetHeader) + headerLength + dataLength, 16, Allocator.Persistent);
            UnsafeUtility.MemCpy(buffer + sizeof(BlobAssetHeader), headerPtr, headerLength);
            UnsafeUtility.MemCpy(buffer + sizeof(BlobAssetHeader) + headerLength, dataPtr, dataLength);

            BlobAssetHeader* header = (BlobAssetHeader*)buffer;
            *header = new BlobAssetHeader();
            header->Length = headerLength + dataLength;
            header->Allocator = Allocator.Persistent;

            // @TODO use 64bit hash
            var hash1 = math.hash(headerPtr, headerLength);
            var hash2 = math.hash(dataPtr, dataLength);
            header->Hash = ((ulong)hash1 << 32) | hash2;

            BlobAssetReference<T> blobAssetReference;
            blobAssetReference.m_data.m_Align8Union = 0;
            header->ValidationPtr = blobAssetReference.m_data.m_Ptr = buffer + sizeof(BlobAssetHeader);
            return blobAssetReference;
        }
    }
}
