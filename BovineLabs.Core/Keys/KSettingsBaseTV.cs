// <copyright file="KSettingsBaseTV.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// K is an Enum or LayerMask alternative that allows your key value pairs to be defined in setting files.
    /// It provides a way to convert human readable strings into values, even within burst jobs.
    /// </summary>
    /// <typeparam name="T"> The type of config. </typeparam>
    /// <typeparam name="TV"> The value to store. </typeparam>
    /// <summary> The base KSettings file for defining custom enums, layers, keys. </summary>
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

        private static T settings;

        public static T I
        {
            get => GetSingleton(ref settings);
            private set => settings = value;
        }

        /// <summary> Given a name, returns the user defined value. </summary>
        /// <param name="name"> The name. </param>
        /// <returns> The value. </returns>
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

        /// <summary> Given a key, returns the name that's associated with it. Mostly used for debugging. </summary>
        /// <param name="key"> The key. </param>
        /// <returns> The value. </returns>
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

        /// <inheritdoc />
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
