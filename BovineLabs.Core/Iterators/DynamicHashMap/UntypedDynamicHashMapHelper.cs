namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UntypedDynamicHashMapHelper
    {
        internal int ValuesOffset;
        internal int KeysOffset;
        internal int NextOffset;
        internal int BucketsOffset;
        internal int Count;
        internal int Capacity;
        internal int BucketCapacityMask; // = bucket capacity - 1
        internal int Log2MinGrowth;
        internal int AllocatedIndex;
        internal int FirstFreeIdx;
        internal int SizeOfTValue;

        internal int BucketCapacity => BucketCapacityMask + 1;

        internal byte* Values
        {
            get
            {
                fixed (UntypedDynamicHashMapHelper* data = &this)
                {
                    return (byte*)data + data->ValuesOffset;
                }
            }
        }

        internal byte* Keys
        {
            get
            {
                fixed (UntypedDynamicHashMapHelper* data = &this)
                {
                    return (byte*)data + data->KeysOffset;
                }
            }
        }

        internal int* Next
        {
            get
            {
                fixed (UntypedDynamicHashMapHelper* data = &this)
                {
                    return (int*)((byte*)data + data->NextOffset);
                }
            }
        }

        internal int* Buckets
        {
            get
            {
                fixed (UntypedDynamicHashMapHelper* data = &this)
                {
                    return (int*)((byte*)data + data->BucketsOffset);
                }
            }
        }

        internal struct Enumerator
        {
            [NativeDisableUnsafePtrRestriction]
            internal UntypedDynamicHashMapHelper* Data;
            internal int Index;
            internal int BucketIndex;
            internal int NextIndex;

            internal Enumerator(UntypedDynamicHashMapHelper* data)
            {
                Data = data;
                Index = -1;
                BucketIndex = 0;
                NextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal bool MoveNext()
            {
                var next = Data->Next;

                if (NextIndex != -1)
                {
                    Index = NextIndex;
                    NextIndex = next[NextIndex];
                    return true;
                }

                var buckets = Data->Buckets;

                for (int i = BucketIndex, num = Data->BucketCapacity; i < num; ++i)
                {
                    var idx = buckets[i];

                    if (idx != -1)
                    {
                        Index = idx;
                        BucketIndex = i + 1;
                        NextIndex = next[idx];

                        return true;
                    }
                }

                Index = -1;
                BucketIndex = Data->BucketCapacity;
                NextIndex = -1;
                return false;
            }

            internal void Reset()
            {
                Index = -1;
                BucketIndex = 0;
                NextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal (IntPtr UntypedDynamicHashMapHelper, int Index) GetCurrent()
            {
                return ((IntPtr)Data, Index);
            }
        }
    }
}
