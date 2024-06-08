// <copyright file="UntypedDynamicHashMapHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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

        internal int BucketCapacity => this.BucketCapacityMask + 1;

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
                this.Data = data;
                this.Index = -1;
                this.BucketIndex = 0;
                this.NextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal bool MoveNext()
            {
                var next = this.Data->Next;

                if (this.NextIndex != -1)
                {
                    this.Index = this.NextIndex;
                    this.NextIndex = next[this.NextIndex];
                    return true;
                }

                var buckets = this.Data->Buckets;

                for (int i = this.BucketIndex, num = this.Data->BucketCapacity; i < num; ++i)
                {
                    var idx = buckets[i];

                    if (idx != -1)
                    {
                        this.Index = idx;
                        this.BucketIndex = i + 1;
                        this.NextIndex = next[idx];

                        return true;
                    }
                }

                this.Index = -1;
                this.BucketIndex = this.Data->BucketCapacity;
                this.NextIndex = -1;
                return false;
            }

            internal void Reset()
            {
                this.Index = -1;
                this.BucketIndex = 0;
                this.NextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal (IntPtr UntypedDynamicHashMapHelper, int Index) GetCurrent()
            {
                return ((IntPtr)this.Data, this.Index);
            }
        }
    }
}
