// <copyright file="NativeStreamExtensionsPerformanceTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PERFORMANCE
namespace BovineLabs.Core.PerformanceTests.Extensions
{
    using BovineLabs.Core.Extensions;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.PerformanceTesting;

    public class NativeStreamExtensionsPerformanceTests
    {
        [TestCase(5120)]
        [TestCase(81920)]
        [TestCase(655360)]
        [Performance]
        public unsafe void WriteLarge(int size)
        {
            NativeStream stream = default;

            var sourceData = new NativeArray<byte>(size, Allocator.TempJob);
            for (var i = 0; i < size; i++)
            {
                sourceData[i] = (byte)(i % 255);
            }

            Measure.Method(() =>
                {
                    var writer = stream.AsWriter();
                    writer.BeginForEachIndex(0);
                    writer.WriteLarge((byte*)sourceData.GetUnsafeReadOnlyPtr(), size);
                    writer.EndForEachIndex();
                })
                .SetUp(() => { stream = new NativeStream(1, Allocator.TempJob); })
                .CleanUp(() => stream.Dispose())
                .Run();

            sourceData.Dispose();
        }

        [TestCase(5120)]
        [TestCase(81920)]
        [TestCase(655360)]
        [Performance]
        public void WriteLargeBurst(int size)
        {
            NativeStream stream = default;

            var sourceData = new NativeArray<byte>(size, Allocator.TempJob);
            for (var i = 0; i < size; i++)
            {
                sourceData[i] = (byte)(i % 255);
            }

            Measure.Method(() => { new WriteLargeJob { Writer = stream.AsWriter(), SourceData = sourceData }.Run(); })
                .SetUp(() => { stream = new NativeStream(1, Allocator.TempJob); })
                .CleanUp(() => stream.Dispose())
                .Run();

            sourceData.Dispose();
        }

        [TestCase(5120)]
        [TestCase(81920)]
        [TestCase(655360)]
        [Performance]
        public void Write(int size)
        {
            NativeStream stream = default;

            var sourceData = new NativeArray<byte>(size, Allocator.TempJob);
            for (var i = 0; i < size; i++)
            {
                sourceData[i] = (byte)(i % 255);
            }

            Measure.Method(() =>
                {
                    var writer = stream.AsWriter();
                    writer.BeginForEachIndex(0);
                    for (var i = 0; i < size; i++)
                    {
                        writer.Write(sourceData[i]);
                    }

                    writer.EndForEachIndex();
                })
                .SetUp(() => { stream = new NativeStream(1, Allocator.TempJob); })
                .CleanUp(() => stream.Dispose())
                .Run();

            sourceData.Dispose();
        }

        [TestCase(5120)]
        [TestCase(81920)]
        [TestCase(655360)]
        [Performance]
        public void WriteBurst(int size)
        {
            NativeStream stream = default;

            var sourceData = new NativeArray<byte>(size, Allocator.TempJob);
            for (var i = 0; i < size; i++)
            {
                sourceData[i] = (byte)(i % 255);
            }

            Measure.Method(() => { new WriteJob { Writer = stream.AsWriter(), SourceData = sourceData }.Run(); })
                .SetUp(() => { stream = new NativeStream(1, Allocator.TempJob); })
                .CleanUp(() => stream.Dispose())
                .Run();

            sourceData.Dispose();
        }

        [TestCase(5120)]
        [TestCase(81920)]
        [TestCase(655360)]
        [Performance]
        public unsafe void ReadLarge(int size)
        {
            NativeStream stream = default;

            var sourceData = new NativeArray<byte>(size, Allocator.TempJob);
            for (var i = 0; i < size; i++)
            {
                sourceData[i] = (byte)(i % 255);
            }

            Measure.Method(() =>
                {
                    var reader = stream.AsReader();
                    reader.BeginForEachIndex(0);
                    var array = new NativeArray<byte>(size, Allocator.Temp);
                    reader.ReadLarge((byte*)array.GetUnsafePtr(), size);
                })
                .SetUp(() =>
                {
                    stream = new NativeStream(1, Allocator.TempJob);
                    var writer = stream.AsWriter();
                    writer.BeginForEachIndex(0);
                    writer.WriteLarge((byte*)sourceData.GetUnsafeReadOnlyPtr(), size);
                    writer.EndForEachIndex();
                })
                .CleanUp(() => stream.Dispose())
                .Run();

            sourceData.Dispose();
        }

        [TestCase(5120)]
        [TestCase(81920)]
        [TestCase(655360)]
        [Performance]
        public unsafe void ReadLargeBurst(int size)
        {
            NativeStream stream = default;

            var sourceData = new NativeArray<byte>(size, Allocator.TempJob);
            for (var i = 0; i < size; i++)
            {
                sourceData[i] = (byte)(i % 255);
            }

            Measure.Method(() => { new ReadLargeJob { Reader = stream.AsReader(), Size = size }.Run(); })
                .SetUp(() =>
                {
                    stream = new NativeStream(1, Allocator.TempJob);
                    var writer = stream.AsWriter();
                    writer.BeginForEachIndex(0);
                    writer.WriteLarge((byte*)sourceData.GetUnsafeReadOnlyPtr(), size);
                    writer.EndForEachIndex();
                })
                .CleanUp(() => stream.Dispose())
                .Run();

            sourceData.Dispose();
        }

        [TestCase(5120)]
        [TestCase(81920)]
        [TestCase(655360)]
        [Performance]
        public void Read(int size)
        {
            NativeStream stream = default;

            var sourceData = new NativeArray<byte>(size, Allocator.TempJob);
            for (var i = 0; i < size; i++)
            {
                sourceData[i] = (byte)(i % 255);
            }

            Measure.Method(() =>
                {
                    var reader = stream.AsReader();
                    reader.BeginForEachIndex(0);

                    var nativeArray = new NativeArray<byte>(size, Allocator.Temp);
                    for (var i = 0; i < size; i++)
                    {
                        nativeArray[i] = reader.Read<byte>();
                    }
                })
                .SetUp(() =>
                {
                    stream = new NativeStream(1, Allocator.TempJob);
                    var writer = stream.AsWriter();
                    writer.BeginForEachIndex(0);
                    for (var i = 0; i < size; i++)
                    {
                        writer.Write(sourceData[i]);
                    }

                    writer.EndForEachIndex();
                })
                .CleanUp(() => stream.Dispose())
                .Run();

            sourceData.Dispose();
        }

        [TestCase(5120)]
        [TestCase(81920)]
        [TestCase(655360)]
        [Performance]
        public void ReadBurst(int size)
        {
            NativeStream stream = default;

            var sourceData = new NativeArray<byte>(size, Allocator.TempJob);
            for (var i = 0; i < size; i++)
            {
                sourceData[i] = (byte)(i % 255);
            }

            Measure.Method(() => { new ReadJob { Reader = stream.AsReader(), Size = size }.Run(); })
                .SetUp(() =>
                {
                    stream = new NativeStream(1, Allocator.TempJob);
                    var writer = stream.AsWriter();
                    writer.BeginForEachIndex(0);
                    for (var i = 0; i < size; i++)
                    {
                        writer.Write(sourceData[i]);
                    }

                    writer.EndForEachIndex();
                })
                .CleanUp(() => stream.Dispose())
                .Run();

            sourceData.Dispose();
        }

        [BurstCompile(CompileSynchronously = true)]
        private unsafe struct WriteLargeJob : IJob
        {
            public NativeStream.Writer Writer;

            [ReadOnly]
            public NativeArray<byte> SourceData;

            public void Execute()
            {
                this.Writer.BeginForEachIndex(0);
                this.Writer.WriteLarge((byte*)this.SourceData.GetUnsafeReadOnlyPtr(), this.SourceData.Length);
                this.Writer.EndForEachIndex();
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct WriteJob : IJob
        {
            public NativeStream.Writer Writer;

            [ReadOnly]
            public NativeArray<byte> SourceData;

            public void Execute()
            {
                this.Writer.BeginForEachIndex(0);
                for (var i = 0; i < this.SourceData.Length; i++)
                {
                    this.Writer.Write(this.SourceData[i]);
                }

                this.Writer.EndForEachIndex();
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private unsafe struct ReadLargeJob : IJob
        {
            [ReadOnly]
            public NativeStream.Reader Reader;

            public int Size;

            public void Execute()
            {
                this.Reader.BeginForEachIndex(0);
                var array = new NativeArray<byte>(this.Size, Allocator.Temp);
                this.Reader.ReadLarge((byte*)array.GetUnsafePtr(), this.Size);
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct ReadJob : IJob
        {
            [ReadOnly]
            public NativeStream.Reader Reader;

            public int Size;

            public void Execute()
            {
                this.Reader.BeginForEachIndex(0);

                var nativeArray = new NativeArray<byte>(this.Size, Allocator.Temp);
                for (var i = 0; i < this.Size; i++)
                {
                    nativeArray[i] = this.Reader.Read<byte>();
                }
            }
        }
    }
}
#endif
