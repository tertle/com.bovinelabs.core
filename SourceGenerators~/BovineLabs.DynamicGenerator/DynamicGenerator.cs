// <copyright file="DynamicGenerator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.DynamicGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using CodeGenHelpers;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [Generator]
    public class DynamicGenerator : IIncrementalGenerator
    {
        internal static readonly SymbolDisplayFormat ShortTypeFormat =
            SymbolDisplayFormat.MinimallyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidates = context
                .SyntaxProvider
                .CreateSyntaxProvider(predicate: IsSyntaxTargetForGeneration, transform: GetCandidate)
                .Where(static t => t != null);

            context.RegisterSourceOutput(
                candidates,
                static (productionContext, candidate) => Execute(productionContext, candidate));
        }

        private static void Execute(SourceProductionContext context, DynamicCandidate candidate)
        {
            DynamicResult result;
            try
            {
                result = GetSemanticTargetForGeneration(candidate, context.CancellationToken);
            }
            catch (Exception ex)
            {
                SourceGenHelpers.Log($"Exception occurred: {ex.Message}\n{ex.StackTrace}");
                return;
            }

            if (result == null)
            {
                return;
            }

            foreach (var diagnostic in result.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            if (result.Data == null)
            {
                return;
            }

            var builder = ProcessData(result.Data);
            if (builder != null)
            {
                context.AddSource(builder);
            }
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (syntaxNode is not TypeDeclarationSyntax { BaseList: { } baseList } typeDeclaration ||
                (typeDeclaration.Kind() != SyntaxKind.StructDeclaration && typeDeclaration.Kind() != SyntaxKind.ClassDeclaration))
            {
                return false;
            }

            foreach (var baseType in baseList.Types)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (baseType.Type is GenericNameSyntax identifier && identifier.Identifier.ValueText.StartsWith("IDynamic", StringComparison.Ordinal))
                {
                    return true;
                }

                if (baseType.Type is QualifiedNameSyntax { Right: GenericNameSyntax identifierName } &&
                    identifierName.Identifier.ValueText.StartsWith("IDynamic", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static DynamicCandidate GetCandidate(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
        {
            var typeDeclaration = (TypeDeclarationSyntax)ctx.Node;
            var typeSymbol = ctx.SemanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
            if (typeSymbol == null)
            {
                return null;
            }

            return new DynamicCandidate(typeDeclaration, typeSymbol);
        }

        private static DynamicResult GetSemanticTargetForGeneration(DynamicCandidate candidate, CancellationToken cancellationToken)
        {
            var typeSymbol = candidate.TypeSymbol;
            var typeSyntax = candidate.TypeSyntax;

            var dynamicInterfaces = GetDynamicInterfaces(typeSymbol, cancellationToken);
            if (dynamicInterfaces.Count == 0)
            {
                return null;
            }

            var diagnostics = new List<Diagnostic>();

            if (typeSymbol.TypeKind != TypeKind.Struct)
            {
                diagnostics.Add(DynamicDiagnostics.NonStruct(typeSymbol, typeSyntax.Identifier.GetLocation()));
                return new DynamicResult(null, diagnostics);
            }

            if (dynamicInterfaces.Count > 1)
            {
                diagnostics.Add(DynamicDiagnostics.MultipleInterfaces(typeSymbol, typeSyntax.Identifier.GetLocation()));
                return new DynamicResult(null, diagnostics);
            }

            var data = CreateData(typeSymbol, dynamicInterfaces[0]);
            return new DynamicResult(data, diagnostics);
        }

        private static IReadOnlyList<DynamicInterface> GetDynamicInterfaces(INamedTypeSymbol typeSymbol, CancellationToken cancellationToken)
        {
            var matches = new List<DynamicInterface>();
            var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            foreach (var interfaceSymbol in typeSymbol.AllInterfaces)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var originalDef = interfaceSymbol.OriginalDefinition;
                if (!seen.Add(originalDef))
                {
                    continue;
                }

                var name = originalDef.Name;
                var typeParamCount = originalDef.TypeParameters.Length;

                switch (name)
                {
                    case "IDynamicHashMap" when typeParamCount == 2:
                        matches.Add(new DynamicInterface(DynamicType.HashMap, interfaceSymbol));
                        break;
                    case "IDynamicHashSet" when typeParamCount == 1:
                        matches.Add(new DynamicInterface(DynamicType.HashSet, interfaceSymbol));
                        break;
                    case "IDynamicMultiHashMap" when typeParamCount == 2:
                        matches.Add(new DynamicInterface(DynamicType.MultiHashMap, interfaceSymbol));
                        break;
                    case "IDynamicPerfectHashMap" when typeParamCount == 2:
                        matches.Add(new DynamicInterface(DynamicType.PerfectHashMap, interfaceSymbol));
                        break;
                    case "IDynamicUntypedHashMap" when typeParamCount == 1:
                        matches.Add(new DynamicInterface(DynamicType.UntypedHashMap, interfaceSymbol));
                        break;
                    case "IDynamicVariableMap" when typeParamCount == 4:
                        matches.Add(new DynamicInterface(DynamicType.VariableMap, interfaceSymbol));
                        break;
                    case "IDynamicVariableMap" when typeParamCount == 6:
                        matches.Add(new DynamicInterface(DynamicType.VariableMap2, interfaceSymbol));
                        break;
                }
            }

            return matches;
        }

        private static DynamicData CreateData(INamedTypeSymbol typeSymbol, DynamicInterface dynamicInterface)
        {
            var arguments = dynamicInterface.InterfaceSymbol.TypeArguments;

            return dynamicInterface.Type switch
            {
                DynamicType.HashMap => new DynamicData(typeSymbol, dynamicInterface.Type, arguments[0], arguments[1]),
                DynamicType.HashSet => new DynamicData(typeSymbol, dynamicInterface.Type, arguments[0]),
                DynamicType.MultiHashMap => new DynamicData(typeSymbol, dynamicInterface.Type, arguments[0], arguments[1]),
                DynamicType.PerfectHashMap => new DynamicData(typeSymbol, dynamicInterface.Type, arguments[0], arguments[1]),
                DynamicType.UntypedHashMap => new DynamicData(typeSymbol, dynamicInterface.Type, arguments[0]),
                DynamicType.VariableMap => new DynamicData(typeSymbol, dynamicInterface.Type, arguments[0], arguments[1], arguments[2], arguments[3]),
                DynamicType.VariableMap2 => new DynamicData(typeSymbol, dynamicInterface.Type, arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5]),
                _ => throw new ArgumentOutOfRangeException(nameof(dynamicInterface.Type), dynamicInterface.Type, "Unexpected dynamic interface type"),
            };
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
            public DynamicData(
                INamedTypeSymbol typeSymbol,
                DynamicType type,
                ITypeSymbol type1,
                ITypeSymbol type2 = null,
                ITypeSymbol type3 = null,
                ITypeSymbol type4 = null,
                ITypeSymbol type5 = null,
                ITypeSymbol type6 = null)
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

            public INamedTypeSymbol TypeSymbol { get; }

            public string TypeName { get; }

            public DynamicType Type { get; }

            public string Type1 { get; }

            public string Type2 { get; }

            public string Type3 { get; }

            public string Type4 { get; }

            public string Type5 { get; }

            public string Type6 { get; }

            private static SymbolDisplayFormat QualifiedFormat { get; } = new(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            private static string GetName(ITypeSymbol symbol)
            {
                return symbol?.ToDisplayString(QualifiedFormat);
            }
        }

        private class DynamicResult
        {
            public DynamicResult(DynamicData data, IReadOnlyList<Diagnostic> diagnostics)
            {
                this.Data = data;
                this.Diagnostics = diagnostics;
            }

            public DynamicData Data { get; }

            public IReadOnlyList<Diagnostic> Diagnostics { get; }
        }

        private class DynamicInterface
        {
            public DynamicInterface(DynamicType type, INamedTypeSymbol interfaceSymbol)
            {
                this.Type = type;
                this.InterfaceSymbol = interfaceSymbol;
            }

            public DynamicType Type { get; }

            public INamedTypeSymbol InterfaceSymbol { get; }
        }

        private class DynamicCandidate
        {
            public DynamicCandidate(TypeDeclarationSyntax typeSyntax, INamedTypeSymbol typeSymbol)
            {
                this.TypeSyntax = typeSyntax;
                this.TypeSymbol = typeSymbol;
            }

            public TypeDeclarationSyntax TypeSyntax { get; }

            public INamedTypeSymbol TypeSymbol { get; }
        }
    }
}
