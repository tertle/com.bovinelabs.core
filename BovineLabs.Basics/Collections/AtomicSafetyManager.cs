// <copyright file="AtomicSafetyManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.Collections
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Basics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary> This is based off the Physics <see cref="AtomicSafetyManager"/> but adds support for NativeHashMap. </summary>
    public struct AtomicSafetyManager : IDisposable
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle temporaryHandle;
#endif

        private bool isCreated;

        public static AtomicSafetyManager Create()
        {
            var ret = default(AtomicSafetyManager);
            ret.CreateTemporaryHandle();
            ret.isCreated = true;
            return ret;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CheckCreatedAndThrow(this.isCreated);
            this.ReleaseTemporaryHandle();
            this.isCreated = false;
        }

        [Conditional(SafetyChecks.ConditionalSymbol)]
        public void MarkNativeArrayAsReadOnly<T>(ref NativeArray<T> array)
            where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, this.temporaryHandle);
#endif
        }

        [Conditional(SafetyChecks.ConditionalSymbol)]
        public void MarkNativeHashMapAsReadOnly<TKey, TValue>(ref NativeHashMap<TKey, TValue> hashMap)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            hashMap.m_Safety = this.temporaryHandle;
#endif
        }

        [Conditional(SafetyChecks.ConditionalSymbol)]
        public void BumpTemporaryHandleVersions()
        {
            // TODO: There should be a better way to invalidate older versions...
            this.ReleaseTemporaryHandle();
            this.CreateTemporaryHandle();
        }

        [Conditional(SafetyChecks.ConditionalSymbol)]
        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local", Justification = "Intended")]
        private static void CheckCreatedAndThrow(bool isCreated)
        {
            if (!isCreated)
            {
                throw new InvalidOperationException("Atomic Safety Manager already disposed");
            }
        }

        [Conditional(SafetyChecks.ConditionalSymbol)]
        private void CreateTemporaryHandle()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.temporaryHandle = AtomicSafetyHandle.Create();
            AtomicSafetyHandle.UseSecondaryVersion(ref this.temporaryHandle);
            AtomicSafetyHandle.SetAllowSecondaryVersionWriting(this.temporaryHandle, false);
#endif
        }

        [Conditional(SafetyChecks.ConditionalSymbol)]
        private void ReleaseTemporaryHandle()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckDeallocateAndThrow(this.temporaryHandle);
            AtomicSafetyHandle.Release(this.temporaryHandle);
#endif
        }
    }
}