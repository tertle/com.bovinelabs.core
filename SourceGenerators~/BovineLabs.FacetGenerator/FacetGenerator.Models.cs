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
            Data = data;
            Diagnostics = diagnostics ?? Array.Empty<Diagnostic>();
        }

        public FacetData Data { get; }

        public IReadOnlyList<Diagnostic> Diagnostics { get; }
    }

    internal sealed class FacetCandidate
    {
        public FacetCandidate(TypeDeclarationSyntax typeSyntax, INamedTypeSymbol typeSymbol)
        {
            TypeSyntax = typeSyntax;
            TypeSymbol = typeSymbol;
        }

        public TypeDeclarationSyntax TypeSyntax { get; }

        public INamedTypeSymbol TypeSymbol { get; }
    }

    internal sealed class FacetSymbols
    {
        private FacetSymbols(
            INamedTypeSymbol facetInterface, INamedTypeSymbol optionalAttribute, INamedTypeSymbol facetAttribute, INamedTypeSymbol readOnlyAttribute,
            INamedTypeSymbol singletonAttribute, INamedTypeSymbol entityType, INamedTypeSymbol entityStorageInfoType,
            INamedTypeSymbol entityStorageInfoLookupType, INamedTypeSymbol componentLookupType, INamedTypeSymbol bufferLookupType,
            INamedTypeSymbol bufferElementDataType, INamedTypeSymbol facetEnabledRefRWType)
        {
            FacetInterface = facetInterface;
            OptionalAttribute = optionalAttribute;
            FacetAttribute = facetAttribute;
            ReadOnlyAttribute = readOnlyAttribute;
            SingletonAttribute = singletonAttribute;
            EntityType = entityType;
            EntityStorageInfoType = entityStorageInfoType;
            EntityStorageInfoLookupType = entityStorageInfoLookupType;
            ComponentLookupType = componentLookupType;
            BufferLookupType = bufferLookupType;
            BufferElementDataType = bufferElementDataType;
            FacetEnabledRefRWType = facetEnabledRefRWType;
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
            INamedTypeSymbol typeSymbol, IReadOnlyList<FacetField> fields, IReadOnlyList<FacetSingletonDependency> singletonDependencies,
            IReadOnlyList<QueryBuilderInvocation> queryBuilderInvocations)
        {
            TypeSymbol = typeSymbol;
            Fields = fields;
            SingletonDependencies = singletonDependencies;
            QueryBuilderInvocations = queryBuilderInvocations;
            TypeName = typeSymbol.ToDisplayString(FacetGenerator.ShortTypeFormat);
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
            Invocation = invocation;
            ComponentTypeSymbol = componentTypeSymbol;
        }

        public string Invocation { get; }

        public ITypeSymbol ComponentTypeSymbol { get; }
    }

    internal sealed class FacetSingletonDependency
    {
        public FacetSingletonDependency(string parameterName, FacetField field)
        {
            ParameterName = parameterName;
            Field = field;
        }

        public string ParameterName { get; }

        public FacetField Field { get; }
    }

    internal sealed class FacetField
    {
        public FacetField(
            IFieldSymbol symbol, ITypeSymbol componentType, FacetFieldKind kind, bool isOptional, bool isReadOnly, bool hasReadOnlyAttribute,
            bool isBufferElement = false)
        {
            Symbol = symbol;
            ComponentTypeSymbol = componentType;
            Kind = kind;
            IsOptional = isOptional;
            IsReadOnly = isReadOnly;
            HasReadOnlyAttribute = hasReadOnlyAttribute;
            IsBufferElement = isBufferElement;
            FieldTypeName = symbol.Type.ToDisplayString(FacetGenerator.ShortTypeFormat);
            ComponentTypeName = componentType.ToDisplayString(FacetGenerator.ShortTypeFormat);
            ArgumentName = FieldName is "entity" or "facet" ? $"{FieldName}Value" : FieldName;
        }

        public IFieldSymbol Symbol { get; }

        public ITypeSymbol ComponentTypeSymbol { get; }

        public FacetFieldKind Kind { get; }

        public bool IsOptional { get; }

        public bool IsReadOnly { get; }

        public bool HasReadOnlyAttribute { get; }

        public bool IsBufferElement { get; }

        public bool IsEntity => Kind == FacetFieldKind.Entity;

        public bool IsEntityStorageInfo => Kind == FacetFieldKind.EntityStorageInfo;

        public bool IsEntityStorageInfoLookup => Kind == FacetFieldKind.EntityStorageInfoLookup;

        public bool IsComponentLookup => Kind == FacetFieldKind.ComponentLookup;

        public bool IsBufferLookup => Kind == FacetFieldKind.BufferLookup;

        public bool IsSingleton => Kind == FacetFieldKind.Singleton;

        public bool IsBuffer => Kind == FacetFieldKind.DynamicBuffer;

        public bool UsesBufferStorage => IsBuffer || IsBufferElement;

        public bool IsEnabled =>
            Kind == FacetFieldKind.FacetEnabledRefRW ||
            Kind == FacetFieldKind.EnabledRefRO;

        public bool IsFacet => Kind == FacetFieldKind.Facet;

        public IReadOnlyList<FacetSingletonDependency> FacetSingletonDependencies { get; private set; } = Array.Empty<FacetSingletonDependency>();

        public string FieldName => Symbol.Name;

        public string ParameterName => FieldName.Length > 1 && FieldName[0] == '_' ? FieldName.Substring(1) : FieldName;

        public string ArgumentName { get; }

        public string FieldTypeName { get; }

        public string ComponentTypeName { get; }

        public string LookupFieldName
        {
            get
            {
                if (IsSingleton || IsFacet || IsEntityStorageInfo || IsEntityStorageInfoLookup || IsComponentLookup || IsBufferLookup)
                {
                    return PascalFieldName;
                }

                if (IsEntity)
                {
                    return "Entities";
                }

                return Pluralize(ComponentTypeSymbol.Name);
            }
        }

        public string ResolvedFieldName
        {
            get
            {
                if (!string.IsNullOrEmpty(_resolvedFieldNameOverride))
                {
                    return _resolvedFieldNameOverride;
                }

                if (IsSingleton || IsFacet || IsEntityStorageInfo || IsEntityStorageInfoLookup || IsComponentLookup || IsBufferLookup)
                {
                    return PascalFieldName;
                }

                if (IsEntity)
                {
                    return "Entities";
                }

                return Pluralize(ComponentTypeSymbol.Name);
            }
        }

        public string HandleName
        {
            get
            {
                if (IsSingleton)
                {
                    return PascalFieldName;
                }

                if (IsFacet || IsEntityStorageInfo || IsEntityStorageInfoLookup || IsComponentLookup || IsBufferLookup)
                {
                    return $"{PascalFieldName}Handle";
                }

                return $"{ComponentTypeSymbol.Name}Handle";
            }
        }

        public string LookupTypeName => IsSingleton
            ? FieldTypeName
            : IsFacet
                ? $"{ComponentTypeName}.Lookup"
                : IsEntityStorageInfo || IsEntityStorageInfoLookup
                    ? "EntityStorageInfoLookup"
                    : IsComponentLookup || IsBufferLookup
                        ? FieldTypeName
                    : IsEntity
                        ? ComponentTypeName
                        : UsesBufferStorage
                            ? $"BufferLookup<{ComponentTypeName}>"
                            : $"ComponentLookup<{ComponentTypeName}>";

        public void SetFacetSingletonDependencies(IReadOnlyList<FacetSingletonDependency> dependencies)
        {
            FacetSingletonDependencies = dependencies ?? Array.Empty<FacetSingletonDependency>();
        }

        public void SetResolvedFieldNameOverride(string resolvedFieldName)
        {
            _resolvedFieldNameOverride = resolvedFieldName;
        }

        public string ResolvedFieldTypeName
        {
            get
            {
                if (IsSingleton)
                {
                    return FieldTypeName;
                }

                if (IsFacet)
                {
                    return $"{ComponentTypeName}.ResolvedChunk";
                }

                if (IsBuffer)
                {
                    return $"BufferAccessor<{ComponentTypeName}>";
                }

                if (IsEntity)
                {
                    return "NativeArray<Entity>";
                }

                if (IsEntityStorageInfo)
                {
                    return "ArchetypeChunk";
                }

                if (IsEntityStorageInfoLookup)
                {
                    return "EntityStorageInfoLookup";
                }

                if (IsComponentLookup || IsBufferLookup)
                {
                    return FieldTypeName;
                }

                if (IsEnabled)
                {
                    return "EnabledMask";
                }

                return $"NativeArray<{ComponentTypeName}>";
            }
        }

        public string HandleTypeName => IsSingleton
            ? FieldTypeName
            : IsFacet
                ? $"{ComponentTypeName}.TypeHandle"
                : IsEntityStorageInfo || IsEntityStorageInfoLookup
                    ? "EntityStorageInfoLookup"
                    : IsComponentLookup || IsBufferLookup
                        ? FieldTypeName
                    : IsEntity
                        ? "EntityTypeHandle"
                        : UsesBufferStorage
                            ? $"BufferTypeHandle<{ComponentTypeName}>"
                            : $"ComponentTypeHandle<{ComponentTypeName}>";

        private static string Pluralize(string name)
        {
            return name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? name : $"{name}s";
        }

        private string _resolvedFieldNameOverride;

        private string PascalFieldName =>
            $"{char.ToUpper(ParameterName[0], System.Globalization.CultureInfo.InvariantCulture)}{ParameterName.Substring(1)}";
    }
}
