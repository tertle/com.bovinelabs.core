// <copyright file="K.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
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
        [BurstCompatible]
        public static byte NameToKey(FixedString32Bytes name)
        {
            if (!Map.Data.TryGetValue(name, out var key))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Debug.LogError($"{name} does not exist");
#endif
            }

            return key;
        }

        /// <summary> Initialize this generic with a set of values. </summary>
        /// <param name="kvp"> The key values to assign. </param>
        public static void Initialize(NameValue[] kvp)
        {
            Map.Data = new KMap(kvp);
        }
    }
}
