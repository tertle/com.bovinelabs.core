// <copyright file="IAssetManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    /// <summary> The asset manager interface. </summary>
    public interface IAssetManager
    {
        /// <summary> Loads a single asset. </summary>
        /// <param name="key"> The asset key. </param>
        /// <param name="progress"> Optional progress callback. </param>
        /// <typeparam name="T"> The asset type. </typeparam>
        /// <returns> The asset task. </returns>
        Task<T> LoadAssetAsync<T>(AssetRef key, IProgress<float> progress = null)
            where T : Object;

        /// <summary> Loads a collection of assets. </summary>
        /// <param name="keys"> The asset keys. </param>
        /// <param name="progress"> Optional progress callback. </param>
        /// <typeparam name="T"> The asset type. </typeparam>
        /// <returns> The asset task. </returns>
        Task<IList<T>> LoadAssetsAsync<T>(IEnumerable<AssetRef> keys, IProgress<float> progress = null)
            where T : Object;

        /// <summary> Loads a collection of assets. </summary>
        /// <param name="key"> The shared asset key. </param>
        /// <param name="progress"> Optional progress callback. </param>
        /// <typeparam name="T"> The asset type. </typeparam>
        /// <returns> The asset task. </returns>
        Task<IList<T>> LoadAssetsAsync<T>(AssetRef key, IProgress<float> progress = null)
            where T : Object;

        /// <summary> Unloads a single asset. </summary>
        /// <param name="asset"> The asset to unload. </param>
        /// <typeparam name="T"> The asset type. </typeparam>
        void UnloadAsset<T>(T asset)
            where T : Object;

        /// <summary> Instantiates a single asset. </summary>
        /// <param name="key"> The asset key. </param>
        /// <param name="progress"> Optional progress callback. </param>
        /// <returns> The asset task. </returns>
        Task<GameObject> InstantiateAsync(AssetRef key, IProgress<float> progress = null);

        /// <summary> Releases a single asset. </summary>
        /// <param name="instance"> The asset to release. </param>
        void Release(GameObject instance);

        /// <summary> Load a scene. </summary>
        /// <param name="key"> The asset key. </param>
        /// <param name="loadSceneMode"> The scene load type. </param>
        /// <param name="progress"> Optional progress callback. </param>
        /// <returns> The asset task. </returns>
        Task<ISceneInstance> LoadSceneAsync(AssetRef key, LoadSceneMode loadSceneMode = LoadSceneMode.Single, IProgress<float> progress = null);

        /// <summary> Unload a scene. </summary>
        /// <param name="instance"> The scene instance to unload. </param>
        void UnloadScene(ISceneInstance instance);
    }
}