namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Iterators;
    using JetBrains.Annotations;
    using Unity.Collections;

    public class DynamicHashSetListElement<TBuffer, T> : DynamicListElement<TBuffer, DynamicHashSetListElement<TBuffer, T>.KVP>
        where TBuffer : unmanaged, IDynamicHashSet<T>
        where T : unmanaged, IEquatable<T>
    {
        public DynamicHashSetListElement(object inspector, int refreshRate = 250)
            : base(inspector, refreshRate)
        {
        }

        private DynamicHashSet<T> GetSet => Context.EntityManager.GetBuffer<TBuffer>(Context.Entity, true).AsHashSet<TBuffer, T>();

        public override bool IsValid()
        {
            return base.IsValid() && Context.EntityManager.HasBuffer<TBuffer>(Context.Entity);
        }

        protected override void PopulateList(List<KVP> list)
        {
            var set = GetSet;

            using var e = set.GetEnumerator();
            while (e.MoveNext())
            {
                list.Add(new KVP { Value = e.Current });
            }
        }

        protected override void OnValueChanged(NativeArray<KVP> newValues)
        {
            var set = GetSet;
            set.Clear();

            foreach (var v in newValues)
            {
                set.Add(v.Value);
            }
        }

        public struct KVP
        {
            [UsedImplicitly]
            public T Value;
        }
    }
}
