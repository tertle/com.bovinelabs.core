// <copyright file="BurstTrampoline.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections.LowLevel.Unsafe;

    internal delegate void BurstTrampolineInvoker0(IntPtr context);

    internal delegate void BurstTrampolineInvoker1(IntPtr context, IntPtr data1Ptr);

    internal delegate void BurstTrampolineInvoker2(IntPtr context, IntPtr data1Ptr, IntPtr data2Ptr);

    internal delegate void BurstTrampolineInvoker3(IntPtr context, IntPtr data1Ptr, IntPtr data2Ptr, IntPtr data3Ptr);

    public readonly struct BurstTrampoline
    {
        private static readonly FunctionPointer<BurstTrampolineInvoker0> StaticInvoker = CreateInvokerFunctionPointer();

        private readonly FunctionPointer<BurstTrampolineInvoker0> functionPointer;
        private readonly IntPtr context;

        public BurstTrampoline(Delegate method)
        {
            this.functionPointer = StaticInvoker;
            GarbagePrevention.Objects.Add(method);
            var handle = GCHandle.Alloc(method);
            this.context = GCHandle.ToIntPtr(handle);
        }

        public delegate void Delegate();

        public bool IsCreated => this.functionPointer.IsCreated;

        public void Invoke()
        {
            this.functionPointer.Invoke(this.context);
        }

        private static FunctionPointer<BurstTrampolineInvoker0> CreateInvokerFunctionPointer()
        {
            BurstTrampolineInvoker0 invoker = Trampoline;
            GarbagePrevention.Objects.Add(invoker);
            return new FunctionPointer<BurstTrampolineInvoker0>(Marshal.GetFunctionPointerForDelegate(invoker));
        }

        [AOT.MonoPInvokeCallback(typeof(BurstTrampolineInvoker0))]
        private static void Trampoline(IntPtr context)
        {
            var handle = GCHandle.FromIntPtr(context);
            ((Delegate)handle.Target)();
        }
    }

    public unsafe readonly struct BurstTrampoline<T>
        where T : unmanaged
    {
        private static readonly FunctionPointer<BurstTrampolineInvoker1> StaticInvoker = CreateInvokerFunctionPointer();

        private readonly FunctionPointer<BurstTrampolineInvoker1> functionPointer;
        private readonly IntPtr context;

        public BurstTrampoline(Delegate method)
        {
            this.functionPointer = StaticInvoker;
            GarbagePrevention.Objects.Add(method);
            var handle = GCHandle.Alloc(method);
            this.context = GCHandle.ToIntPtr(handle);
        }

        public delegate void Delegate(in T data);

        public bool IsCreated => this.functionPointer.IsCreated;

        public void Invoke(in T data)
        {
            fixed (T* dataPtr = &data)
            {
                this.functionPointer.Invoke(this.context, (IntPtr)dataPtr);
            }
        }

        private static FunctionPointer<BurstTrampolineInvoker1> CreateInvokerFunctionPointer()
        {
            BurstTrampolineInvoker1 invoker = Trampoline;
            GarbagePrevention.Objects.Add(invoker);
            return new FunctionPointer<BurstTrampolineInvoker1>(Marshal.GetFunctionPointerForDelegate(invoker));
        }

        [AOT.MonoPInvokeCallback(typeof(BurstTrampolineInvoker1))]
        private static void Trampoline(IntPtr context, IntPtr data1Ptr)
        {
            var handle = GCHandle.FromIntPtr(context);
            ref var value = ref UnsafeUtility.AsRef<T>((void*)data1Ptr);
            ((Delegate)handle.Target)(in value);
        }
    }

    public unsafe readonly struct BurstTrampoline<T1, T2>
        where T1 : unmanaged
        where T2 : unmanaged
    {
        private static readonly FunctionPointer<BurstTrampolineInvoker2> StaticInvoker = CreateInvokerFunctionPointer();

        private readonly FunctionPointer<BurstTrampolineInvoker2> functionPointer;
        private readonly IntPtr context;

        public BurstTrampoline(Delegate method)
        {
            this.functionPointer = StaticInvoker;
            GarbagePrevention.Objects.Add(method);
            var handle = GCHandle.Alloc(method);
            this.context = GCHandle.ToIntPtr(handle);
        }

        public delegate void Delegate(in T1 data1, in T2 data2);

        public bool IsCreated => this.functionPointer.IsCreated;

        [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1519:Braces should not be omitted from multi-line child statement", Justification = "Fixed")]
        public void Invoke(in T1 data1, in T2 data2)
        {
            fixed (T1* data1Ptr = &data1)
            fixed (T2* data2Ptr = &data2)
            {
                this.functionPointer.Invoke(this.context, (IntPtr)data1Ptr, (IntPtr)data2Ptr);
            }
        }

        private static FunctionPointer<BurstTrampolineInvoker2> CreateInvokerFunctionPointer()
        {
            BurstTrampolineInvoker2 invoker = Trampoline;
            GarbagePrevention.Objects.Add(invoker);
            return new FunctionPointer<BurstTrampolineInvoker2>(Marshal.GetFunctionPointerForDelegate(invoker));
        }

        [AOT.MonoPInvokeCallback(typeof(BurstTrampolineInvoker2))]
        private static void Trampoline(IntPtr context, IntPtr data1Ptr, IntPtr data2Ptr)
        {
            var handle = GCHandle.FromIntPtr(context);
            ref var value1 = ref UnsafeUtility.AsRef<T1>((void*)data1Ptr);
            ref var value2 = ref UnsafeUtility.AsRef<T2>((void*)data2Ptr);
            ((Delegate)handle.Target)(in value1, in value2);
        }
    }

    public unsafe readonly struct BurstTrampoline<T1, T2, T3>
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
    {
        private static readonly FunctionPointer<BurstTrampolineInvoker3> StaticInvoker = CreateInvokerFunctionPointer();

        private readonly FunctionPointer<BurstTrampolineInvoker3> functionPointer;
        private readonly IntPtr context;

        public BurstTrampoline(Delegate method)
        {
            this.functionPointer = StaticInvoker;
            GarbagePrevention.Objects.Add(method);
            var handle = GCHandle.Alloc(method);
            this.context = GCHandle.ToIntPtr(handle);
        }

        public delegate void Delegate(in T1 data1, in T2 data2, in T3 data3);

        public bool IsCreated => this.functionPointer.IsCreated;

        [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1519:Braces should not be omitted from multi-line child statement", Justification = "Fixed")]
        public void Invoke(in T1 data1, in T2 data2, in T3 data3)
        {
            fixed (T1* data1Ptr = &data1)
            fixed (T2* data2Ptr = &data2)
            fixed (T3* data3Ptr = &data3)
            {
                this.functionPointer.Invoke(this.context, (IntPtr)data1Ptr, (IntPtr)data2Ptr, (IntPtr)data3Ptr);
            }
        }

        private static FunctionPointer<BurstTrampolineInvoker3> CreateInvokerFunctionPointer()
        {
            BurstTrampolineInvoker3 invoker = Trampoline;
            GarbagePrevention.Objects.Add(invoker);
            return new FunctionPointer<BurstTrampolineInvoker3>(Marshal.GetFunctionPointerForDelegate(invoker));
        }

        [AOT.MonoPInvokeCallback(typeof(BurstTrampolineInvoker3))]
        private static void Trampoline(IntPtr context, IntPtr data1Ptr, IntPtr data2Ptr, IntPtr data3Ptr)
        {
            var handle = GCHandle.FromIntPtr(context);
            ref var value1 = ref UnsafeUtility.AsRef<T1>((void*)data1Ptr);
            ref var value2 = ref UnsafeUtility.AsRef<T2>((void*)data2Ptr);
            ref var value3 = ref UnsafeUtility.AsRef<T3>((void*)data3Ptr);
            ((Delegate)handle.Target)(in value1, in value2, in value3);
        }
    }

    public unsafe readonly struct BurstTrampolineOut<T>
        where T : unmanaged
    {
        private static readonly FunctionPointer<BurstTrampolineInvoker1> StaticInvoker = CreateInvokerFunctionPointer();

        private readonly FunctionPointer<BurstTrampolineInvoker1> functionPointer;
        private readonly IntPtr context;

        public BurstTrampolineOut(Delegate method)
        {
            this.functionPointer = StaticInvoker;
            GarbagePrevention.Objects.Add(method);
            var handle = GCHandle.Alloc(method);
            this.context = GCHandle.ToIntPtr(handle);
        }

        public delegate void Delegate(out T data);

        public bool IsCreated => this.functionPointer.IsCreated;

        public void Invoke(out T data)
        {
            data = default;

            fixed (T* dataPtr = &data)
            {
                this.functionPointer.Invoke(this.context, (IntPtr)dataPtr);
            }
        }

        private static FunctionPointer<BurstTrampolineInvoker1> CreateInvokerFunctionPointer()
        {
            BurstTrampolineInvoker1 invoker = Trampoline;
            GarbagePrevention.Objects.Add(invoker);
            return new FunctionPointer<BurstTrampolineInvoker1>(Marshal.GetFunctionPointerForDelegate(invoker));
        }

        [AOT.MonoPInvokeCallback(typeof(BurstTrampolineInvoker1))]
        private static void Trampoline(IntPtr context, IntPtr data1Ptr)
        {
            var handle = GCHandle.FromIntPtr(context);
            ref var value = ref UnsafeUtility.AsRef<T>((void*)data1Ptr);
            ((Delegate)handle.Target)(out value);
        }
    }

    public unsafe readonly struct BurstTrampolineOut<TIn, TOut>
        where TIn : unmanaged
        where TOut : unmanaged
    {
        private static readonly FunctionPointer<BurstTrampolineInvoker2> StaticInvoker = CreateInvokerFunctionPointer();

        private readonly FunctionPointer<BurstTrampolineInvoker2> functionPointer;
        private readonly IntPtr context;

        public BurstTrampolineOut(Delegate method)
        {
            this.functionPointer = StaticInvoker;
            GarbagePrevention.Objects.Add(method);
            var handle = GCHandle.Alloc(method);
            this.context = GCHandle.ToIntPtr(handle);
        }

        public delegate void Delegate(in TIn data, out TOut outData);

        public bool IsCreated => this.functionPointer.IsCreated;

        [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1519:Braces should not be omitted from multi-line child statement", Justification = "Fixed")]
        public void Invoke(in TIn data, out TOut outData)
        {
            outData = default;

            fixed (TIn* dataPtr = &data)
            fixed (TOut* outDataPtr = &outData)
            {
                this.functionPointer.Invoke(this.context, (IntPtr)dataPtr, (IntPtr)outDataPtr);
            }
        }

        private static FunctionPointer<BurstTrampolineInvoker2> CreateInvokerFunctionPointer()
        {
            BurstTrampolineInvoker2 invoker = Trampoline;
            GarbagePrevention.Objects.Add(invoker);
            return new FunctionPointer<BurstTrampolineInvoker2>(Marshal.GetFunctionPointerForDelegate(invoker));
        }

        [AOT.MonoPInvokeCallback(typeof(BurstTrampolineInvoker2))]
        private static void Trampoline(IntPtr context, IntPtr data1Ptr, IntPtr data2Ptr)
        {
            var handle = GCHandle.FromIntPtr(context);
            ref var value = ref UnsafeUtility.AsRef<TIn>((void*)data1Ptr);
            ref var outValue = ref UnsafeUtility.AsRef<TOut>((void*)data2Ptr);
            ((Delegate)handle.Target)(in value, out outValue);
        }
    }

    public unsafe readonly struct BurstTrampolineOut<T1, T2, TOut>
        where T1 : unmanaged
        where T2 : unmanaged
        where TOut : unmanaged
    {
        private static readonly FunctionPointer<BurstTrampolineInvoker3> StaticInvoker = CreateInvokerFunctionPointer();

        private readonly FunctionPointer<BurstTrampolineInvoker3> functionPointer;
        private readonly IntPtr context;

        public BurstTrampolineOut(Delegate method)
        {
            this.functionPointer = StaticInvoker;
            GarbagePrevention.Objects.Add(method);
            var handle = GCHandle.Alloc(method);
            this.context = GCHandle.ToIntPtr(handle);
        }

        public delegate void Delegate(in T1 data1, in T2 data2, out TOut outData);

        public bool IsCreated => this.functionPointer.IsCreated;

        [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1519:Braces should not be omitted from multi-line child statement", Justification = "Fixed")]
        public void Invoke(in T1 data1, in T2 data2, out TOut outData)
        {
            outData = default;

            fixed (T1* data1Ptr = &data1)
            fixed (T2* data2Ptr = &data2)
            fixed (TOut* outDataPtr = &outData)
            {
                this.functionPointer.Invoke(this.context, (IntPtr)data1Ptr, (IntPtr)data2Ptr, (IntPtr)outDataPtr);
            }
        }

        private static FunctionPointer<BurstTrampolineInvoker3> CreateInvokerFunctionPointer()
        {
            BurstTrampolineInvoker3 invoker = Trampoline;
            GarbagePrevention.Objects.Add(invoker);
            return new FunctionPointer<BurstTrampolineInvoker3>(Marshal.GetFunctionPointerForDelegate(invoker));
        }

        [AOT.MonoPInvokeCallback(typeof(BurstTrampolineInvoker3))]
        private static void Trampoline(IntPtr context, IntPtr data1Ptr, IntPtr data2Ptr, IntPtr data3Ptr)
        {
            var handle = GCHandle.FromIntPtr(context);
            ref var value1 = ref UnsafeUtility.AsRef<T1>((void*)data1Ptr);
            ref var value2 = ref UnsafeUtility.AsRef<T2>((void*)data2Ptr);
            ref var outValue = ref UnsafeUtility.AsRef<TOut>((void*)data3Ptr);
            ((Delegate)handle.Target)(in value1, in value2, out outValue);
        }
    }

    internal static class GarbagePrevention
    {
        [SuppressMessage("ReSharper", "CollectionNeverQueried.Local", Justification = "GC Prevention")]
        [SuppressMessage("ReSharper", "CollectionNeverQueried.Global", Justification = "GC Prevention")]
        internal static readonly List<object> Objects = new();
    }
}
