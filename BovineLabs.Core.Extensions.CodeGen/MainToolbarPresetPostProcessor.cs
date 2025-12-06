// <copyright file="MainToolbarPresetPostProcessor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions.CodeGen
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using BovineLabs.Core.Editor;
    using JetBrains.Annotations;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Unity.CompilationPipeline.Common.Diagnostics;
    using Unity.CompilationPipeline.Common.ILPostProcessing;

    [UsedImplicitly]
    internal sealed class MainToolbarPresetPostProcessor : ILPostProcessor
    {
        private static readonly string MarkerAttributeFullName = typeof(MainToolbarPresetAttribute).FullName!;
        private const string AttributeAssemblyName = "BovineLabs.Core.Editor";
        private const string UnityAttributeFullName = "UnityEditor.Toolbars.UnityOnlyMainToolbarPresetAttribute";
        private const string UnityAssemblyName = "UnityEditor";

        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            if (string.Equals(compiledAssembly.Name, AttributeAssemblyName, StringComparison.Ordinal))
            {
                return true;
            }

            return compiledAssembly.References.Any(r => string.Equals(Path.GetFileNameWithoutExtension(r), AttributeAssemblyName, StringComparison.Ordinal));
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            var diagnostics = new List<DiagnosticMessage>();

            if (!this.WillProcess(compiledAssembly))
            {
                return new ILPostProcessResult(null, diagnostics);
            }

            var unityAttributeType = Type.GetType($"{UnityAttributeFullName}, {UnityAssemblyName}", false);
            if (unityAttributeType == null)
            {
                diagnostics.Add(new DiagnosticMessage
                {
                    DiagnosticType = DiagnosticType.Warning,
                    MessageData = $"Unable to resolve {UnityAttributeFullName} in assembly {UnityAssemblyName}.",
                });

                return new ILPostProcessResult(null, diagnostics);
            }

            var unityAttributeCtor = unityAttributeType.GetConstructor(Type.EmptyTypes);
            if (unityAttributeCtor == null)
            {
                diagnostics.Add(new DiagnosticMessage
                {
                    DiagnosticType = DiagnosticType.Warning,
                    MessageData = $"{UnityAttributeFullName} does not expose a parameterless constructor.",
                });

                return new ILPostProcessResult(null, diagnostics);
            }

            var assemblyDefinition = AssemblyDefinitionFor(compiledAssembly);
            var module = assemblyDefinition.MainModule;
            var unityAttributeCtorRef = module.ImportReference(unityAttributeCtor);

            var modified = false;
            foreach (var type in module.Types)
            {
                modified |= this.AddUnityAttribute(type, unityAttributeCtorRef);
            }

            if (!modified)
            {
                return new ILPostProcessResult(null, diagnostics);
            }

            var pe = new MemoryStream();
            var pdb = new MemoryStream();
            var writerParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = pdb,
                WriteSymbols = true,
            };

            assemblyDefinition.Write(pe, writerParameters);
            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), diagnostics);
        }

        private bool AddUnityAttribute(TypeDefinition type, MethodReference unityAttributeCtorRef)
        {
            var modified = false;

            if (type.HasMethods)
            {
                foreach (var method in type.Methods)
                {
                    if (!method.HasCustomAttributes)
                    {
                        continue;
                    }

                    var hasMarker = false;
                    var hasUnityAttribute = false;

                    foreach (var attribute in method.CustomAttributes)
                    {
                        var attributeTypeName = attribute.AttributeType.FullName;

                        hasMarker |= attributeTypeName == MarkerAttributeFullName;
                        hasUnityAttribute |= attributeTypeName == UnityAttributeFullName;
                    }

                    if (!hasMarker || hasUnityAttribute)
                    {
                        continue;
                    }

                    method.CustomAttributes.Add(new CustomAttribute(unityAttributeCtorRef));
                    modified = true;
                }
            }

            if (type.HasNestedTypes)
            {
                foreach (var t in type.NestedTypes)
                {
                    modified |= this.AddUnityAttribute(t, unityAttributeCtorRef);
                }
            }

            return modified;
        }

        private static AssemblyDefinition AssemblyDefinitionFor(ICompiledAssembly compiledAssembly)
        {
            var resolver = new PostProcessorAssemblyResolver(compiledAssembly);
            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                AssemblyResolver = resolver,
                ReflectionImporterProvider = new PostProcessorReflectionImporterProvider(),
                ReadingMode = ReadingMode.Immediate,
            };

            var peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData);
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(peStream, readerParameters);

            resolver.AddAssemblyDefinitionBeingOperatedOn(assemblyDefinition);

            return assemblyDefinition;
        }

        private sealed class PostProcessorAssemblyResolver : IAssemblyResolver
        {
            private readonly string[] referenceDirectories;
            private readonly Dictionary<string, HashSet<string>> referenceToPathMap;
            private readonly Dictionary<string, AssemblyDefinition> cache = new();
            private readonly ICompiledAssembly compiledAssembly;
            private AssemblyDefinition selfAssembly;

            public PostProcessorAssemblyResolver(ICompiledAssembly compiledAssembly)
            {
                this.compiledAssembly = compiledAssembly;
                this.referenceToPathMap = new Dictionary<string, HashSet<string>>();

                foreach (var reference in compiledAssembly.References)
                {
                    var assemblyName = Path.GetFileNameWithoutExtension(reference);
                    if (!this.referenceToPathMap.TryGetValue(assemblyName, out var fileList))
                    {
                        fileList = new HashSet<string>();
                        this.referenceToPathMap.Add(assemblyName, fileList);
                    }

                    fileList.Add(reference);
                }

                this.referenceDirectories = this.referenceToPathMap.Values.SelectMany(pathSet => pathSet.Select(Path.GetDirectoryName)).Distinct().ToArray();
            }

            public void Dispose()
            {
            }

            public AssemblyDefinition Resolve(AssemblyNameReference name)
            {
                return this.Resolve(name, new ReaderParameters(ReadingMode.Deferred));
            }

            public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
            {
                if (name.Name == this.compiledAssembly.Name)
                {
                    return this.selfAssembly!;
                }

                var fileName = this.FindFile(name);
                if (fileName == null)
                {
                    return null;
                }

                var cacheKey = fileName;

                if (this.cache.TryGetValue(cacheKey, out var result))
                {
                    return result;
                }

                parameters.AssemblyResolver = this;

                var ms = MemoryStreamFor(fileName);

                var pdb = fileName + ".pdb";
                if (File.Exists(pdb))
                {
                    parameters.SymbolStream = MemoryStreamFor(pdb);
                }

                var assemblyDefinition = AssemblyDefinition.ReadAssembly(ms, parameters);
                this.cache.Add(cacheKey, assemblyDefinition);
                return assemblyDefinition;
            }

            private string FindFile(AssemblyNameReference name)
            {
                if (this.referenceToPathMap.TryGetValue(name.Name, out var paths))
                {
                    if (paths.Count == 1)
                    {
                        return paths.First();
                    }

                    foreach (var path in paths)
                    {
                        var onDiskAssemblyName = AssemblyName.GetAssemblyName(path);
                        if (onDiskAssemblyName.FullName == name.FullName)
                        {
                            return path;
                        }
                    }

                    throw new ArgumentException($"Tried to resolve a reference in assembly '{name.FullName}' however the assembly could not be found. Known references which did not match: \n{string.Join("\n", paths)}");
                }

                foreach (var parentDir in this.referenceDirectories)
                {
                    var candidate = Path.Combine(parentDir, name.Name + ".dll");
                    if (File.Exists(candidate))
                    {
                        if (!this.referenceToPathMap.TryGetValue(candidate, out var referencePaths))
                        {
                            referencePaths = new HashSet<string>();
                            this.referenceToPathMap.Add(candidate, referencePaths);
                        }

                        referencePaths.Add(candidate);

                        return candidate;
                    }
                }

                return null;
            }

            private static MemoryStream MemoryStreamFor(string fileName)
            {
                return Retry(10, TimeSpan.FromSeconds(1), () =>
                {
                    byte[] byteArray;
                    using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        byteArray = new byte[fs.Length];
                        var readLength = fs.Read(byteArray, 0, (int)fs.Length);
                        if (readLength != fs.Length)
                        {
                            throw new InvalidOperationException("File read length is not full length of file.");
                        }
                    }

                    return new MemoryStream(byteArray);
                });
            }

            private static MemoryStream Retry(int retryCount, TimeSpan waitTime, Func<MemoryStream> func)
            {
                try
                {
                    return func();
                }
                catch (IOException)
                {
                    if (retryCount == 0)
                    {
                        throw;
                    }

                    Console.WriteLine($"Caught IO Exception, trying {retryCount} more times");
                    Thread.Sleep(waitTime);
                    return Retry(retryCount - 1, waitTime, func);
                }
            }

            public void AddAssemblyDefinitionBeingOperatedOn(AssemblyDefinition assemblyDefinition)
            {
                this.selfAssembly = assemblyDefinition;
            }
        }

        private sealed class PostProcessorReflectionImporterProvider : IReflectionImporterProvider
        {
            public IReflectionImporter GetReflectionImporter(ModuleDefinition module)
            {
                return new PostProcessorReflectionImporter(module);
            }
        }

        private sealed class PostProcessorReflectionImporter : DefaultReflectionImporter
        {
            private const string SystemPrivateCoreLib = "System.Private.CoreLib";
            private readonly AssemblyNameReference correctCorlib;

            public PostProcessorReflectionImporter(ModuleDefinition module)
                : base(module)
            {
                this.correctCorlib = module.AssemblyReferences.FirstOrDefault(a => a.Name is "mscorlib" or "netstandard" or SystemPrivateCoreLib);
            }

            public override AssemblyNameReference ImportReference(AssemblyName reference)
            {
                if (this.correctCorlib != null && reference.Name == SystemPrivateCoreLib)
                {
                    return this.correctCorlib;
                }

                return base.ImportReference(reference);
            }
        }
    }
}
