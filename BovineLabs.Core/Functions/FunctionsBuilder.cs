// <copyright file="FunctionsBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Functions
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Utility;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary> The builder for creating <see cref="Functions{T, TO}" />. </summary>
    /// <typeparam name="T"> Is the void* data that will be passed to the ExecuteFunction. Also serves as a grouping mechanism for ReflectAll. </typeparam>
    /// <typeparam name="TO"> Is the type of result that is expected from the ExecuteFunction. </typeparam>
    public unsafe struct FunctionsBuilder<T, TO> : IDisposable
        where T : unmanaged
        where TO : unmanaged
    {
        private static List<MethodInfo> cachedReflectAll;

        private NativeHashSet<BuildData> functions;

        /// <summary> Initializes a new instance of the <see cref="FunctionsBuilder{T, TO}" /> struct. </summary>
        /// <param name="allocator"> The allocator to use for the builder. This should nearly always be <see cref="Allocator.Temp" />. </param>
        public FunctionsBuilder(Allocator allocator)
        {
            this.functions = new NativeHashSet<BuildData>(0, allocator);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.functions.Dispose();
        }

        /// <summary> Find all implementations of <see cref="IFunction{T}" />. </summary>
        /// <param name="state"> The system state passed to OnCreate. </param>
        /// <returns> Itself. </returns>
        public FunctionsBuilder<T, TO> ReflectAll(ref SystemState state)
        {
            if (cachedReflectAll == null)
            {
                cachedReflectAll = new List<MethodInfo>();

                var baseMethod = typeof(FunctionsBuilder<T, TO>).GetMethod(nameof(this.AddInternalDefault), BindingFlags.Instance | BindingFlags.NonPublic)!;

                var implementations = ReflectionUtility.GetAllImplementations<IFunction<T>>();
                foreach (var type in implementations)
                {
                    if (!UnsafeUtility.IsUnmanaged(type))
                    {
                        continue;
                    }

                    var genericMethod = baseMethod.MakeGenericMethod(type);
                    cachedReflectAll.Add(genericMethod);
                }
            }

            fixed (void* ptr = &state)
            {
                foreach (var genericMethod in cachedReflectAll)
                {
                    genericMethod.Invoke(this, new object[] { (IntPtr)ptr });
                }
            }

            return this;
        }

        /// <summary> Manually add an instance of <see cref="IFunction{T}" />. </summary>
        /// <param name="state"> The system state passed to OnCreate. </param>
        /// <param name="function"> The instance </param>
        /// <typeparam name="TF"> The type of <see cref="IFunction{T}" />. </typeparam>
        /// <returns> Itself. </returns>
        public FunctionsBuilder<T, TO> Add<TF>(ref SystemState state, TF function)
            where TF : unmanaged, IFunction<T>
        {
            return this.Add(ref state, function, BurstRuntime.GetHashCode64<TF>());
        }

        /// <summary> Manually add an instance of <see cref="IFunction{T}" />. </summary>
        /// <param name="state"> The system state passed to OnCreate. </param>
        /// <param name="function"> The instance. </param>
        /// <param name="hash"> Unique hash of the function. </param>
        /// <typeparam name="TF"> The type of <see cref="IFunction{T}" />. </typeparam>
        /// <returns> Itself. </returns>
        public FunctionsBuilder<T, TO> Add<TF>(ref SystemState state, TF function, long hash)
            where TF : unmanaged, IFunction<T>
        {
            var buildData = new BuildData { Hash = hash };

            if (this.functions.Contains(buildData))
            {
                BLGlobalLogger.LogError($"Trying to add function with hash {hash} multiple times");
                return this;
            }

            var pinned = (TF*)UnsafeUtility.MallocTracked(UnsafeUtility.SizeOf<TF>(), UnsafeUtility.AlignOf<TF>(), Allocator.Persistent, 0);
            *pinned = function;
            pinned->OnCreate(ref state);

            var executeFunction = BurstCompiler.CompileFunctionPointer(pinned->ExecuteFunction);
            var updateFunction = default(FunctionPointer<UpdateFunction>);
            var destroyFunction = IntPtr.Zero;

            if (pinned->UpdateFunction != null)
            {
                updateFunction = BurstCompiler.CompileFunctionPointer(pinned->UpdateFunction!);
            }

            if (pinned->DestroyFunction != null)
            {
                destroyFunction = Marshal.GetFunctionPointerForDelegate(pinned->DestroyFunction);
            }

            buildData.FunctionData = new FunctionData
            {
                Target = pinned,
                DestroyFunction = destroyFunction,
                ExecuteFunction = executeFunction,
                UpdateFunction = updateFunction,
            };

            var result = this.functions.Add(buildData);
            Assert.IsTrue(result);

            return this;
        }

        /// <summary> Manually create an instance of <see cref="IFunction{T}" />. </summary>
        /// <param name="state"> The system state passed to OnCreate. </param>
        /// <typeparam name="TF"> The type of <see cref="IFunction{T}" /> to create. </typeparam>
        /// <returns> Itself. </returns>
        public FunctionsBuilder<T, TO> Add<TF>(ref SystemState state)
            where TF : unmanaged, IFunction<T>
        {
            fixed (SystemState* ptr = &state)
            {
                return this.AddInternalDefault<TF>(ptr);
            }
        }

        /// <summary> Builds the <see cref="Functions{T, TO}" /> to use with all the found <see cref="IFunction{T}" />. </summary>
        /// <returns> A new instance of <see cref="Functions{T, TO}" />. </returns>
        public Functions<T, TO> Build()
        {
            var array = new NativeArray<FunctionData>(this.functions.Count, Allocator.Persistent);

            using var e = this.functions.GetEnumerator();
            var index = 0;

            while (e.MoveNext())
            {
                array[index++] = e.Current.FunctionData;
            }

            return new Functions<T, TO>(array);
        }

        /// <summary> Builds the <see cref="Functions{T, TO}" /> to use with all the found <see cref="IFunction{T}" />. </summary>
        /// <returns> A new instance of <see cref="Functions{T, TO}" />. </returns>
        public FunctionsHash<T, TO> BuildHash()
        {
            var hash = new NativeHashMap<long, FunctionData>(this.functions.Count, Allocator.Persistent);
            using var e = this.functions.GetEnumerator();

            while (e.MoveNext())
            {
                hash[e.Current.Hash] = e.Current.FunctionData;
            }

            return new FunctionsHash<T, TO>(hash);
        }

        private FunctionsBuilder<T, TO> AddInternalDefault<TF>(SystemState* state)
            where TF : unmanaged, IFunction<T>
        {
            return this.Add<TF>(ref *state, default);
        }

        private struct BuildData : IEquatable<BuildData>
        {
            public long Hash;
            public FunctionData FunctionData;

            public bool Equals(BuildData other)
            {
                return this.Hash == other.Hash;
            }

            public override int GetHashCode()
            {
                return this.Hash.GetHashCode();
            }
        }
    }
}
