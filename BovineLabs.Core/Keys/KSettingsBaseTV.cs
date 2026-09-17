namespace BovineLabs.Core.Keys
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Scripting.LifecycleManagement;

    public abstract class KSettingsBase<T, TV> : KSettingsBase<TV>
        where T : KSettingsBase<T, TV>
        where TV : unmanaged, IEquatable<TV>
    {
        private static readonly SharedStatic<UnsafeHashMap<FixedString32Bytes, TV>> Forward =
            SharedStatic<UnsafeHashMap<FixedString32Bytes, TV>>.GetOrCreate<UnsafeHashMap<FixedString32Bytes, TV>, T>();

        private static readonly SharedStatic<UnsafeHashMap<TV, FixedString32Bytes>> Reverse =
            SharedStatic<UnsafeHashMap<TV, FixedString32Bytes>>.GetOrCreate<UnsafeHashMap<TV, FixedString32Bytes>, T>();

        private static readonly SharedStatic<UnsafeList<FixedNameValue<TV>>> Ordered =
            SharedStatic<UnsafeList<FixedNameValue<TV>>>.GetOrCreate<UnsafeList<FixedNameValue<TV>>, T>();

        [NoAutoStaticsCleanup]
        private static T settings;

        [NoAutoStaticsCleanup]
        public static T I
        {
            get => GetSingleton(ref settings);
            private set => settings = value;
        }

        public static TV NameToKey(FixedString32Bytes name)
        {
            if (!TryNameToKey(name, out var key))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                BLGlobalLogger.LogError($"{name} does not exist");
#endif
            }

            return key;
        }

        public static bool TryNameToKey(FixedString32Bytes name, out TV key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (!Forward.Data.IsCreated)
            {
                throw new Exception("K not setup");
            }
#endif

            return Forward.Data.TryGetValue(name, out key);
        }

        public static FixedString32Bytes KeyToName(TV key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (!Reverse.Data.IsCreated)
            {
                throw new Exception("K not setup");
            }
#endif

            if (!Reverse.Data.TryGetValue(key, out var name))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                BLGlobalLogger.LogError($"{key} does not exist");
#endif
            }

            return name;
        }

        [SuppressMessage("ReSharper", "NotDisposedResourceIsReturned", Justification = "No Required")]
        public static UnsafeList<FixedNameValue<TV>>.Enumerator Enumerator()
        {
            return Ordered.Data.IsCreated ? Ordered.Data.GetEnumerator() : default;
        }

        protected sealed override void Initialize()
        {
            I = (T)this;

            if (Forward.Data.IsCreated)
            {
                Forward.Data.Clear();
                Reverse.Data.Clear();
                Ordered.Data.Clear();
            }
            else
            {
                Forward.Data = new UnsafeHashMap<FixedString32Bytes, TV>(0, Allocator.Domain);
                Reverse.Data = new UnsafeHashMap<TV, FixedString32Bytes>(0, Allocator.Domain);
                Ordered.Data = new UnsafeList<FixedNameValue<TV>>(0, Allocator.Domain);
            }

            foreach (var nv in this.Keys)
            {
                Forward.Data.Add(nv.Name, nv.Value);

                // we allow multi values with same key
                Reverse.Data.TryAdd(nv.Value, nv.Name);

                Ordered.Data.Add(new FixedNameValue<TV>
                {
                    Name = nv.Name,
                    Value = nv.Value,
                });
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            I = (T)this;
        }
#endif
    }
}
