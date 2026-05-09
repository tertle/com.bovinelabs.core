// <copyright file="BurstTrampolineExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    public struct BurstManagedNoArgs
    {
        public byte Value;
    }

    public struct BurstManagedPair<TFirst, TSecond>
        where TFirst : unmanaged
        where TSecond : unmanaged
    {
        public TFirst First;
        public TSecond Second;
    }

    public struct BurstManagedTriple<TFirst, TSecond, TThird>
        where TFirst : unmanaged
        where TSecond : unmanaged
        where TThird : unmanaged
    {
        public TFirst First;
        public TSecond Second;
        public TThird Third;
    }

    public static class BurstTrampolineExtensions
    {
        public static void Invoke(this BurstTrampoline wrapper)
        {
            var arguments = default(BurstManagedNoArgs);
            wrapper.Invoke(ref arguments);
        }

        public static void Invoke<T>(this BurstTrampoline wrapper, in T value)
            where T : unmanaged
        {
            var arguments = value;
            wrapper.Invoke(ref arguments);
        }

        public static void Invoke<TFirst, TSecond>(this BurstTrampoline wrapper, in TFirst first, in TSecond second)
            where TFirst : unmanaged
            where TSecond : unmanaged
        {
            var arguments = new BurstManagedPair<TFirst, TSecond> { First = first, Second = second };
            wrapper.Invoke(ref arguments);
        }

        public static void Invoke<TFirst, TSecond, TThird>(this BurstTrampoline wrapper, in TFirst first, in TSecond second, in TThird third)
            where TFirst : unmanaged
            where TSecond : unmanaged
            where TThird : unmanaged
        {
            var arguments = new BurstManagedTriple<TFirst, TSecond, TThird> { First = first, Second = second, Third = third };
            wrapper.Invoke(ref arguments);
        }

        public static void InvokeRef<TRef>(this BurstTrampoline wrapper, ref TRef value)
            where TRef : unmanaged
        {
            wrapper.Invoke(ref value);
        }

        public static void InvokeOut<TOut>(this BurstTrampoline wrapper, out TOut value)
            where TOut : unmanaged
        {
            value = default;
            wrapper.Invoke(ref value);
        }

        public static void InvokeOut<TIn, TOut>(this BurstTrampoline wrapper, in TIn input, out TOut value)
            where TIn : unmanaged
            where TOut : unmanaged
        {
            var arguments = new BurstManagedPair<TIn, TOut> { First = input };
            wrapper.Invoke(ref arguments);
            value = arguments.Second;
        }

        public static void InvokeOut<TFirst, TSecond, TOut>(this BurstTrampoline wrapper, in TFirst first, in TSecond second, out TOut value)
            where TFirst : unmanaged
            where TSecond : unmanaged
            where TOut : unmanaged
        {
            var arguments = new BurstManagedTriple<TFirst, TSecond, TOut> { First = first, Second = second };
            wrapper.Invoke(ref arguments);
            value = arguments.Third;
        }
    }
}
