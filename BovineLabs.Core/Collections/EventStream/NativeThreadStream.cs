// <copyright file="NativeThreadStream.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    /// <summary>
    /// A thread data stream supporting parallel reading and parallel writing.
    /// Allows you to write different types or arrays into a single stream.
    /// </summary>
    [NativeContainer]
    public partial struct NativeThreadStream : IDisposable, IEquatable<NativeThreadStream>
    {
        /// <summary> Gets the number of streams the list can use. </summary>
        public static int ForEachCount => UnsafeThreadStream.ForEachCount;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required by safety injection.")]
        private AtomicSafetyHandle m_Safety;

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required by safety injection.")]
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeThreadStream>();
#endif

        private UnsafeThreadStream stream;

        /// <summary> Initializes a new instance of the <see cref="NativeThreadStream" /> struct. </summary>
        /// <param name="allocator"> The specified type of memory allocation. </param>
        public NativeThreadStream(AllocatorManager.AllocatorHandle allocator)
        {
            Allocate(out this, allocator);
            this.stream.AllocateForEach();
        }

        /// <summary> Gets a value indicating whether memory for the container is allocated. </summary>
        /// <value> True if this container object's internal storage has been allocated. </value>
        /// <remarks>
        ///     <para>
        ///     Note that the container storage is not created if you use the default constructor.
        ///     You must specify at least an allocation type to construct a usable container.
        ///     </para>
        /// </remarks>
        public bool IsCreated => this.stream.IsCreated;

        public bool IsEmpty()
        {
            return this.stream.IsEmpty();
        }

        /// <summary> Returns reader instance. </summary>
        /// <returns> The reader instance. </returns>
        public Reader AsReader()
        {
            return new Reader(ref this);
        }

        /// <summary> Returns writer instance. </summary>
        /// <returns> The writer instance. </returns>
        public Writer AsWriter()
        {
            return new Writer(ref this);
        }

        /// <summary> Returns strictly typed writer instance. </summary>
        /// <typeparam name="T"> The type allowed for writing. </typeparam>
        /// <returns> The writer instance. </returns>
        public Writer<T> AsWriter<T>()
            where T : unmanaged
        {
            return new Writer<T>(ref this);
        }

        /// <summary>
        /// The current number of items in the container.
        /// </summary>
        /// <returns> The item count. </returns>
        public int Count()
        {
            this.CheckReadAccess();
            return this.stream.Count();
        }

        /// <summary>
        /// Copies stream data into NativeArray.
        /// </summary>
        /// <typeparam name="T"> The type of value. </typeparam>
        /// <param name="allocator">
        /// A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        /// <returns> A new NativeArray, allocated with the given strategy and wrapping the stream data. </returns>
        /// <remarks> The array is a copy of stream data. </remarks>
        /// <returns> The native array. </returns>
        public NativeArray<T> ToNativeArray<T>(Allocator allocator)
            where T : struct
        {
            this.CheckReadAccess();
            return this.stream.ToNativeArray<T>(allocator);
        }

        /// <summary>
        /// Disposes of this stream and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(this.m_Safety);
#endif
            this.stream.Dispose();
        }

        /// <summary>
        /// Safely disposes of this container and deallocates its memory when the jobs that use it have completed.
        /// </summary>
        /// <remarks>
        /// You can call this function dispose of the container immediately after scheduling the job. Pass
        /// the [JobHandle](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html) returned by
        /// the [Job.Schedule](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobExtensions.Schedule.html)
        /// method using the `jobHandle` parameter so the job scheduler can dispose the container after all jobs
        /// using it have run.
        /// </remarks>
        /// <param name="dependency"> All jobs spawned will depend on this JobHandle. </param>
        /// <returns>
        /// A new job handle containing the prior handles as well as the handle for the job that deletes
        /// the container.
        /// </returns>
        public JobHandle Dispose(JobHandle dependency)
        {
            var jobHandle = this.stream.Dispose(dependency);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(this.m_Safety);
#endif
            return jobHandle;
        }

        /// <inheritdoc />
        public bool Equals(NativeThreadStream other)
        {
            return this.stream.Equals(other.stream);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "Only changes in dispose.")]
        public override int GetHashCode()
        {
            return this.stream.GetHashCode();
        }

        private static void Allocate(out NativeThreadStream stream, AllocatorManager.AllocatorHandle allocator)
        {
            UnsafeThreadStream.AllocateBlock(out stream.stream, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.CheckAllocator(allocator);
            stream.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            CollectionHelper.SetStaticSafetyId<NativeThreadStream>(ref stream.m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(stream.m_Safety, true);
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
