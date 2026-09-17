namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    /// <summary>
    /// Supports parallel readers and parallel writers.
    /// </summary>
    [NativeContainer]
    public partial struct NativeThreadStream : IDisposable, IEquatable<NativeThreadStream>
    {
        private static readonly unsafe int MaxLargeSize = UnsafeThreadStreamBlockData.AllocationSize - sizeof(void*);

        public static int ForEachCount => UnsafeThreadStream.ForEachCount;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required by safety injection.")]
        private AtomicSafetyHandle m_Safety;

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required by safety injection.")]
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeThreadStream>();
#endif

        private UnsafeThreadStream stream;

        public NativeThreadStream(AllocatorManager.AllocatorHandle allocator)
        {
            Allocate(out this, allocator);
            this.stream.AllocateForEach();
        }

        public bool IsCreated => this.stream.IsCreated;

        public bool IsEmpty()
        {
            return this.stream.IsEmpty();
        }

        public Reader AsReader()
        {
            return new Reader(ref this);
        }

        public Writer AsWriter()
        {
            return new Writer(ref this);
        }

        public Writer<T> AsWriter<T>()
            where T : unmanaged
        {
            return new Writer<T>(ref this);
        }

        public int Count()
        {
            this.CheckReadAccess();
            return this.stream.Count();
        }

        public NativeArray<T> ToNativeArray<T>(Allocator allocator)
            where T : unmanaged
        {
            this.CheckReadAccess();
            return this.stream.ToNativeArray<T>(allocator);
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(this.m_Safety);
#endif
            this.stream.Dispose();
        }

        public JobHandle Dispose(JobHandle dependency)
        {
            var jobHandle = this.stream.Dispose(dependency);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(this.m_Safety);
#endif
            return jobHandle;
        }

        public bool Equals(NativeThreadStream other)
        {
            return this.stream.Equals(other.stream);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "Only changes in dispose.")]
        public override int GetHashCode()
        {
            return this.stream.GetHashCode();
        }

        private static void Allocate(out NativeThreadStream stream, AllocatorManager.AllocatorHandle allocator)
        {
            CollectionChecks.CheckAllocator(allocator);

            UnsafeThreadStream.AllocateBlock(out stream.stream, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            stream.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
            CollectionHelper.SetStaticSafetyId(ref stream.m_Safety, ref s_staticSafetyId.Data, "BovineLabs.Core.Collections.NativeThreadStream");
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local", Justification = "Point of method")]
        private static void ValidateAllocator(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (allocator <= Allocator.None)
            {
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReadAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
        }
    }
}
