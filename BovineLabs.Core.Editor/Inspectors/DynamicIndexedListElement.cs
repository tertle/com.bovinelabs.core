// <copyright file="DynamicIndexedListElement.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Iterators;
    using JetBrains.Annotations;
    using Unity.Collections;

    public class DynamicIndexedListElement<TBuffer, TKey, TIndex, TValue> : DynamicListElement<TBuffer, DynamicIndexedListElement<TBuffer, TKey, TIndex, TValue>.KVP>
        where TBuffer : unmanaged, IDynamicIndexedMap<TKey, TIndex, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex : unmanaged, IEquatable<TIndex>
        where TValue : unmanaged
    {
        public DynamicIndexedListElement(object inspector, int refreshRate = 250)
            : base(inspector, refreshRate)
        {
        }

        private DynamicIndexedMap<TKey, TIndex, TValue> GetMap => this.Context.EntityManager.GetBuffer<TBuffer>(this.Context.Entity).AsIndexedMap<TBuffer, TKey, TIndex, TValue>();

        public override bool IsValid()
        {
            return base.IsValid() && this.Context.EntityManager.HasBuffer<TBuffer>(this.Context.Entity);
        }

        protected override void PopulateList(List<KVP> list)
        {
            var map = this.GetMap;

            using var e = map.GetEnumerator();
            while (e.MoveNext())
            {
                list.Add(new KVP(e.Current));
            }
        }

        protected override void OnValueChanged(NativeArray<KVP> newValues)
        {
            // TODO
        }

        public struct KVP
        {
            [UsedImplicitly]
            public TKey Key;

            [UsedImplicitly]
            public TIndex Index;

            [UsedImplicitly]
            public TValue Value;

            public KVP(KIV<TKey, TIndex, TValue> kvp)
            {
                this.Key = kvp.Key;
                this.Index = kvp.Indexed;
                this.Value = kvp.Value;
            }
        }
    }
}
