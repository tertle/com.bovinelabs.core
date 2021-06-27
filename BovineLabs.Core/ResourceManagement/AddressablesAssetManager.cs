// <copyright file="AddressablesAssetManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using JetBrains.Annotations;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.Assertions;
    using UnityEngine.Profiling;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceProviders;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    /// <summary> Loads assets using addressables. </summary>
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class AddressablesAssetManager : IAssetManager
    {
        private const int UpdateDelayInMs = 20;

        private bool isQuitting;

        /// <summary> Initializes a new instance of the <see cref="AddressablesAssetManager"/> class. </summary>
        public AddressablesAssetManager()
        {
            Application.quitting += () => this.isQuitting = true;
        }

        /// <inheritdoc />
        public Task<T> LoadAssetAsync<T>(AssetRef key, IProgress<float> progress = null)
            where T : Object
        {
            var asyncOperation = Addressables.LoadAssetAsync<T>(key);
            return GetTask(asyncOperation, progress);
        }

        /// <inheritdoc />
        public Task<IList<T>> LoadAssetsAsync<T>(IEnumerable<AssetRef> keys, IProgress<float> progress = null)
            where T : Object
        {
            var asyncOperation = Addressables.LoadAssetsAsync<T>(keys, null, Addressables.MergeMode.Union);
            return GetTask(asyncOperation, progress);
        }

        /// <inheritdoc />
        public Task<IList<T>> LoadAssetsAsync<T>(AssetRef key, IProgress<float> progress = null)
            where T : Object
        {
            var asyncOperation = Addressables.LoadAssetsAsync<T>(key, null);
            return GetTask(asyncOperation, progress);
        }

        /// <inheritdoc />
        public void UnloadAsset<T>(T asset)
            where T : Object
        {
            if (this.isQuitting)
            {
                return;
            }

            Addressables.Release(asset);
        }

        /// <inheritdoc />
        public Task<GameObject> InstantiateAsync(AssetRef key, IProgress<float> progress = null)
        {
            var asyncOperation = Addressables.InstantiateAsync(key);
            return GetTask(asyncOperation, progress);
        }

        /// <inheritdoc />
        public void Release(GameObject instance)
        {
            if (this.isQuitting)
            {
                return;
            }

            Addressables.ReleaseInstance(instance);
        }

        /// <inheritdoc />
        public Task<ISceneInstance> LoadSceneAsync(AssetRef key, LoadSceneMode loadSceneMode = LoadSceneMode.Single, IProgress<float> progress = null)
        {
            var asyncOperation = Addressables.LoadSceneAsync(key, loadSceneMode);

            return Task.Factory.StartNew<ISceneInstance>(() =>
            {
                if (progress != null)
                {
                    while (!asyncOperation.IsDone)
                    {
                        Task.Delay(UpdateDelayInMs).Wait();
                        progress.Report(asyncOperation.PercentComplete);
                    }
                }
                else
                {
                    asyncOperation.Task.Wait();
                }

                return new SceneInstanceWrapper(asyncOperation.Result);
            });
        }

        /// <inheritdoc />
        public void UnloadScene(ISceneInstance instance)
        {
            if (this.isQuitting)
            {
                return;
            }

            var wrapper = instance as SceneInstanceWrapper;
            Assert.IsNotNull(wrapper);
            Addressables.UnloadSceneAsync(wrapper.SceneInstance);
        }

        private static Task<T> GetTask<T>(AsyncOperationHandle<T> asyncOperation, IProgress<float> progress)
        {
            if (progress == null)
            {
                return asyncOperation.Task;
            }

            return Task.Factory.StartNew(() =>
            {
                Profiler.BeginThreadProfiling("AddressablesAssetManager", "");
                while (!asyncOperation.IsDone)
                {
                    Task.Delay(UpdateDelayInMs).Wait();
                    progress.Report(asyncOperation.PercentComplete);
                }

                return asyncOperation.Result;
            });
        }

        private class SceneInstanceWrapper : ISceneInstance
        {
            public SceneInstanceWrapper(SceneInstance sceneInstance)
            {
                this.SceneInstance = sceneInstance;
            }

            public SceneInstance SceneInstance { get; }
        }
    }
}