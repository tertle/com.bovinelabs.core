namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Iterators;
    using JetBrains.Annotations;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public class DynamicHashMapListElement<T, TBuffer, TKey, TValue> : DynamicListElement<T, DynamicHashMapListElement<T, TBuffer, TKey, TValue>.KVP>
        where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public DynamicHashMapListElement(object inspector, int refreshRate = 250)
            : base(inspector, refreshRate)
        {
        }

        private DynamicHashMap<TKey, TValue> GetMap(bool isReadOnly = true) =>
            Context.EntityManager.GetBuffer<TBuffer>(Context.Entity, isReadOnly).AsHashMap<TBuffer, TKey, TValue>();

        public override bool IsValid()
        {
            return base.IsValid() && Context.EntityManager.HasBuffer<TBuffer>(Context.Entity);
        }

        protected override void PopulateList(List<KVP> list)
        {
            var map = GetMap();

            using var e = map.GetEnumerator();
            while (e.MoveNext())
            {
                list.Add(new KVP(e.Current));
            }
        }

        protected override void OnValueChanged(NativeArray<KVP> newValues)
        {
            var keys = newValues.Slice().SliceWithStride<TKey>();
            var values = newValues.Slice().SliceWithStride<TValue>(UnsafeUtility.SizeOf<TKey>());

            var map = GetMap(false);
            map.Clear();
            map.AddBatchUnsafe(keys, values);
        }

        public struct KVP
        {
            [UsedImplicitly]
            public TKey Key;

            [UsedImplicitly]
            public TValue Value;

            public KVP(Iterators.KVPair<TKey, TValue> kvp)
            {
                Key = kvp.Key;
                Value = kvp.Value;
            }
        }
    }
}
