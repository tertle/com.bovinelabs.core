// <copyright file="DynamicGenerator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.DynamicGenerator
{
    using System;
    using System.Threading;
    using CodeGenHelpers;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [Generator]
    public class DynamicGenerator: IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var contextProvider = context
                .SyntaxProvider
                .CreateSyntaxProvider(predicate: IsSyntaxTargetForGeneration, transform: GetSemanticTargetForGeneration)
                .Where(t => t != null);

            context.RegisterSourceOutput(contextProvider, (productionContext, data) =>
            {
                try
                {
                    var builder = ProcessData(data);
                    if (builder == null)
                    {
                        return;
                    }

                    productionContext.AddSource(builder);
                }
                catch (Exception ex)
                {
                    SourceGenHelpers.Log($"Exception occured: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Is Struct
            if (syntaxNode is not StructDeclarationSyntax structDeclarationSyntax)
            {
                return false;
            }

            // Has Base List
            if (structDeclarationSyntax.BaseList == null)
            {
                return false;
            }

            // Has any IDynamic identifier
            var hasDynamic = false;
            foreach (var baseType in structDeclarationSyntax.BaseList.Types)

            {
                if (baseType.Type is GenericNameSyntax identifier && identifier.Identifier.ValueText.StartsWith("IDynamic"))
                {
                    hasDynamic = true;
                    break;
                }
            }

            if (!hasDynamic)
                return false;

            return true;
        }

        private static DynamicData GetSemanticTargetForGeneration(
            GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
        {
            var structSyntax = (StructDeclarationSyntax)ctx.Node;

            // Get the symbol for the struct
            INamedTypeSymbol ts = ctx.SemanticModel.GetDeclaredSymbol(structSyntax);
            if (ts == null)
            {
                return null;
            }

            // Check all interfaces implemented by the struct
            foreach (var interfaceSymbol in ts.AllInterfaces)
            {
                // Get the original definition (without type arguments)
                var originalDef = interfaceSymbol.OriginalDefinition;
                string name = originalDef.Name;
                int typeParamCount = originalDef.TypeParameters.Length;

                switch (name)
                {
                    // Check in the specified order
                    case "IDynamicHashMap" when typeParamCount == 2:
                        return new DynamicData(ts, DynamicType.HashMap, interfaceSymbol.TypeArguments[0], interfaceSymbol.TypeArguments[1]);
                    case "IDynamicHashSet" when typeParamCount == 1:
                        return new DynamicData(ts, DynamicType.HashSet, interfaceSymbol.TypeArguments[0]);
                    case "IDynamicMultiHashMap" when typeParamCount == 2:
                        return new DynamicData(ts, DynamicType.MultiHashMap, interfaceSymbol.TypeArguments[0], interfaceSymbol.TypeArguments[1]);
                    case "IDynamicPerfectHashMap" when typeParamCount == 2:
                        return new DynamicData(ts, DynamicType.PerfectHashMap, interfaceSymbol.TypeArguments[0], interfaceSymbol.TypeArguments[1]);
                    case "IDynamicUntypedHashMap" when typeParamCount == 1:
                        return new DynamicData(ts, DynamicType.UntypedHashMap, interfaceSymbol.TypeArguments[0]);
                    case "IDynamicVariableMap" when typeParamCount == 4:
                        return new DynamicData(ts, DynamicType.VariableMap, interfaceSymbol.TypeArguments[0], interfaceSymbol.TypeArguments[1], interfaceSymbol.TypeArguments[2], interfaceSymbol.TypeArguments[3]);
                    case "IDynamicVariableMap" when typeParamCount == 6:
                        return new DynamicData(ts, DynamicType.VariableMap2, interfaceSymbol.TypeArguments[0], interfaceSymbol.TypeArguments[1], interfaceSymbol.TypeArguments[2], interfaceSymbol.TypeArguments[3], interfaceSymbol.TypeArguments[4], interfaceSymbol.TypeArguments[5]);
                }
            }

            // No matching interface found
            return null;
        }

        private static ClassBuilder ProcessData(DynamicData data)
        {
            try
            {
                var builder = CodeBuilder
                    .Create(data.TypeSymbol.ContainingNamespace)
                    .AddClass(data.TypeSymbol.Name + "Extensions")
                    .WithAccessModifier(data.TypeSymbol.DeclaredAccessibility)
                    .OfType(TypeKind.Class)
                    .AddNamespaceImport("BovineLabs.Core.Iterators")
                    .AddNamespaceImport("Unity.Entities")
                    .AddNamespaceImport("System.Runtime.CompilerServices")
                    .MakeStaticClass();

                InitializeMethod(builder, data);
                AsMapMethod(builder, data);

                return builder;
            }
            catch (Exception ex)
            {
                SourceGenHelpers.Log(ex.ToString());
                return null;
            }
        }

        private static void InitializeMethod(ClassBuilder builder, DynamicData data)
        {
            // Skip Initialize method for PerfectHashMap as it requires special parameters
            if (data.Type == DynamicType.PerfectHashMap)
            {
                return;
            }

            var rt = $"DynamicBuffer<{data.TypeName}>";

            var initialize = builder
                .AddMethod("Initialize", Accessibility.Public)
                .WithReturnType(rt)
                .MakeStaticMethod()
                .AddAttribute("MethodImpl(MethodImplOptions.AggressiveInlining)");
            initialize.AddParameter($"this {rt}", "buffer");

            initialize.AddParameterWithDefaultValue("int", "capacity", "0");
            initialize.AddParameterWithDefaultValue("int", "minGrowth", "DynamicExtensions.DefaultMinGrowth");

            initialize.WithBody(b =>
            {
                var init = data.Type switch
                {
                    DynamicType.HashMap => $"InitializeHashMap<{data.TypeName}, {data.Type1}, {data.Type2}>",
                    DynamicType.HashSet => $"InitializeHashSet<{data.TypeName}, {data.Type1}>",
                    DynamicType.MultiHashMap => $"InitializeMultiHashMap<{data.TypeName}, {data.Type1}, {data.Type2}>",
                    DynamicType.UntypedHashMap => $"InitializeUntypedHashMap<{data.TypeName}, {data.Type1}>",
                    DynamicType.VariableMap => $"InitializeVariableMap<{data.TypeName}, {data.Type1}, {data.Type2}, {data.Type3}, {data.Type4}>",
                    DynamicType.VariableMap2 => $"InitializeVariableMap<{data.TypeName}, {data.Type1}, {data.Type2}, {data.Type3}, {data.Type4}, {data.Type5}, {data.Type6}>",
                    _ => throw new ArgumentOutOfRangeException()
                };

                b.AppendLine($"return buffer.{init}(capacity, minGrowth);");
            });
        }

        private static void AsMapMethod(ClassBuilder builder, DynamicData data)
        {
            var rt = data.Type switch
            {
                DynamicType.HashMap => $"DynamicHashMap<{data.Type1}, {data.Type2}>",
                DynamicType.HashSet => $"DynamicHashSet<{data.Type1}>",
                DynamicType.MultiHashMap => $"DynamicMultiHashMap<{data.Type1}, {data.Type2}>",
                DynamicType.PerfectHashMap => $"DynamicPerfectHashMap<{data.Type1}, {data.Type2}>",
                DynamicType.UntypedHashMap => $"DynamicUntypedHashMap<{data.Type1}>",
                DynamicType.VariableMap => $"DynamicVariableMap<{data.Type1}, {data.Type2}, {data.Type3}, {data.Type4}>",
                DynamicType.VariableMap2 => $"DynamicVariableMap<{data.Type1}, {data.Type2}, {data.Type3}, {data.Type4}, {data.Type5}, {data.Type6}>",
                _ => throw new ArgumentOutOfRangeException()
            };

            var asMap = builder
                .AddMethod("AsMap", Accessibility.Public)
                .WithReturnType(rt)
                .MakeStaticMethod()
                .AddAttribute("MethodImpl(MethodImplOptions.AggressiveInlining)");
            asMap.AddParameter($"this DynamicBuffer<{data.TypeName}>", "buffer");

            asMap.WithBody(b =>
            {
                var a = data.Type switch
                {
                    DynamicType.HashMap => $"AsHashMap<{data.TypeName}, {data.Type1}, {data.Type2}>",
                    DynamicType.HashSet => $"AsHashSet<{data.TypeName}, {data.Type1}>",
                    DynamicType.MultiHashMap => $"AsMultiHashMap<{data.TypeName}, {data.Type1}, {data.Type2}>",
                    DynamicType.PerfectHashMap => $"AsPerfectHashMap<{data.TypeName}, {data.Type1}, {data.Type2}>",
                    DynamicType.UntypedHashMap => $"AsUntypedHashMap<{data.TypeName}, {data.Type1}>",
                    DynamicType.VariableMap => $"AsVariableMap<{data.TypeName}, {data.Type1}, {data.Type2}, {data.Type3}, {data.Type4}>",
                    DynamicType.VariableMap2 => $"AsVariableMap<{data.TypeName}, {data.Type1}, {data.Type2}, {data.Type3}, {data.Type4}, {data.Type5}, {data.Type6}>",
                    _ => throw new ArgumentOutOfRangeException(),
                };

                b.AppendLine($"return buffer.{a}();");
            });
        }

        private enum DynamicType
        {
            HashMap,
            HashSet,
            MultiHashMap,
            PerfectHashMap,
            UntypedHashMap,
            VariableMap,
            VariableMap2,
        }

        private class DynamicData
        {
            public readonly ITypeSymbol TypeSymbol;
            public readonly string TypeName;
            public readonly DynamicType Type;
            public readonly string Type1;
            public readonly string Type2;
            public readonly string Type3;
            public readonly string Type4;
            public readonly string Type5;
            public readonly string Type6;

            public DynamicData(ITypeSymbol typeSymbol, DynamicType type, ITypeSymbol type1, ITypeSymbol type2 = null, ITypeSymbol type3 = null, ITypeSymbol type4 = null, ITypeSymbol type5 = null, ITypeSymbol type6 = null)
            {
                this.TypeSymbol = typeSymbol;
                this.TypeName = GetName(typeSymbol);
                this.Type = type;
                this.Type1 = GetName(type1);
                this.Type2 = GetName(type2);
                this.Type3 = GetName(type3);
                this.Type4 = GetName(type4);
                this.Type5 = GetName(type5);
                this.Type6 = GetName(type6);
            }

            private static SymbolDisplayFormat QualifiedFormat { get; } = new(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included, genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            private static string GetName(ITypeSymbol symbol)
            {
                return symbol?.ToDisplayString(QualifiedFormat);
            }
        }
    }
}
