// <copyright file="IColumn.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators.Columns
{
    using System;

    public unsafe interface IColumn<T>
        where T : unmanaged, IEquatable<T>
    {
        void Initialize(int offset, int capacity);
        int CalculateDataSize(int capacity);

        T GetValue(int idx);

        void Add(T key, int idx);
        void Replace(T newKey, int idx);
        void Remove(int idx);
        void Clear();

        void* StartResize();
        void ApplyResize(void* resizePtr);
        T GetValueOld(void* resizePtr, int idx);
    }
}
