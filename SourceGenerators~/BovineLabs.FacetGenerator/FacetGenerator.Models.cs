// <copyright file="FacetGenerator.Models.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.FacetGenerator
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal enum FacetFieldKind
    {
        RefRW,
        RefRO,
        FacetEnabledRefRW,
        EnabledRefRO,
        DynamicBuffer,
        Entity,
        EntityStorageInfo,
        EntityStorageInfoLookup,
        ComponentLookup,
        BufferLookup,
        Singleton,
        Facet,
    }

    internal sealed class FacetResult
    {
        public FacetResult(FacetData data, IReadOnlyList<Diagnostic> diagnostics)
        {
            this.Data = data;
            this.Diagnostics = diagnostics ?? Array.Empty<Diagnostic>();
        }

        public FacetData Data { get; }

        public IReadOnlyList<Diagnostic> Diagnostics { get; }
    }

    internal sealed class FacetCandidate
    {
        public FacetCandidate(TypeDeclarationSyntax typeSyntax, INamedTypeSymbol typeSymbol)
        {
            this.TypeSyntax = typeSyntax;
            this.TypeSymbol = typeSymbol;
        }

        public TypeDeclarationSyntax TypeSyntax { get; }

        public INamedTypeSymbol TypeSymbol { get; }
    }

    internal sealed class FacetSymbols
    {
        private FacetSymbols(
            INamedTypeSymbol facetInterface,
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
            INamedTypeSymbol facetEnabledRefRWType)
        {
            this.FacetInterface = facetInterface;
            this.OptionalAttribute = optionalAttribute;
            this.FacetAttribute = facetAttribute;
            this.ReadOnlyAttribute = readOnlyAttribute;
            this.SingletonAttribute = singletonAttribute;
            this.EntityType = entityType;
            this.EntityStorageInfoType = entityStorageInfoType;
            this.EntityStorageInfoLookupType = entityStorageInfoLookupType;
            this.ComponentLookupType = componentLookupType;
            this.BufferLookupType = bufferLookupType;
            this.BufferElementDataType = bufferElementDataType;
            this.FacetEnabledRefRWType = facetEnabledRefRWType;
        }

        public INamedTypeSymbol FacetInterface { get; }

        public INamedTypeSymbol OptionalAttribute { get; }

        public INamedTypeSymbol FacetAttribute { get; }

        public INamedTypeSymbol ReadOnlyAttribute { get; }

        public INamedTypeSymbol SingletonAttribute { get; }

        public INamedTypeSymbol EntityType { get; }

        public INamedTypeSymbol EntityStorageInfoType { get; }

        public INamedTypeSymbol EntityStorageInfoLookupType { get; }

        public INamedTypeSymbol ComponentLookupType { get; }

        public INamedTypeSymbol BufferLookupType { get; }

        public INamedTypeSymbol BufferElementDataType { get; }

        public INamedTypeSymbol FacetEnabledRefRWType { get; }

        public static FacetSymbols Create(Compilation compilation)
        {
            return new FacetSymbols(
                compilation.GetTypeByMetadataName("BovineLabs.Core.IFacet"),
                compilation.GetTypeByMetadataName("BovineLabs.Core.FacetOptionalAttribute"),
                compilation.GetTypeByMetadataName("BovineLabs.Core.FacetAttribute"),
                compilation.GetTypeByMetadataName("Unity.Collections.ReadOnlyAttribute"),
                compilation.GetTypeByMetadataName("BovineLabs.Core.SingletonAttribute"),
                compilation.GetTypeByMetadataName("Unity.Entities.Entity"),
                compilation.GetTypeByMetadataName("Unity.Entities.EntityStorageInfo"),
                compilation.GetTypeByMetadataName("Unity.Entities.EntityStorageInfoLookup"),
                compilation.GetTypeByMetadataName("Unity.Entities.ComponentLookup`1"),
                compilation.GetTypeByMetadataName("Unity.Entities.BufferLookup`1"),
                compilation.GetTypeByMetadataName("Unity.Entities.IBufferElementData"),
                compilation.GetTypeByMetadataName("BovineLabs.Core.FacetEnabledRefRW`1"));
        }
    }

    internal sealed class FacetData
    {
        public FacetData(
            INamedTypeSymbol typeSymbol,
            IReadOnlyList<FacetField> fields,
            IReadOnlyList<FacetSingletonDependency> singletonDependencies,
            IReadOnlyList<QueryBuilderInvocation> queryBuilderInvocations)
        {
            this.TypeSymbol = typeSymbol;
            this.Fields = fields;
            this.SingletonDependencies = singletonDependencies;
            this.QueryBuilderInvocations = queryBuilderInvocations;
            this.TypeName = typeSymbol.ToDisplayString(FacetGenerator.ShortTypeFormat);
        }

        public INamedTypeSymbol TypeSymbol { get; }

        public IReadOnlyList<FacetField> Fields { get; }

        public IReadOnlyList<FacetSingletonDependency> SingletonDependencies { get; }

        public IReadOnlyList<QueryBuilderInvocation> QueryBuilderInvocations { get; }

        public string TypeName { get; }
    }

    internal sealed class QueryBuilderInvocation
    {
        public QueryBuilderInvocation(string invocation, ITypeSymbol componentTypeSymbol)
        {
            this.Invocation = invocation;
            this.ComponentTypeSymbol = componentTypeSymbol;
        }

        public string Invocation { get; }

        public ITypeSymbol ComponentTypeSymbol { get; }
    }

    internal sealed class FacetSingletonDependency
    {
        public FacetSingletonDependency(string parameterName, FacetField field)
        {
            this.ParameterName = parameterName;
            this.Field = field;
        }

        public string ParameterName { get; }

        public FacetField Field { get; }
    }

    internal sealed class FacetField
    {
        public FacetField(
            IFieldSymbol symbol,
            ITypeSymbol componentType,
            FacetFieldKind kind,
            bool isOptional,
            bool isReadOnly,
            bool hasReadOnlyAttribute,
            bool isBufferElement = false)
        {
            this.Symbol = symbol;
            this.ComponentTypeSymbol = componentType;
            this.Kind = kind;
            this.IsOptional = isOptional;
            this.IsReadOnly = isReadOnly;
            this.HasReadOnlyAttribute = hasReadOnlyAttribute;
            this.IsBufferElement = isBufferElement;
            this.FieldTypeName = symbol.Type.ToDisplayString(FacetGenerator.ShortTypeFormat);
            this.ComponentTypeName = componentType.ToDisplayString(FacetGenerator.ShortTypeFormat);
            this.ArgumentName = this.FieldName is "entity" or "facet" ? $"{this.FieldName}Value" : this.FieldName;
        }

        public IFieldSymbol Symbol { get; }

        public ITypeSymbol ComponentTypeSymbol { get; }

        public FacetFieldKind Kind { get; }

        public bool IsOptional { get; }

        public bool IsReadOnly { get; }

        public bool HasReadOnlyAttribute { get; }

        public bool IsBufferElement { get; }

        public bool IsEntity => this.Kind == FacetFieldKind.Entity;

        public bool IsEntityStorageInfo => this.Kind == FacetFieldKind.EntityStorageInfo;

        public bool IsEntityStorageInfoLookup => this.Kind == FacetFieldKind.EntityStorageInfoLookup;

        public bool IsComponentLookup => this.Kind == FacetFieldKind.ComponentLookup;

        public bool IsBufferLookup => this.Kind == FacetFieldKind.BufferLookup;

        public bool IsSingleton => this.Kind == FacetFieldKind.Singleton;

        public bool IsBuffer => this.Kind == FacetFieldKind.DynamicBuffer;

        public bool UsesBufferStorage => this.IsBuffer || this.IsBufferElement;

        public bool IsEnabled =>
            this.Kind == FacetFieldKind.FacetEnabledRefRW ||
            this.Kind == FacetFieldKind.EnabledRefRO;

        public bool IsFacet => this.Kind == FacetFieldKind.Facet;

        public IReadOnlyList<FacetSingletonDependency> FacetSingletonDependencies { get; private set; } = Array.Empty<FacetSingletonDependency>();

        public string FieldName => this.Symbol.Name;

        public string ArgumentName { get; }

        public string FieldTypeName { get; }

        public string ComponentTypeName { get; }

        public string LookupFieldName
        {
            get
            {
                if (this.IsSingleton || this.IsFacet || this.IsEntityStorageInfo || this.IsEntityStorageInfoLookup || this.IsComponentLookup || this.IsBufferLookup)
                {
                    return this.PascalFieldName;
                }

                if (this.IsEntity)
                {
                    return "Entities";
                }

                return Pluralize(this.ComponentTypeSymbol.Name);
            }
        }

        public string ResolvedFieldName
        {
            get
            {
                if (!string.IsNullOrEmpty(this.resolvedFieldNameOverride))
                {
                    return this.resolvedFieldNameOverride;
                }

                if (this.IsSingleton || this.IsFacet || this.IsEntityStorageInfo || this.IsEntityStorageInfoLookup || this.IsComponentLookup || this.IsBufferLookup)
                {
                    return this.PascalFieldName;
                }

                if (this.IsEntity)
                {
                    return "Entities";
                }

                return Pluralize(this.ComponentTypeSymbol.Name);
            }
        }

        public string HandleName
        {
            get
            {
                if (this.IsSingleton)
                {
                    return this.PascalFieldName;
                }

                if (this.IsFacet || this.IsEntityStorageInfo || this.IsEntityStorageInfoLookup || this.IsComponentLookup || this.IsBufferLookup)
                {
                    return $"{this.PascalFieldName}Handle";
                }

                return $"{this.ComponentTypeSymbol.Name}Handle";
            }
        }

        public string LookupTypeName => this.IsSingleton
            ? this.FieldTypeName
            : this.IsFacet
                ? $"{this.ComponentTypeName}.Lookup"
                : this.IsEntityStorageInfo || this.IsEntityStorageInfoLookup
                    ? "EntityStorageInfoLookup"
                    : this.IsComponentLookup || this.IsBufferLookup
                        ? this.FieldTypeName
                    : this.IsEntity
                        ? this.ComponentTypeName
                        : this.UsesBufferStorage
                            ? $"BufferLookup<{this.ComponentTypeName}>"
                            : $"ComponentLookup<{this.ComponentTypeName}>";

        public void SetFacetSingletonDependencies(IReadOnlyList<FacetSingletonDependency> dependencies)
        {
            this.FacetSingletonDependencies = dependencies ?? Array.Empty<FacetSingletonDependency>();
        }

        public void SetResolvedFieldNameOverride(string resolvedFieldName)
        {
            this.resolvedFieldNameOverride = resolvedFieldName;
        }

        public string ResolvedFieldTypeName
        {
            get
            {
                if (this.IsSingleton)
                {
                    return this.FieldTypeName;
                }

                if (this.IsFacet)
                {
                    return $"{this.ComponentTypeName}.ResolvedChunk";
                }

                if (this.IsBuffer)
                {
                    return $"BufferAccessor<{this.ComponentTypeName}>";
                }

                if (this.IsEntity)
                {
                    return "NativeArray<Entity>";
                }

                if (this.IsEntityStorageInfo)
                {
                    return "ArchetypeChunk";
                }

                if (this.IsEntityStorageInfoLookup)
                {
                    return "EntityStorageInfoLookup";
                }

                if (this.IsComponentLookup || this.IsBufferLookup)
                {
                    return this.FieldTypeName;
                }

                if (this.IsEnabled)
                {
                    return "EnabledMask";
                }

                return $"NativeArray<{this.ComponentTypeName}>";
            }
        }

        public string HandleTypeName => this.IsSingleton
            ? this.FieldTypeName
            : this.IsFacet
                ? $"{this.ComponentTypeName}.TypeHandle"
                : this.IsEntityStorageInfo || this.IsEntityStorageInfoLookup
                    ? "EntityStorageInfoLookup"
                    : this.IsComponentLookup || this.IsBufferLookup
                        ? this.FieldTypeName
                    : this.IsEntity
                        ? "EntityTypeHandle"
                        : this.UsesBufferStorage
                            ? $"BufferTypeHandle<{this.ComponentTypeName}>"
                            : $"ComponentTypeHandle<{this.ComponentTypeName}>";

        private static string Pluralize(string name)
        {
            return name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? name : $"{name}s";
        }

        private string resolvedFieldNameOverride;

        private string PascalFieldName => $"{char.ToUpper(this.FieldName[0], System.Globalization.CultureInfo.InvariantCulture)}{this.FieldName.Substring(1)}";
    }
}
