// <copyright file="AssetManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BovineLabs.Core.Utility;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    /// <summary>
    /// The AssetManager facade, actual asset loading handled by <see cref="IAssetManager" /> which is dynamically
    /// acquired.
    /// To override the default implementation, simply create a class inheriting <see cref="IAssetManager" /> anywhere in
    /// the project.
    /// </summary>
    public static class AssetManager
    {
        internal static IAssetManager Impl { get; private set; }

        /// <summary> Loads a single asset. </summary>
        /// <param name="key"> The asset key. </param>
        /// <param name="progress"> Optional progress callback. </param>
        /// <typeparam name="T"> The asset type. </typeparam>
        /// <returns> The asset task. </returns>
        public static Task<T> LoadAssetAsync<T>(AssetRef key, IProgress<float> progress = null)
            where T : Object
        {
            return Impl.LoadAssetAsync<T>(key, progress);
        }

        /// <summary> Loads a collection of assets. </summary>
        /// <param name="keys"> The asset keys. </param>
        /// <param name="progress"> Optional progress callback. </param>
        /// <typeparam name="T"> The asset type. </typeparam>
        /// <returns> The asset task. </returns>
        public static Task<IList<T>> LoadAssetsAsync<T>(IEnumerable<AssetRef> keys, IProgress<float> progress = null)
            where T : Object
        {
            return Impl.LoadAssetsAsync<T>(keys, progress);
        }

        /// <summary> Loads a collection of assets. </summary>
        /// <param name="key"> The shared asset key. </param>
        /// <param name="progress"> Optional progress callback. </param>
        /// <typeparam name="T"> The asset type. </typeparam>
        /// <returns> The asset task. </returns>
        public static Task<IList<T>> LoadAssetsAsync<T>(AssetRef key, IProgress<float> progress = null)
            where T : Object
        {
            return Impl.LoadAssetsAsync<T>(key, progress);
        }

        /// <summary> Unloads a single asset. </summary>
        /// <param name="asset"> The asset to unload. </param>
        /// <typeparam name="T"> The asset type. </typeparam>
        public static void UnloadAsset<T>(T asset)
            where T : Object
        {
            Impl.UnloadAsset(asset);
        }

        /// <summary> Instantiates a single asset. </summary>
        /// <param name="key"> The asset key. </param>
        /// ///
        /// <param name="progress"> Optional progress callback. </param>
        /// <returns> The asset task. </returns>
        public static Task<GameObject> InstantiateAsync(AssetRef key, IProgress<float> progress = null)
        {
            return Impl.InstantiateAsync(key, progress);
        }

        /// <summary> Releases a single asset. </summary>
        /// <param name="instance"> The asset to release. </param>
        public static void Release(GameObject instance)
        {
            Impl.Release(instance);
        }

        /// <summary> Load a scene. </summary>
        /// <param name="key"> The asset key. </param>
        /// <param name="loadSceneMode"> The scene load type. </param>
        /// <param name="progress"> Optional progress callback. </param>
        /// <returns> The asset task. </returns>
        public static Task<ISceneInstance> LoadSceneAsync(AssetRef key, LoadSceneMode loadSceneMode = LoadSceneMode.Single, IProgress<float> progress = null)
        {
            return Impl.LoadSceneAsync(key, loadSceneMode, progress);
        }

        /// <summary> Unload a scene. </summary>
        /// <param name="instance"> The scene instance to unload. </param>
        public static void UnloadScene(ISceneInstance instance)
        {
            Impl.UnloadScene(instance);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Impl = ReflectionUtility.GetCustomImplementation<IAssetManager, AddressablesAssetManager>();
        }
    }
}