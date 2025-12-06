// <copyright file="FacetGenerator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.FacetGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using CodeGenHelpers;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal enum FacetFieldKind
    {
        RefRW,
        RefRO,
        EnabledRefRW,
        EnabledRefRO,
        DynamicBuffer,
        Entity,
        Singleton,
        Facet,
    }

    [Generator]
    public class FacetGenerator : IIncrementalGenerator
    {
        internal static readonly SymbolDisplayFormat ShortTypeFormat =
            SymbolDisplayFormat.MinimallyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var facetSymbols = context.CompilationProvider.Select(static (compilation, _) => FacetSymbols.Create(compilation));

            var candidates = context.SyntaxProvider
                .CreateSyntaxProvider(predicate: IsSyntaxTargetForGeneration, transform: GetFacetCandidate)
                .Where(c => c != null);

            var inputs = candidates.Combine(facetSymbols)
                .Select((pair, cancellationToken) => GetSemanticTargetForGeneration(pair.Left, pair.Right, cancellationToken))
                .Where(r => r != null);

            context.RegisterSourceOutput(inputs, static (ctx, result) => Execute(ctx, result));
        }

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

            var fields = new List<FacetField>();
            foreach (var fieldSymbol in typeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (fieldSymbol.IsStatic)
                {
                    continue;
                }

                if (TryCreateFacetField(fieldSymbol, optionalAttribute, facetAttribute, readOnlyAttribute, singletonAttribute, entityType, facetInterface, diagnostics, out var field))
                {
                    fields.Add(field);
                }
            }

            if (fields.Count == 0)
            {
                diagnostics.Add(FacetDiagnostics.NoFields(typeSymbol, typeSyntax.Identifier.GetLocation()));
            }

            var hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
            IReadOnlyList<FacetSingletonDependency> singletonDependencies = Array.Empty<FacetSingletonDependency>();

            if (!hasErrors && fields.Count > 0)
            {
                var singletonCache = new Dictionary<FacetTraversalKey, IReadOnlyList<FacetSingletonDependency>>();
                singletonDependencies = CollectSingletonDependencies(typeSymbol, fields, facetAttribute, singletonAttribute, facetInterface, singletonCache);
            }

            var data = !hasErrors && fields.Count > 0 ? new FacetData(typeSymbol, fields, singletonDependencies) : null;

            return new FacetResult(data, diagnostics);
        }

        private static IReadOnlyList<FacetSingletonDependency> CollectSingletonDependencies(
            INamedTypeSymbol typeSymbol,
            IReadOnlyList<FacetField> fields,
            INamedTypeSymbol facetAttribute,
            INamedTypeSymbol singletonAttribute,
            INamedTypeSymbol facetInterface,
            IDictionary<FacetTraversalKey, IReadOnlyList<FacetSingletonDependency>> singletonCache)
        {
            var dependencies = new List<FacetSingletonDependency>();

            foreach (var field in fields)
            {
                if (field.IsSingleton)
                {
                    dependencies.Add(new FacetSingletonDependency(field.FieldName, field));
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
                    var singletonField = new FacetField(fieldSymbol, fieldSymbol.Type, FacetFieldKind.Singleton, false, true);
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

        private static string CreateSingletonParameterName(IReadOnlyList<string> path, string fieldName)
        {
            if (path == null || path.Count == 0)
            {
                return fieldName;
            }

            var name = path[0];

            for (var i = 1; i < path.Count; i++)
            {
                name += Pascalize(path[i]);
            }

            return $"{name}{Pascalize(fieldName)}";
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
            INamedTypeSymbol facetInterface,
            IList<Diagnostic> diagnostics,
            out FacetField field)
        {
            field = null;

            var attributes = fieldSymbol.GetAttributes();

            var hasSingletonAttribute = HasAttribute(attributes, singletonAttribute);
            var hasFacetAttribute = !hasSingletonAttribute && HasAttribute(attributes, facetAttribute);
            var isOptional = !hasSingletonAttribute && HasAttribute(attributes, optionalAttribute);
            var hasReadOnlyAttribute = HasAttribute(attributes, readOnlyAttribute);

            if (hasSingletonAttribute)
            {
                field = new FacetField(fieldSymbol, fieldSymbol.Type, FacetFieldKind.Singleton, false, true);
                return true;
            }

            if (hasFacetAttribute)
            {
                if (facetInterface != null &&
                    fieldSymbol.Type is INamedTypeSymbol { TypeKind: TypeKind.Struct } facetType &&
                    facetType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, facetInterface)))
                {
                    field = new FacetField(fieldSymbol, facetType, FacetFieldKind.Facet, isOptional, hasReadOnlyAttribute || fieldSymbol.IsReadOnly);
                    return true;
                }

                diagnostics?.Add(FacetDiagnostics.InvalidFacetField(fieldSymbol, fieldSymbol.Locations.FirstOrDefault()));
                return false;
            }

            if (entityType != null && SymbolEqualityComparer.Default.Equals(fieldSymbol.Type, entityType))
            {
                field = new FacetField(fieldSymbol, fieldSymbol.Type, FacetFieldKind.Entity, isOptional, true);
                return true;
            }

            if (fieldSymbol.Type is not INamedTypeSymbol namedType || namedType.TypeArguments.Length != 1)
            {
                diagnostics?.Add(FacetDiagnostics.UnsupportedField(fieldSymbol, fieldSymbol.Locations.FirstOrDefault()));
                return false;
            }

            FacetFieldKind kind;
            switch (namedType.Name)
            {
                case "RefRW" when hasReadOnlyAttribute:
                    diagnostics?.Add(FacetDiagnostics.ReadOnlyRefRW(fieldSymbol, fieldSymbol.Locations.FirstOrDefault()));
                    return false;
                case "RefRW":
                    kind = FacetFieldKind.RefRW;
                    break;
                case "RefRO":
                    kind = FacetFieldKind.RefRO;
                    break;
                case "EnabledRefRW" when hasReadOnlyAttribute:
                    diagnostics?.Add(FacetDiagnostics.ReadOnlyRefRW(fieldSymbol, fieldSymbol.Locations.FirstOrDefault()));
                    return false;
                case "EnabledRefRW":
                    kind = FacetFieldKind.EnabledRefRW;
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
            var isReadOnly =
                kind == FacetFieldKind.RefRO ||
                kind == FacetFieldKind.EnabledRefRO ||
                hasReadOnlyAttribute && kind != FacetFieldKind.EnabledRefRW;

            field = new FacetField(fieldSymbol, componentType, kind, isOptional, isReadOnly);
            return true;
        }

        private static void Execute(SourceProductionContext context, FacetResult result)
        {
            try
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }

                var data = result.Data;
                if (data == null)
                {
                    return;
                }

                var builder = Generate(data);
                if (builder == null)
                {
                    return;
                }

                var source = builder.Build();

                var hintName = $"{data.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)).Replace('<', '[').Replace('>', ']')}.IFacet.g.cs";
                context.AddSource(hintName, source);
            }
            catch (Exception ex)
            {
                SourceGenHelpers.Log(ex.ToString());
            }
        }

        private static CodeBuilder Generate(FacetData data)
        {
            var builder = CodeBuilder
                .Create(data.TypeSymbol.ContainingNamespace.ToDisplayString())
                .AddNamespaceImport("Unity.Collections")
                .AddNamespaceImport("Unity.Entities");

            var namespaces = new HashSet<string>(StringComparer.Ordinal);

            foreach (var field in data.Fields)
            {
                if (field.ComponentTypeSymbol.ContainingNamespace.IsGlobalNamespace)
                {
                    continue;
                }

                var namespaceName = field.ComponentTypeSymbol.ContainingNamespace.ToDisplayString();
                if (namespaces.Add(namespaceName))
                {
                    builder.AddNamespaceImport(namespaceName);
                }
            }

            var typeBuilder = builder
                .AddClass(data.TypeSymbol.Name)
                .WithAccessModifier(data.TypeSymbol.DeclaredAccessibility)
                .OfType(TypeKind.Struct)
                .ReadOnly(data.TypeSymbol.IsReadOnly);

            typeBuilder.WithSummary($"Facet helpers generated for {data.TypeName}.");
            AddConstructor(typeBuilder, data);
            AddLookup(typeBuilder, data);
            AddResolvedChunk(typeBuilder, data);
            AddTypeHandle(typeBuilder, data);
            AddCreateQueryBuilder(typeBuilder, data);

            return builder;
        }

        private static void AddConstructor(ClassBuilder typeBuilder, FacetData data)
        {
            var ctor = typeBuilder.AddConstructor(Accessibility.Public)
                .WithSummary($"Initializes a new instance of {data.TypeName}.");
            foreach (var field in data.Fields)
            {
                ctor.AddParameter(field.FieldTypeName, field.FieldName);
            }

            ctor.WithBody(body =>
            {
                foreach (var field in data.Fields)
                {
                    body.AppendLine($"this.{field.FieldName} = {field.FieldName};");
                }
            });
        }

        private static void AddCreateQueryBuilder(ClassBuilder typeBuilder, FacetData data)
        {
            var queries = data.Fields
                .Where(f => !f.IsOptional && !f.IsSingleton && !f.IsFacet && !f.IsEntity)
                .Select(GetQueryBuilderInvocation)
                .ToArray();

            var method = typeBuilder
                .AddMethod("CreateQueryBuilder", Accessibility.Public)
                .MakeStaticMethod()
                .WithReturnType("EntityQueryBuilder")
                .WithSummary($"Creates an EntityQueryBuilder requesting the required components for {data.TypeName}.")
                .WithParameterDoc("allocator", "Allocator used for the query builder.");

            method.AddParameterWithDefaultValue("Allocator", "allocator", "Allocator.Temp");

            method.WithBody(body =>
            {
                var chain = queries.Length == 0
                    ? string.Empty
                    : string.Concat(queries.Select(q => $".{q}"));

                body.AppendLine($"return new EntityQueryBuilder(allocator){chain};");
            });
        }

        private static void AddLookup(ClassBuilder typeBuilder, FacetData data)
        {
            var lookup = typeBuilder.AddNestedClass("Lookup", true, Accessibility.Public)
                .IsStruct()
                .WithSummary($"Provides entity-level access to {data.TypeName}.");

            var lookupFields = data.Fields.Where(f => !f.IsSingleton && !f.IsEntity).ToArray();
            var singletonFields = data.Fields.Where(f => f.IsSingleton).ToArray();
            var singletonDependencies = data.SingletonDependencies;

            foreach (var field in lookupFields)
            {
                var lookupField = lookup.AddProperty(field.LookupFieldName, Accessibility.Public).SetType(field.LookupTypeName);

                if (field.IsReadOnly)
                {
                    lookupField.AddAttribute("ReadOnly");
                }
            }

            foreach (var field in singletonFields)
            {
                var singletonField = lookup
                    .AddProperty(field.LookupFieldName, Accessibility.Public)
                    .SetType(field.FieldTypeName);

                singletonField.AddAttribute("ReadOnly");
            }

            var indexer = lookup
                .AddProperty($"this[Entity entity]", Accessibility.Public)
                .SetType(data.TypeName)
                .WithSummary($"Gets the {data.TypeName} for the specified entity.");

            indexer.WithGetter(getter =>
            {
                foreach (var field in data.Fields)
                {
                    WriteLookupAcquisition(getter, field, false);
                }

                getter.AppendLine($"return new {data.TypeName}({string.Join(", ", data.Fields.Select(f => f.ArgumentName))});");
            });

            var tryGet = lookup.AddMethod("TryGet", Accessibility.Public).WithReturnType("bool");
            tryGet.AddParameter("Entity", "entity");
            tryGet.AddParameter($"out {data.TypeName}", "facet");
            tryGet.WithSummary($"Attempts to retrieve {data.TypeName} for an entity.")
                .WithParameterDoc("entity", "The entity to read.")
                .WithParameterDoc("facet", "The resolved facet when the entity has the required components.");

            tryGet.WithBody(body =>
            {
                body.AppendLine("facet = default;");
                body.NewLine();

                foreach (var field in data.Fields)
                {
                    WriteLookupAcquisition(body, field, true);
                }

                body.AppendLine($"facet = new {data.TypeName}({string.Join(", ", data.Fields.Select(f => f.ArgumentName))});");
                body.AppendLine("return true;");
            });

            var create = lookup.AddMethod("Create", Accessibility.Public).WithReturnType("void");
            create.AddParameter("ref SystemState", "state");
            create.WithSummary($"Initializes lookups used by {data.TypeName}.")
                .WithParameterDoc("state", "System state providing lookup handles.");
            create.WithBody(body =>
            {
                foreach (var field in lookupFields)
                {
                    if (field.IsFacet)
                    {
                        body.AppendLine($"this.{field.LookupFieldName}.Create(ref state);");
                        continue;
                    }

                    var lookupExpression = field.IsBuffer
                        ? $"state.GetBufferLookup<{field.ComponentTypeName}>({(field.IsReadOnly ? "true" : string.Empty)})"
                        : $"state.GetComponentLookup<{field.ComponentTypeName}>({(field.IsReadOnly ? "true" : string.Empty)})";

                    body.AppendLine($"this.{field.LookupFieldName} = {lookupExpression};");
                }
            });

            var update = lookup.AddMethod("Update", Accessibility.Public).WithReturnType("void");
            update.AddParameter("ref SystemState", "state");
            foreach (var dependency in singletonDependencies)
            {
                update.AddParameter($"in {dependency.Field.ComponentTypeName}", dependency.ParameterName);
            }
            update.WithSummary($"Refreshes lookups for {data.TypeName} and updates singleton caches.")
                .WithParameterDoc("state", "System state used to update handles.");
            foreach (var dependency in singletonDependencies)
            {
                update.WithParameterDoc(dependency.ParameterName, GetSingletonRetrievalDoc(dependency.Field));
            }

            update.WithBody(body =>
            {
                foreach (var field in lookupFields)
                {
                    if (field.IsFacet)
                    {
                        body.AppendLine($"this.{field.LookupFieldName}.Update(ref state{GetFacetSingletonArguments(field)});");
                    }
                    else
                    {
                        body.AppendLine($"this.{field.LookupFieldName}.Update(ref state);");
                    }
                }

                foreach (var field in singletonFields)
                {
                    body.AppendLine($"this.{field.LookupFieldName} = {field.FieldName};");
                }
            });
        }

        private static void AddResolvedChunk(ClassBuilder typeBuilder, FacetData data)
        {
            var resolvedChunk = typeBuilder.AddNestedClass("ResolvedChunk", false, Accessibility.Public)
                .IsStruct()
                .WithSummary($"Chunk-level accessors for {data.TypeName}.");

            foreach (var field in data.Fields)
            {
                resolvedChunk.AddProperty(field.ResolvedFieldName, Accessibility.Public).SetType(field.ResolvedFieldTypeName);
            }

            var resolvedIndexer = resolvedChunk
                .AddProperty("this[int index]", Accessibility.Public)
                .SetType(data.TypeName)
                .WithSummary($"Gets the {data.TypeName} for an entity in the chunk by index.");

            var arguments = data.Fields.Select(GetResolvedArgument).ToArray();

            resolvedIndexer.WithGetterExpression($"new {data.TypeName}({string.Join(", ", arguments)})");
        }

        private static void AddTypeHandle(ClassBuilder typeBuilder, FacetData data)
        {
            var typeHandle = typeBuilder.AddNestedClass("TypeHandle", true, Accessibility.Public)
                .IsStruct()
                .WithSummary($"Maintains type handles for chunk access to {data.TypeName}.");

            var typeHandleFields = data.Fields.Where(f => !f.IsSingleton).ToArray();
            var singletonFields = data.Fields.Where(f => f.IsSingleton).ToArray();
            var singletonDependencies = data.SingletonDependencies;

            foreach (var field in data.Fields)
            {
                var handleField = typeHandle.AddProperty(field.HandleName, Accessibility.Public).SetType(field.HandleTypeName);

                if (field.IsReadOnly)
                {
                    handleField.AddAttribute("ReadOnly");
                }
            }

            var create = typeHandle.AddMethod("Create", Accessibility.Public).WithReturnType("void");
            create.AddParameter("ref SystemState", "state");
            create.WithSummary($"Initializes type handles used by {data.TypeName}.")
                .WithParameterDoc("state", "System state used to create handles.");
            create.WithBody(body =>
            {
                foreach (var field in typeHandleFields)
                {
                    if (field.IsFacet)
                    {
                        body.AppendLine($"this.{field.HandleName}.Create(ref state);");
                        continue;
                    }

                    if (field.IsEntity)
                    {
                        body.AppendLine($"this.{field.HandleName} = state.GetEntityTypeHandle();");
                        continue;
                    }

                    var method = field.IsBuffer ? "GetBufferTypeHandle" : "GetComponentTypeHandle";
                    var readOnly = field.IsReadOnly ? "true" : string.Empty;
                    body.AppendLine($"this.{field.HandleName} = state.{method}<{field.ComponentTypeName}>({readOnly});");
                }
            });

            var update = typeHandle.AddMethod("Update", Accessibility.Public).WithReturnType("void");
            update.AddParameter("ref SystemState", "state");
            foreach (var dependency in singletonDependencies)
            {
                update.AddParameter($"in {dependency.Field.ComponentTypeName}", dependency.ParameterName);
            }
            update.WithSummary($"Updates type handles for {data.TypeName} and refreshes singleton caches.")
                .WithParameterDoc("state", "System state used to update handles.");
            foreach (var dependency in singletonDependencies)
            {
                update.WithParameterDoc(dependency.ParameterName, GetSingletonRetrievalDoc(dependency.Field));
            }

            update.WithBody(body =>
            {
                foreach (var field in typeHandleFields)
                {
                    if (field.IsFacet)
                    {
                        body.AppendLine($"this.{field.HandleName}.Update(ref state{GetFacetSingletonArguments(field)});");
                    }
                    else
                    {
                        body.AppendLine($"this.{field.HandleName}.Update(ref state);");
                    }
                }

                foreach (var field in singletonFields)
                {
                    body.AppendLine($"this.{field.HandleName} = {field.FieldName};");
                }
            });

            var resolve = typeHandle.AddMethod("Resolve", Accessibility.Public).WithReturnType("ResolvedChunk");
            resolve.AddParameter("ArchetypeChunk", "chunk");
            resolve.WithSummary($"Resolves a chunk into {data.TypeName}.ResolvedChunk for job access.")
                .WithParameterDoc("chunk", "Chunk being processed.");
            resolve.WithBody(body =>
            {
                // Unity chunk accessors return default handles/arrays when a component is absent,
                // so optional fields remain safe even if the query includes archetypes without them.
                var assignments = data.Fields.Select(field =>
                {
                    var value = field.IsSingleton
                        ? $"this.{field.HandleName}"
                        : field.IsFacet
                            ? GetFacetResolveExpression(field)
                        : field.IsEntity
                            ? GetEntityResolveExpression(field)
                            : field.IsBuffer
                                ? GetBufferResolveExpression(field)
                                : field.IsEnabled ? GetEnabledResolveExpression(field) : GetComponentResolveExpression(field);
                    return $"{field.ResolvedFieldName} = {value},";
                });

                using (body.BlockWithDelimiter("return new ResolvedChunk"))
                {
                    body.AppendLines(assignments, assignment => assignment);
                }
            });
        }

        private static string GetComponentResolveExpression(FacetField field)
        {
            return $"chunk.GetNativeArray(ref this.{field.HandleName})";
        }

        private static string GetEntityResolveExpression(FacetField field)
        {
            return $"chunk.GetNativeArray(this.{field.HandleName})";
        }

        private static string GetEnabledResolveExpression(FacetField field)
        {
            return $"chunk.GetEnabledMask(ref this.{field.HandleName})";
        }

        private static string GetBufferResolveExpression(FacetField field)
        {
            return $"chunk.GetBufferAccessor(ref this.{field.HandleName})";
        }

        private static string GetFacetResolveExpression(FacetField field)
        {
            return $"this.{field.HandleName}.Resolve(chunk)";
        }

        private static string GetFacetSingletonArguments(FacetField field)
        {
            if (!field.IsFacet || field.FacetSingletonDependencies.Count == 0)
            {
                return string.Empty;
            }

            var arguments = string.Join(", ", field.FacetSingletonDependencies.Select(d => d.ParameterName));
            return $", {arguments}";
        }

        private static void WriteLookupAcquisition(ICodeWriter writer, FacetField field, bool inTryGet)
        {
            var lookup = $"this.{field.LookupFieldName}";
            var name = field.ArgumentName;

            switch (field.Kind)
            {
                case FacetFieldKind.Entity:
                    writer.AppendLine($"var {name} = entity;");
                    break;

                case FacetFieldKind.Singleton:
                    writer.AppendLine($"var {name} = this.{field.LookupFieldName};");
                    break;

                case FacetFieldKind.Facet:
                    if (field.IsOptional)
                    {
                        writer.AppendLine($"{lookup}.TryGet(entity, out var {name});");
                    }
                    else if (inTryGet)
                    {
                        using (writer.Block($"if (!{lookup}.TryGet(entity, out var {name}))"))
                        {
                            writer.AppendLine("return false;");
                        }
                    }
                    else
                    {
                        writer.AppendLine($"var {name} = {lookup}[entity];");
                    }

                    break;

                case FacetFieldKind.DynamicBuffer:
                    if (field.IsOptional)
                    {
                        writer.AppendLine($"{lookup}.TryGetBuffer(entity, out var {name});");
                    }
                    else if (inTryGet)
                    {
                        using (writer.Block($"if (!{lookup}.TryGetBuffer(entity, out var {name}))"))
                        {
                            writer.AppendLine("return false;");
                        }
                    }
                    else
                    {
                        writer.AppendLine($"var {name} = {lookup}[entity];");
                    }

                    break;

                case FacetFieldKind.RefRW:
                    if (field.IsOptional)
                    {
                        writer.AppendLine($"{lookup}.TryGetRefRW(entity, out var {name});");
                    }
                    else if (inTryGet)
                    {
                        using (writer.Block($"if (!{lookup}.TryGetRefRW(entity, out var {name}))"))
                        {
                            writer.AppendLine("return false;");
                        }
                    }
                    else
                    {
                        writer.AppendLine($"var {name} = {lookup}.GetRefRW(entity);");
                    }

                    break;

                case FacetFieldKind.RefRO:
                    if (field.IsOptional)
                    {
                        writer.AppendLine($"{lookup}.TryGetRefRO(entity, out var {name});");
                    }
                    else if (inTryGet)
                    {
                        using (writer.Block($"if (!{lookup}.TryGetRefRO(entity, out var {name}))"))
                        {
                            writer.AppendLine("return false;");
                        }
                    }
                    else
                    {
                        writer.AppendLine($"var {name} = {lookup}.GetRefRO(entity);");
                    }

                    break;

                case FacetFieldKind.EnabledRefRW:
                    if (inTryGet || field.IsOptional)
                    {
                        writer.AppendLine($"var {name} = {lookup}.GetEnabledRefRWOptional<{field.ComponentTypeName}>(entity);");

                        if (!field.IsOptional)
                        {
                            using (writer.Block($"if (!{name}.IsValid)"))
                            {
                                writer.AppendLine("return false;");
                            }
                        }
                    }
                    else
                    {
                        writer.AppendLine($"var {name} = {lookup}.GetEnabledRefRW<{field.ComponentTypeName}>(entity);");
                    }

                    break;

                case FacetFieldKind.EnabledRefRO:
                    if (inTryGet || field.IsOptional)
                    {
                        writer.AppendLine($"var {name} = {lookup}.GetEnabledRefROOptional<{field.ComponentTypeName}>(entity);");

                        if (!field.IsOptional)
                        {
                            using (writer.Block($"if (!{name}.IsValid)"))
                            {
                                writer.AppendLine("return false;");
                            }
                        }
                    }
                    else
                    {
                        writer.AppendLine($"var {name} = {lookup}.GetEnabledRefRO<{field.ComponentTypeName}>(entity);");
                    }

                    break;
            }
        }

        private static string GetResolvedArgument(FacetField field)
        {
            if (field.IsSingleton)
            {
                return $"this.{field.ResolvedFieldName}";
            }

            if (field.IsFacet)
            {
                return $"{field.ResolvedFieldName}[index]";
            }

            if (field.IsEntity)
            {
                return $"{field.ResolvedFieldName}[index]";
            }

            if (field.IsBuffer)
            {
                return field.IsOptional
                    ? $"{field.ResolvedFieldName}.Length != 0 ? {field.ResolvedFieldName}[index] : default"
                    : $"{field.ResolvedFieldName}[index]";
            }

            if (field.IsEnabled)
            {
                var accessor = field.IsOptional ? "GetOptionalEnabledRef" : "GetEnabledRef";
                var rw = field.Kind == FacetFieldKind.EnabledRefRW ? "RW" : "RO";
                return $"{field.ResolvedFieldName}.{accessor}{rw}<{field.ComponentTypeName}>(index)";
            }

            var constructor = field.Kind == FacetFieldKind.RefRO
                ? $"new RefRO<{field.ComponentTypeName}>"
                : $"new RefRW<{field.ComponentTypeName}>";

            if (!field.IsOptional)
            {
                return $"{constructor}({field.ResolvedFieldName}, index)";
            }

            return $"{field.ResolvedFieldName}.IsCreated ? {constructor}({field.ResolvedFieldName}, index) : default";
        }

        private static string GetQueryBuilderInvocation(FacetField field)
        {
            return field.Kind switch
            {
                FacetFieldKind.RefRW => $"WithAllRW<{field.ComponentTypeName}>()",
                FacetFieldKind.RefRO => $"WithAll<{field.ComponentTypeName}>()",
                FacetFieldKind.EnabledRefRW => $"WithAllRW<{field.ComponentTypeName}>()",
                FacetFieldKind.EnabledRefRO => $"WithAllRW<{field.ComponentTypeName}>()",
                FacetFieldKind.DynamicBuffer when field.IsReadOnly => $"WithAll<{field.ComponentTypeName}>()",
                FacetFieldKind.DynamicBuffer => $"WithAllRW<{field.ComponentTypeName}>()",
                _ => throw new ArgumentOutOfRangeException(nameof(field.Kind), field.Kind, null),
            };
        }

        private static string GetXmlSafeTypeName(FacetField field)
        {
            return field.ComponentTypeName.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string GetSingletonRetrievalDoc(FacetField field)
        {
            var typeName = GetXmlSafeTypeName(field);

            if (field.ComponentTypeSymbol is INamedTypeSymbol { Name: "DynamicBuffer", TypeArguments: { Length: 1 } } namedType)
            {
                var elementTypeName = namedType.TypeArguments[0].ToDisplayString(ShortTypeFormat).Replace("<", "&lt;").Replace(">", "&gt;");
                return $"Singleton value for {typeName} which is typically retrieved via SystemAPI.GetSingletonBuffer&lt;{elementTypeName}&gt;().";
            }

            return $"Singleton value for {typeName} which is typically retrieved via SystemAPI.GetSingleton&lt;{typeName}&gt;().";
        }

        private static string Pascalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return $"{char.ToUpper(value[0], System.Globalization.CultureInfo.InvariantCulture)}{value.Substring(1)}";
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
            INamedTypeSymbol entityType)
        {
            this.FacetInterface = facetInterface;
            this.OptionalAttribute = optionalAttribute;
            this.FacetAttribute = facetAttribute;
            this.ReadOnlyAttribute = readOnlyAttribute;
            this.SingletonAttribute = singletonAttribute;
            this.EntityType = entityType;
        }

        public INamedTypeSymbol FacetInterface { get; }

        public INamedTypeSymbol OptionalAttribute { get; }

        public INamedTypeSymbol FacetAttribute { get; }

        public INamedTypeSymbol ReadOnlyAttribute { get; }

        public INamedTypeSymbol SingletonAttribute { get; }

        public INamedTypeSymbol EntityType { get; }

        public static FacetSymbols Create(Compilation compilation)
        {
            return new FacetSymbols(
                compilation.GetTypeByMetadataName("BovineLabs.Core.IFacet"),
                compilation.GetTypeByMetadataName("BovineLabs.Core.FacetOptionalAttribute"),
                compilation.GetTypeByMetadataName("BovineLabs.Core.FacetAttribute"),
                compilation.GetTypeByMetadataName("Unity.Collections.ReadOnlyAttribute"),
                compilation.GetTypeByMetadataName("BovineLabs.Core.SingletonAttribute"),
                compilation.GetTypeByMetadataName("Unity.Entities.Entity"));
        }
    }

    internal sealed class FacetData
    {
        public FacetData(INamedTypeSymbol typeSymbol, IReadOnlyList<FacetField> fields, IReadOnlyList<FacetSingletonDependency> singletonDependencies)
        {
            this.TypeSymbol = typeSymbol;
            this.Fields = fields;
            this.SingletonDependencies = singletonDependencies;
            this.typeName = typeSymbol.ToDisplayString(FacetGenerator.ShortTypeFormat);
        }

        public INamedTypeSymbol TypeSymbol { get; }

        public IReadOnlyList<FacetField> Fields { get; }

        public IReadOnlyList<FacetSingletonDependency> SingletonDependencies { get; }

        public string TypeName => this.typeName;

        private readonly string typeName;
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
        public FacetField(IFieldSymbol symbol, ITypeSymbol componentType, FacetFieldKind kind, bool isOptional, bool isReadOnly)
        {
            this.Symbol = symbol;
            this.ComponentTypeSymbol = componentType;
            this.Kind = kind;
            this.IsOptional = isOptional;
            this.IsReadOnly = isReadOnly;
            this.FieldTypeName = symbol.Type.ToDisplayString(FacetGenerator.ShortTypeFormat);
            this.ComponentTypeName = componentType.ToDisplayString(FacetGenerator.ShortTypeFormat);
            this.ArgumentName = this.FieldName is "entity" or "facet" ? $"{this.FieldName}Value" : this.FieldName;
        }

        public IFieldSymbol Symbol { get; }

        public ITypeSymbol ComponentTypeSymbol { get; }

        public FacetFieldKind Kind { get; }

        public bool IsOptional { get; }

        public bool IsReadOnly { get; }

        public bool IsEntity => this.Kind == FacetFieldKind.Entity;

        public bool IsSingleton => this.Kind == FacetFieldKind.Singleton;

        public bool IsBuffer => this.Kind == FacetFieldKind.DynamicBuffer;

        public bool IsEnabled => this.Kind == FacetFieldKind.EnabledRefRW || this.Kind == FacetFieldKind.EnabledRefRO;

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
                if (this.IsSingleton || this.IsFacet)
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
                if (this.IsSingleton || this.IsFacet)
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
                if (this.IsSingleton || this.IsFacet)
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
                : this.IsEntity
                    ? this.ComponentTypeName
                    : this.IsBuffer
                        ? $"BufferLookup<{this.ComponentTypeName}>"
                        : $"ComponentLookup<{this.ComponentTypeName}>";

        public void SetFacetSingletonDependencies(IReadOnlyList<FacetSingletonDependency> dependencies)
        {
            this.FacetSingletonDependencies = dependencies ?? Array.Empty<FacetSingletonDependency>();
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
                : this.IsEntity
                    ? "EntityTypeHandle"
                    : this.IsBuffer
                        ? $"BufferTypeHandle<{this.ComponentTypeName}>"
                        : $"ComponentTypeHandle<{this.ComponentTypeName}>";

        private static string Pluralize(string name)
        {
            return name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? name : $"{name}s";
        }

        private string PascalFieldName => $"{char.ToUpper(this.FieldName[0], System.Globalization.CultureInfo.InvariantCulture)}{this.FieldName.Substring(1)}";
    }
}
