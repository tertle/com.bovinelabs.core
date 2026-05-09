// <copyright file="DynamicGeneratorTestBuffers.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators.Columns
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.Iterators.Columns;
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    internal struct MultiHashColumnTestsBuffer : IDynamicVariableMap<int, float, short, MultiHashColumn<short>>
    {
        byte IDynamicVariableMap<int, float, short, MultiHashColumn<short>>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct OrderedListColumnTestsBuffer : IDynamicVariableMap<int, float, int, OrderedListColumn<int>>
    {
        byte IDynamicVariableMap<int, float, int, OrderedListColumn<int>>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct OrderedListColumnTestsSmallBuffer : IDynamicVariableMap<long, short, short, OrderedListColumn<short>>
    {
        byte IDynamicVariableMap<long, short, short, OrderedListColumn<short>>.Value { get; }
    }
}
