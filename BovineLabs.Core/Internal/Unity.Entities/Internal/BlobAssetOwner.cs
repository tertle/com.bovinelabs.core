namespace BovineLabs.Core.Internal
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    // A storage token for the engine owner; retaining and releasing still use the engine's batch.
    public readonly struct BlobAssetOwner
    {
        private readonly Unity.Entities.BlobAssetOwner _value;

        internal BlobAssetOwner(Unity.Entities.BlobAssetOwner value)
        {
            _value = value;
        }

        public bool IsCreated => _value.IsCreated;

        // The result owns the initial batch lease; the returned reference borrows its payload.
        public static unsafe BlobAssetOwner Create<T>(BlobAssetReference<T> source, out BlobAssetReference<T> reference, out int byteCount)
            where T : unmanaged
        {
            var payloadLength = source.m_data.Header->Length;
            var alignedLength = (payloadLength + 15) & ~15;
            byteCount = sizeof(BlobAssetBatch) + sizeof(BlobAssetHeader) + alignedLength;
            var batch = (BlobAssetBatch*)UnsafeUtility.MallocTracked(
                byteCount, math.max(16, JobsUtility.CacheLineSize), Allocator.Persistent, 0);
            *batch = BlobAssetBatch.CreateForSerialize(1, byteCount);
            var header = (BlobAssetHeader*)(batch + 1);
            *header = BlobAssetHeader.CreateForSerialize(alignedLength, source.m_data.Header->Hash);
            UnsafeUtility.MemCpy(header + 1, source.GetUnsafePtr(), payloadLength);
            UnsafeUtility.MemClear((byte*)(header + 1) + payloadLength, alignedLength - payloadLength);

            var owner = new Unity.Entities.BlobAssetOwner(batch, byteCount);
            reference = BlobAssetReference<T>.Create(new BlobAssetReferenceData { m_Ptr = (byte*)(header + 1) });
            return new BlobAssetOwner(owner);
        }

        public void SetSharedComponent(EntityCommandBuffer commands, Entity entity)
        {
            commands.SetSharedComponent(entity, _value);
        }

        public void Retain()
        {
            _value.Retain();
        }

        public void Release()
        {
            _value.Release();
        }
    }
}
