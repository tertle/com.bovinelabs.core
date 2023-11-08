// <copyright file="ChunkPadding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ENABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks
{
    using System;
    using BovineLabs.Core.Assertions;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    internal static class ChunkPadding
    {
        public static ComponentType Get(int size)
        {
            Check.Assume(size > 0, "Need to use a positive size");
            size = math.ceilpow2(size);

            return size switch
            {
                1 => ComponentType.ReadWrite<Padding1>(),
                2 => ComponentType.ReadWrite<Padding2>(),
                4 => ComponentType.ReadWrite<Padding4>(),
                8 => ComponentType.ReadWrite<Padding8>(),
                16 => ComponentType.ReadWrite<Padding16>(),
                32 => ComponentType.ReadWrite<Padding32>(),
                64 => ComponentType.ReadWrite<Padding64>(),
                128 => ComponentType.ReadWrite<Padding128>(),
                256 => ComponentType.ReadWrite<Padding256>(),
                512 => ComponentType.ReadWrite<Padding512>(),
                _ => throw new Exception("Invalid capacity"),
            };
        }

        private struct Padding1 : IComponentData
        {
            private byte value0;
        }

        private struct Padding2 : IComponentData
        {
            private byte value0;
            private byte value1;
        }

        private struct Padding4 : IComponentData
        {
            private byte value0;
            private byte value1;
            private byte value2;
            private byte value3;
        }

        private struct Padding8 : IComponentData
        {
            private byte value0;
            private byte value1;
            private byte value2;
            private byte value3;
            private byte value4;
            private byte value5;
            private byte value6;
            private byte value7;
        }

        private struct Padding16 : IComponentData
        {
            private FixedBytes16 value;
        }

        private struct Padding32 : IComponentData
        {
            private FixedBytes16 value0;
            private FixedBytes16 value1;
        }

        private struct Padding64 : IComponentData
        {
            private FixedBytes16 value0;
            private FixedBytes16 value1;
            private FixedBytes16 value2;
            private FixedBytes16 value3;
        }

        private struct Padding128 : IComponentData
        {
            private FixedBytes16 value0;
            private FixedBytes16 value1;
            private FixedBytes16 value2;
            private FixedBytes16 value3;
            private FixedBytes16 value4;
            private FixedBytes16 value5;
            private FixedBytes16 value6;
            private FixedBytes16 value7;
        }

        private struct Padding256 : IComponentData
        {
            private FixedBytes16 value0;
            private FixedBytes16 value1;
            private FixedBytes16 value2;
            private FixedBytes16 value3;
            private FixedBytes16 value4;
            private FixedBytes16 value5;
            private FixedBytes16 value6;
            private FixedBytes16 value7;
            private FixedBytes16 value8;
            private FixedBytes16 value9;
            private FixedBytes16 value10;
            private FixedBytes16 value11;
            private FixedBytes16 value12;
            private FixedBytes16 value13;
            private FixedBytes16 value14;
            private FixedBytes16 value15;
        }

        private struct Padding512 : IComponentData
        {
            private FixedBytes16 value0;
            private FixedBytes16 value1;
            private FixedBytes16 value2;
            private FixedBytes16 value3;
            private FixedBytes16 value4;
            private FixedBytes16 value5;
            private FixedBytes16 value6;
            private FixedBytes16 value7;
            private FixedBytes16 value8;
            private FixedBytes16 value9;
            private FixedBytes16 value10;
            private FixedBytes16 value11;
            private FixedBytes16 value12;
            private FixedBytes16 value13;
            private FixedBytes16 value14;
            private FixedBytes16 value15;
            private FixedBytes16 value16;
            private FixedBytes16 value17;
            private FixedBytes16 value18;
            private FixedBytes16 value19;
            private FixedBytes16 value20;
            private FixedBytes16 value21;
            private FixedBytes16 value22;
            private FixedBytes16 value23;
            private FixedBytes16 value24;
            private FixedBytes16 value25;
            private FixedBytes16 value26;
            private FixedBytes16 value27;
            private FixedBytes16 value28;
            private FixedBytes16 value29;
            private FixedBytes16 value30;
            private FixedBytes16 value31;
        }
    }
}
#endif
