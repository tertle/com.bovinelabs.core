// <copyright file="FacetGenerator.Semantics.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.FacetGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class FacetGenerator
    {
        private static bool IsSyntaxTargetForGeneration(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (syntaxNode is not TypeDeclarationSyntax typeDeclaration)
            {
                return false;
            }

            if (typeDeclaration.Kind() != SyntaxKind.StructDeclaration && typeDeclaration.Kind() != SyntaxKind.ClassDeclaration)
            {
                return false;
            }

            if (typeDeclaration.BaseList == null)
            {
                return false;
            }

            foreach (var baseType in typeDeclaration.BaseList.Types)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (baseType.Type is IdentifierNameSyntax { Identifier: { ValueText: "IFacet" } })
                {
                    return true;
                }

                if (baseType.Type is QualifiedNameSyntax { Right: { Identifier: { ValueText: "IFacet" } } })
                {
                    return true;
                }
            }

            return false;
        }

        private static FacetCandidate GetFacetCandidate(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
        {
            var typeSyntax = (TypeDeclarationSyntax)ctx.Node;
            var typeSymbol = ctx.SemanticModel.GetDeclaredSymbol(typeSyntax, cancellationToken);
            if (typeSymbol == null)
            {
                return null;
            }

            return new FacetCandidate(typeSyntax, typeSymbol);
        }

        private static FacetResult GetSemanticTargetForGeneration(FacetCandidate candidate, FacetSymbols symbols, CancellationToken cancellationToken)
        {
            var typeSyntax = candidate.TypeSyntax;
            var typeSymbol = candidate.TypeSymbol;

            var diagnostics = new List<Diagnostic>();

            var facetInterface = symbols.FacetInterface;
            if (facetInterface == null || !typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, facetInterface)))
            {
                return null;
            }

            if (typeSymbol.TypeKind != TypeKind.Struct)
            {
                return new FacetResult(null, new[] { FacetDiagnostics.NonStructFacet(typeSymbol, typeSyntax.Identifier.GetLocation()) });
            }

            if (!typeSyntax.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                diagnostics.Add(FacetDiagnostics.MissingPartial(typeSymbol, typeSyntax.Identifier.GetLocation()));
            }

            INamedTypeSymbol optionalAttribute = symbols.OptionalAttribute;
            INamedTypeSymbol facetAttribute = symbols.FacetAttribute;
            INamedTypeSymbol readOnlyAttribute = symbols.ReadOnlyAttribute;
            INamedTypeSymbol singletonAttribute = symbols.SingletonAttribute;
            INamedTypeSymbol entityType = symbols.EntityType;
            INamedTypeSymbol entityStorageInfoType = symbols.EntityStorageInfoType;
            INamedTypeSymbol entityStorageInfoLookupType = symbols.EntityStorageInfoLookupType;
            INamedTypeSymbol componentLookupType = symbols.ComponentLookupType;
            INamedTypeSymbol bufferLookupType = symbols.BufferLookupType;
            INamedTypeSymbol bufferElementDataType = symbols.BufferElementDataType;
            INamedTypeSymbol facetEnabledRefRWType = symbols.FacetEnabledRefRWType;

            var fields = new List<FacetField>();
            foreach (var fieldSymbol in typeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (fieldSymbol.IsStatic)
                {
                    continue;
                }

                if (TryCreateFacetField(
                    fieldSymbol,
                    optionalAttribute,
                    facetAttribute,
                    readOnlyAttribute,
                    singletonAttribute,
                    entityType,
                    entityStorageInfoType,
                    entityStorageInfoLookupType,
                    componentLookupType,
                    bufferLookupType,
                    bufferElementDataType,
                    facetEnabledRefRWType,
                    facetInterface,
                    diagnostics,
                    out var field))
                {
                    fields.Add(field);
                }
            }

            if (fields.Count == 0)
            {
                diagnostics.Add(FacetDiagnostics.NoFields(typeSymbol, typeSyntax.Identifier.GetLocation()));
            }

            ValidateFacetGraph(typeSymbol, facetAttribute, facetInterface, diagnostics);

            var hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
            IReadOnlyList<FacetSingletonDependency> singletonDependencies = Array.Empty<FacetSingletonDependency>();
            IReadOnlyList<QueryBuilderInvocation> queryBuilderInvocations = Array.Empty<QueryBuilderInvocation>();

            if (!hasErrors && fields.Count > 0)
            {
                var singletonCache = new Dictionary<FacetTraversalKey, IReadOnlyList<FacetSingletonDependency>>();
                singletonDependencies = CollectSingletonDependencies(typeSymbol, fields, facetAttribute, singletonAttribute, readOnlyAttribute, facetInterface, singletonCache);
                queryBuilderInvocations = CollectQueryBuilderInvocations(
                    typeSymbol,
                    fields,
                    optionalAttribute,
                    facetAttribute,
                    readOnlyAttribute,
                    singletonAttribute,
                    entityType,
                    entityStorageInfoType,
                    entityStorageInfoLookupType,
                    componentLookupType,
                    bufferLookupType,
                    bufferElementDataType,
                    facetEnabledRefRWType,
                    facetInterface);
            }

            var data = !hasErrors && fields.Count > 0
                ? new FacetData(typeSymbol, fields, singletonDependencies, queryBuilderInvocations)
                : null;

            return new FacetResult(data, diagnostics);
        }

        private static IReadOnlyList<FacetSingletonDependency> CollectSingletonDependencies(
            INamedTypeSymbol typeSymbol,
            IReadOnlyList<FacetField> fields,
            INamedTypeSymbol facetAttribute,
            INamedTypeSymbol singletonAttribute,
            INamedTypeSymbol readOnlyAttribute,
            INamedTypeSymbol facetInterface,
            IDictionary<FacetTraversalKey, IReadOnlyList<FacetSingletonDependency>> singletonCache)
        {
            var dependencies = new List<FacetSingletonDependency>();

            foreach (var field in fields)
            {
                if (field.IsSingleton)
                {
                    var parameterName = CreateSingletonParameterName(null, field.FieldName);
                    dependencies.Add(new FacetSingletonDependency(parameterName, field));
                    continue;
                }

                if (!field.IsFacet || field.ComponentTypeSymbol is not INamedTypeSymbol facetType)
                {
                    field.SetFacetSingletonDependencies(Array.Empty<FacetSingletonDependency>());
                    continue;
                }

                var nestedDependencies = CollectFacetSingletonDependencies(
                    facetType,
                    facetAttribute,
                    singletonAttribute,
                    readOnlyAttribute,
                    facetInterface,
                    new[] { field.FieldName },
                    singletonCache,
                    new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default) { typeSymbol });

                field.SetFacetSingletonDependencies(nestedDependencies);
                dependencies.AddRange(nestedDependencies);
            }

            return dependencies;
        }

        private static IReadOnlyList<FacetSingletonDependency> CollectFacetSingletonDependencies(
            INamedTypeSymbol facetType,
            INamedTypeSymbol facetAttribute,
            INamedTypeSymbol singletonAttribute,
            INamedTypeSymbol readOnlyAttribute,
            INamedTypeSymbol facetInterface,
            IReadOnlyList<string> path,
            IDictionary<FacetTraversalKey, IReadOnlyList<FacetSingletonDependency>> singletonCache,
            ISet<INamedTypeSymbol> recursionStack)
        {
            var cacheKey = new FacetTraversalKey(facetType, CreatePathKey(path));
            if (singletonCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var dependencies = new List<FacetSingletonDependency>();

            if (!recursionStack.Add(facetType))
            {
                singletonCache[cacheKey] = dependencies;
                return dependencies;
            }

            foreach (var fieldSymbol in facetType.GetMembers().OfType<IFieldSymbol>())
            {
                var attributes = fieldSymbol.GetAttributes();

                if (fieldSymbol.IsStatic)
                {
                    continue;
                }

                if (HasAttribute(attributes, singletonAttribute))
                {
                    var hasReadOnlyAttribute = HasAttribute(attributes, readOnlyAttribute);
                    var singletonField = new FacetField(fieldSymbol, fieldSymbol.Type, FacetFieldKind.Singleton, false, true, hasReadOnlyAttribute);
                    var parameterName = CreateSingletonParameterName(path, singletonField.FieldName);
                    dependencies.Add(new FacetSingletonDependency(parameterName, singletonField));
                    continue;
                }

                if (!HasAttribute(attributes, facetAttribute) ||
                    fieldSymbol.Type is not INamedTypeSymbol { TypeKind: TypeKind.Struct } nestedFacetType ||
                    facetInterface == null ||
                    !nestedFacetType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, facetInterface)))
                {
                    continue;
                }

                var nestedPath = new List<string>(path) { fieldSymbol.Name };
                var nestedDependencies = CollectFacetSingletonDependencies(
                    nestedFacetType,
                    facetAttribute,
                    singletonAttribute,
                    readOnlyAttribute,
                    facetInterface,
                    nestedPath,
                    singletonCache,
                    recursionStack);

                dependencies.AddRange(nestedDependencies);
            }

            recursionStack.Remove(facetType);
            singletonCache[cacheKey] = dependencies;
            return dependencies;
        }

        private static void ValidateFacetGraph(
            INamedTypeSymbol typeSymbol,
            INamedTypeSymbol facetAttribute,
            INamedTypeSymbol facetInterface,
            IList<Diagnostic> diagnostics)
        {
            if (facetAttribute == null || facetInterface == null)
            {
                return;
            }

            var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var recursionStack = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            void Walk(INamedTypeSymbol current)
            {
                if (!recursionStack.Add(current))
                {
                    return;
                }

                foreach (var fieldSymbol in current.GetMembers().OfType<IFieldSymbol>())
                {
                    if (fieldSymbol.IsStatic)
                    {
                        continue;
                    }

                    var attributes = fieldSymbol.GetAttributes();
                    if (!HasAttribute(attributes, facetAttribute))
                    {
                        continue;
                    }

                    if (fieldSymbol.Type is not INamedTypeSymbol nestedFacetType || nestedFacetType.TypeKind != TypeKind.Struct)
                    {
                        continue;
                    }

                    if (!nestedFacetType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, facetInterface)))
                    {
                        continue;
                    }

                    if (recursionStack.Contains(nestedFacetType))
                    {
                        diagnostics.Add(FacetDiagnostics.FacetCycle(fieldSymbol, fieldSymbol.Locations.FirstOrDefault(), nestedFacetType));
                        continue;
                    }

                    if (!visited.Contains(nestedFacetType))
                    {
                        Walk(nestedFacetType);
                    }
                }

                recursionStack.Remove(current);
                visited.Add(current);
            }

            Walk(typeSymbol);
        }

        private static IReadOnlyList<QueryBuilderInvocation> CollectQueryBuilderInvocations(
            INamedTypeSymbol typeSymbol,
            IReadOnlyList<FacetField> fields,
            INamedTypeSymbol optionalAttribute,
            INamedTypeSymbol facetAttribute,
            INamedTypeSymbol readOnlyAttribute,
            INamedTypeSymbol singletonAttribute,
            INamedTypeSymbol entityType,
            INamedTypeSymbol entityStorageInfoType,
            INamedTypeSymbol entityStorageInfoLookupType,
            INamedTypeSymbol componentLookupType,
            INamedTypeSymbol bufferLookupType,
            INamedTypeSymbol bufferElementDataType,
            INamedTypeSymbol facetEnabledRefRWType,
            INamedTypeSymbol facetInterface)
        {
            var invocations = new List<QueryBuilderInvocation>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var visitedFacets = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default) { typeSymbol };

            foreach (var field in fields)
            {
                AddField(field);
            }

            return invocations;

            void AddInvocation(FacetField field)
            {
                var invocation = GetQueryBuilderInvocation(field);
                if (seen.Add(invocation))
                {
                    invocations.Add(new QueryBuilderInvocation(invocation, field.ComponentTypeSymbol));
                }
            }

            void AddField(FacetField field)
            {
                if (field.IsFacet)
                {
                    if (field.IsOptional)
                    {
                        return;
                    }

                    if (field.ComponentTypeSymbol is INamedTypeSymbol nestedFacetType)
                    {
                        AddFacetType(nestedFacetType);
                    }

                    return;
                }

                if (ShouldAddQueryBuilderInvocation(field))
                {
                    AddInvocation(field);
                }
            }

            void AddFacetType(INamedTypeSymbol facetType)
            {
                if (!visitedFacets.Add(facetType))
                {
                    return;
                }

                foreach (var fieldSymbol in facetType.GetMembers().OfType<IFieldSymbol>())
                {
                    if (fieldSymbol.IsStatic)
                    {
                        continue;
                    }

                    if (!TryCreateFacetField(
                        fieldSymbol,
                        optionalAttribute,
                        facetAttribute,
                        readOnlyAttribute,
                        singletonAttribute,
                        entityType,
                        entityStorageInfoType,
                        entityStorageInfoLookupType,
                        componentLookupType,
                        bufferLookupType,
                        bufferElementDataType,
                        facetEnabledRefRWType,
                        facetInterface,
                        null,
                        out var nestedField))
                    {
                        continue;
                    }

                    AddField(nestedField);
                }
            }
        }

        private static string CreateSingletonParameterName(IReadOnlyList<string> path, string fieldName)
        {
            if (path == null || path.Count == 0)
            {
                return Camelize(fieldName);
            }

            var name = path[0];

            for (var i = 1; i < path.Count; i++)
            {
                name += Pascalize(path[i]);
            }

            return Camelize($"{name}{Pascalize(fieldName)}");
        }

        private static string CreatePathKey(IReadOnlyList<string> path)
        {
            if (path == null || path.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(".", path);
        }

        private static bool TryCreateFacetField(
            IFieldSymbol fieldSymbol,
            INamedTypeSymbol optionalAttribute,
            INamedTypeSymbol facetAttribute,
            INamedTypeSymbol readOnlyAttribute,
            INamedTypeSymbol singletonAttribute,
            INamedTypeSymbol entityType,
            INamedTypeSymbol entityStorageInfoType,
            INamedTypeSymbol entityStorageInfoLookupType,
            INamedTypeSymbol componentLookupType,
            INamedTypeSymbol bufferLookupType,
            INamedTypeSymbol bufferElementDataType,
            INamedTypeSymbol facetEnabledRefRWType,
            INamedTypeSymbol facetInterface,
            IList<Diagnostic> diagnostics,
            out FacetField field)
        {
            field = null;

            var attributes = fieldSymbol.GetAttributes();

            var hasSingletonAttribute = HasAttribute(attributes, singletonAttribute);
            var hasFacetAttribute = HasAttribute(attributes, facetAttribute);
            var hasOptionalAttribute = HasAttribute(attributes, optionalAttribute);
            var hasReadOnlyAttribute = HasAttribute(attributes, readOnlyAttribute);

            if (hasSingletonAttribute && (hasFacetAttribute || hasOptionalAttribute))
            {
                diagnostics?.Add(FacetDiagnostics.SingletonAttributeConflict(fieldSymbol, fieldSymbol.Locations.FirstOrDefault()));
                return false;
            }

            var isOptional = !hasSingletonAttribute && hasOptionalAttribute;
            var isFacetField = !hasSingletonAttribute && hasFacetAttribute;

            if (hasSingletonAttribute)
            {
                if (fieldSymbol.Type is INamedTypeSymbol { Name: "DynamicBuffer", TypeArguments: { Length: 1 } } && !hasReadOnlyAttribute)
                {
                    diagnostics?.Add(FacetDiagnostics.ReadOnlySingletonBuffer(fieldSymbol, fieldSymbol.Locations.FirstOrDefault()));
                    return false;
                }

                field = new FacetField(fieldSymbol, fieldSymbol.Type, FacetFieldKind.Singleton, false, true, hasReadOnlyAttribute);
                return true;
            }

            if (isFacetField)
            {
                if (facetInterface != null &&
                    fieldSymbol.Type is INamedTypeSymbol { TypeKind: TypeKind.Struct } facetType &&
                    facetType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, facetInterface)))
                {
                    field = new FacetField(fieldSymbol, facetType, FacetFieldKind.Facet, isOptional, hasReadOnlyAttribute, hasReadOnlyAttribute);
                    return true;
                }

                diagnostics?.Add(FacetDiagnostics.InvalidFacetField(fieldSymbol, fieldSymbol.Locations.FirstOrDefault()));
                return false;
            }

            if (entityType != null && SymbolEqualityComparer.Default.Equals(fieldSymbol.Type, entityType))
            {
                field = new FacetField(fieldSymbol, fieldSymbol.Type, FacetFieldKind.Entity, isOptional, true, hasReadOnlyAttribute);
                return true;
            }

            if (entityStorageInfoType != null && SymbolEqualityComparer.Default.Equals(fieldSymbol.Type, entityStorageInfoType))
            {
                field = new FacetField(fieldSymbol, fieldSymbol.Type, FacetFieldKind.EntityStorageInfo, isOptional, true, hasReadOnlyAttribute);
                return true;
            }

            if (entityStorageInfoLookupType != null && SymbolEqualityComparer.Default.Equals(fieldSymbol.Type, entityStorageInfoLookupType))
            {
                field = new FacetField(fieldSymbol, fieldSymbol.Type, FacetFieldKind.EntityStorageInfoLookup, isOptional, true, hasReadOnlyAttribute);
                return true;
            }

            if (fieldSymbol.Type is not INamedTypeSymbol namedType || namedType.TypeArguments.Length != 1)
            {
                diagnostics?.Add(FacetDiagnostics.UnsupportedField(fieldSymbol, fieldSymbol.Locations.FirstOrDefault()));
                return false;
            }

            if (componentLookupType != null && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, componentLookupType))
            {
                field = new FacetField(fieldSymbol, namedType.TypeArguments[0], FacetFieldKind.ComponentLookup, isOptional, hasReadOnlyAttribute, hasReadOnlyAttribute);
                return true;
            }

            if (bufferLookupType != null && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, bufferLookupType))
            {
                field = new FacetField(fieldSymbol, namedType.TypeArguments[0], FacetFieldKind.BufferLookup, isOptional, hasReadOnlyAttribute, hasReadOnlyAttribute);
                return true;
            }

            if (facetEnabledRefRWType != null && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, facetEnabledRefRWType))
            {
                if (hasReadOnlyAttribute)
                {
                    diagnostics?.Add(FacetDiagnostics.ReadOnlyRefRW(fieldSymbol, fieldSymbol.Locations.FirstOrDefault(), "EnabledRefRO"));
                    return false;
                }

                var customComponentType = namedType.TypeArguments[0];
                var customIsBufferElement = ImplementsInterface(customComponentType, bufferElementDataType);
                field = new FacetField(
                    fieldSymbol,
                    customComponentType,
                    FacetFieldKind.FacetEnabledRefRW,
                    isOptional,
                    false,
                    false,
                    customIsBufferElement);
                return true;
            }

            FacetFieldKind kind;
            switch (namedType.Name)
            {
                case "RefRW" when hasReadOnlyAttribute:
                    diagnostics?.Add(FacetDiagnostics.ReadOnlyRefRW(fieldSymbol, fieldSymbol.Locations.FirstOrDefault(), "RefRO"));
                    return false;
                case "RefRW":
                    kind = FacetFieldKind.RefRW;
                    break;
                case "RefRO":
                    kind = FacetFieldKind.RefRO;
                    break;
                case "EnabledRefRO":
                    kind = FacetFieldKind.EnabledRefRO;
                    break;
                case "DynamicBuffer":
                    kind = FacetFieldKind.DynamicBuffer;
                    break;
                default:
                    diagnostics?.Add(FacetDiagnostics.UnsupportedField(fieldSymbol, fieldSymbol.Locations.FirstOrDefault()));
                    return false;
            }

            var componentType = namedType.TypeArguments[0];
            var isBufferElement = ImplementsInterface(componentType, bufferElementDataType);
            var isReadOnly =
                kind == FacetFieldKind.RefRO ||
                kind == FacetFieldKind.EnabledRefRO ||
                hasReadOnlyAttribute;

            field = new FacetField(fieldSymbol, componentType, kind, isOptional, isReadOnly, hasReadOnlyAttribute, isBufferElement);
            return true;
        }

        private static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol interfaceType)
        {
            return interfaceType != null &&
                   type is INamedTypeSymbol namedType &&
                   namedType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceType));
        }

        private static bool ShouldAddQueryBuilderInvocation(FacetField field)
        {
            return !field.IsOptional &&
                   !field.IsSingleton &&
                   !field.IsFacet &&
                   !field.IsEntity &&
                   !field.IsEntityStorageInfo &&
                   !field.IsEntityStorageInfoLookup &&
                   !field.IsComponentLookup &&
                   !field.IsBufferLookup;
        }

        private static string GetQueryBuilderInvocation(FacetField field)
        {
            return field.Kind switch
            {
                FacetFieldKind.RefRW => $"WithAllRW<{field.ComponentTypeName}>()",
                FacetFieldKind.RefRO => $"WithAll<{field.ComponentTypeName}>()",
                FacetFieldKind.FacetEnabledRefRW => $"WithAllRW<{field.ComponentTypeName}>()",
                FacetFieldKind.EnabledRefRO => $"WithAll<{field.ComponentTypeName}>()",
                FacetFieldKind.DynamicBuffer when field.IsReadOnly => $"WithAll<{field.ComponentTypeName}>()",
                FacetFieldKind.DynamicBuffer => $"WithAllRW<{field.ComponentTypeName}>()",
                _ => throw new ArgumentOutOfRangeException(nameof(field.Kind), field.Kind, null),
            };
        }

        private readonly struct FacetTraversalKey : IEquatable<FacetTraversalKey>
        {
            public FacetTraversalKey(INamedTypeSymbol facetType, string path)
            {
                this.FacetType = facetType;
                this.Path = path ?? string.Empty;
            }

            public INamedTypeSymbol FacetType { get; }

            public string Path { get; }

            public bool Equals(FacetTraversalKey other)
            {
                return SymbolEqualityComparer.Default.Equals(this.FacetType, other.FacetType) &&
                       string.Equals(this.Path, other.Path, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is FacetTraversalKey other && this.Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = SymbolEqualityComparer.Default.GetHashCode(this.FacetType);

                unchecked
                {
                    hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(this.Path);
                }

                return hash;
            }
        }

        private static bool HasAttribute(ImmutableArray<AttributeData> attributes, INamedTypeSymbol attribute)
        {
            if (attribute == null)
            {
                return false;
            }

            foreach (var attributeData in attributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attribute))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
