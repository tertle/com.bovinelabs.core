// <copyright file="K.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System.Collections.Generic;
    using Unity.Burst;
    using Unity.Collections;
    using UnityEngine;

    /// <summary>
    /// K is an Enum or LayerMask alternative that allows your key value pairs to be defined in setting files.
    /// It provides a way to convert human readable strings into values, even within burst jobs.
    /// </summary>
    /// <typeparam name="T"> The type of config. </typeparam>
    public static class K<T>
    {
        private static readonly SharedStatic<KMap> Map = SharedStatic<KMap>.GetOrCreate<KMap, T>();

        /// <summary> Given a name, returns the user defined value. </summary>
        /// <param name="name"> The name. </param>
        /// <returns> The value. </returns>
        public static int NameToKey(FixedString32Bytes name)
        {
            if (!TryNameToKey(name, out var key))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                Debug.LogError($"{name} does not exist");
#endif
            }

            return key;
        }

        public static bool TryNameToKey(FixedString32Bytes name, out int key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (Map.Data.Capacity == 0)
            {
                Debug.LogError("Trying to read from an uninitialized K");
                key = default;
                return false;
            }
#endif

            return Map.Data.TryGetValue(name, out key);
        }

        /// <summary> Given a key, returns the name that's associated with it. Mostly used for debugging. </summary>
        /// <param name="key"> The key. </param>
        /// <returns> The value. </returns>
        public static FixedString32Bytes KeyToName(int key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (Map.Data.Capacity == 0)
            {
                Debug.LogError("Trying to read from an uninitialized K");
                return default;
            }
#endif

            if (!Map.Data.TryGetValue(key, out var name))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                Debug.LogError($"{key} does not exist");
#endif
            }

            return name;
        }

        /// <summary> Initialize this generic with a set of values. </summary>
        /// <param name="kvp"> The key values to assign. </param>
        public static void Initialize(IReadOnlyList<NameValue> kvp)
        {
            Map.Data = new KMap(kvp);
        }
    }
}
