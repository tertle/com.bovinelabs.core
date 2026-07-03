// <copyright file="FacetGenerator.CodeGen.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.FacetGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeGenHelpers;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class FacetGenerator
    {
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
                .Create(data.TypeSymbol.ContainingNamespace.ToDisplayString());

            var namespaces = new HashSet<string>(StringComparer.Ordinal)
            {
                "Unity.Collections",
                "Unity.Entities",
                "BovineLabs.Core.Extensions",
            };

            AddDeclaredUsingNamespaces(data.TypeSymbol, namespaces);
            AddRequiredTypeNamespaces(data, namespaces);

            foreach (var ns in namespaces)
            {
                builder.AddNamespaceImport(ns);
            }

            ResolveResolvedFieldNameConflicts(data.Fields);

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
            AddSingletonData(typeBuilder, data);
            AddCreateQueryBuilder(typeBuilder, data);

            return builder;
        }

        private static void AddDeclaredUsingNamespaces(INamedTypeSymbol typeSymbol, ISet<string> namespaces)
        {
            foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is not TypeDeclarationSyntax typeSyntax)
                {
                    continue;
                }

                var compilationUnit = typeSyntax.SyntaxTree.GetCompilationUnitRoot();
                AddUsingDirectives(compilationUnit.Usings, namespaces);

                foreach (var namespaceSyntax in typeSyntax.Ancestors().OfType<BaseNamespaceDeclarationSyntax>())
                {
                    AddUsingDirectives(namespaceSyntax.Usings, namespaces);
                }
            }
        }

        private static void AddRequiredTypeNamespaces(FacetData data, ISet<string> namespaces)
        {
            foreach (var field in data.Fields)
            {
                AddTypeNamespaces(field.ComponentTypeSymbol, namespaces);
                AddTypeNamespaces(field.Symbol.Type, namespaces);
            }

            foreach (var invocation in data.QueryBuilderInvocations)
            {
                AddTypeNamespaces(invocation.ComponentTypeSymbol, namespaces);
            }
        }

        private static void AddTypeNamespaces(ITypeSymbol typeSymbol, ISet<string> namespaces)
        {
            if (typeSymbol == null)
            {
                return;
            }

            switch (typeSymbol)
            {
                case INamedTypeSymbol namedType:
                    if (!namedType.ContainingNamespace.IsGlobalNamespace)
                    {
                        namespaces.Add(namedType.ContainingNamespace.ToDisplayString());
                    }

                    foreach (var typeArgument in namedType.TypeArguments)
                    {
                        AddTypeNamespaces(typeArgument, namespaces);
                    }

                    break;
                case IArrayTypeSymbol arrayType:
                    AddTypeNamespaces(arrayType.ElementType, namespaces);
                    break;
                case IPointerTypeSymbol pointerType:
                    AddTypeNamespaces(pointerType.PointedAtType, namespaces);
                    break;
            }
        }

        private static void AddUsingDirectives(SyntaxList<UsingDirectiveSyntax> directives, ISet<string> namespaces)
        {
            foreach (var directive in directives)
            {
                var name = directive.Name.ToString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (directive.Alias != null)
                {
                    var alias = directive.Alias.Name.ToString();
                    if (!string.IsNullOrWhiteSpace(alias))
                    {
                        namespaces.Add($"{alias} = {name}");
                    }

                    continue;
                }

                if (directive.StaticKeyword != default)
                {
                    namespaces.Add($"static {name}");
                    continue;
                }

                namespaces.Add(name);
            }
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
            var queries = data.QueryBuilderInvocations;

            var method = typeBuilder
                .AddMethod("CreateQueryBuilder", Accessibility.Public)
                .MakeStaticMethod()
                .WithReturnType("EntityQueryBuilder")
                .WithSummary($"Creates an EntityQueryBuilder requesting the required components for {data.TypeName}.")
                .WithParameterDoc("allocator", "Allocator used for the query builder.");

            method.AddParameterWithDefaultValue("Allocator", "allocator", "Allocator.Temp");

            method.WithBody(body =>
            {
                var chain = queries.Count == 0
                    ? string.Empty
                    : string.Concat(queries.Select(q => $".{q.Invocation}"));

                body.AppendLine($"return new EntityQueryBuilder(allocator){chain};");
            });
        }

        private static void AddLookup(ClassBuilder typeBuilder, FacetData data)
        {
            var lookup = typeBuilder.AddNestedClass("Lookup", true, Accessibility.Public)
                .IsStruct()
                .WithSummary($"Provides entity-level access to {data.TypeName}.");

            var lookupFields = data.Fields.Where(f => !f.IsSingleton && !f.IsEntity).ToArray();
            var lookupSlots = GetLookupSlots(lookupFields);
            var singletonFields = data.Fields.Where(f => f.IsSingleton).ToArray();
            var singletonDependencies = data.SingletonDependencies;
            var singletonParameterNames = singletonDependencies
                .Where(dependency => dependency.Field.IsSingleton)
                .ToDictionary(dependency => dependency.Field, dependency => dependency.ParameterName);

            foreach (var slot in lookupSlots)
            {
                var field = slot.Field;
                var lookupField = lookup.AddProperty(field.LookupFieldName, Accessibility.Public).SetType(field.LookupTypeName);

                if (slot.IsReadOnly)
                {
                    lookupField.AddAttribute("ReadOnly");
                }
            }

            foreach (var field in singletonFields)
            {
                var singletonField = lookup
                    .AddProperty(field.LookupFieldName, Accessibility.Public)
                    .SetType(field.FieldTypeName);

                if (field.HasReadOnlyAttribute)
                {
                    singletonField.AddAttribute("ReadOnly");
                }
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
                foreach (var slot in lookupSlots)
                {
                    var field = slot.Field;
                    var readOnlyArgument = slot.IsReadOnly ? "true" : string.Empty;

                    if (field.IsFacet)
                    {
                        body.AppendLine($"this.{field.LookupFieldName}.Create(ref state);");
                        continue;
                    }

                    if (field.IsEntityStorageInfo || field.IsEntityStorageInfoLookup)
                    {
                        body.AppendLine($"this.{field.LookupFieldName} = state.GetEntityStorageInfoLookup();");
                        continue;
                    }

                    if (field.IsComponentLookup)
                    {
                        body.AppendLine($"this.{field.LookupFieldName} = state.GetComponentLookup<{field.ComponentTypeName}>({readOnlyArgument});");
                        continue;
                    }

                    if (field.IsBufferLookup)
                    {
                        body.AppendLine($"this.{field.LookupFieldName} = state.GetBufferLookup<{field.ComponentTypeName}>({readOnlyArgument});");
                        continue;
                    }

                    var lookupExpression = field.IsBuffer
                        ? $"state.GetBufferLookup<{field.ComponentTypeName}>({readOnlyArgument})"
                        : $"state.GetComponentLookup<{field.ComponentTypeName}>({readOnlyArgument})";

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
                foreach (var slot in lookupSlots)
                {
                    var field = slot.Field;

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
                    if (!singletonParameterNames.TryGetValue(field, out var parameterName))
                    {
                        parameterName = field.FieldName;
                    }

                    body.AppendLine($"this.{field.LookupFieldName} = {parameterName};");
                }
            });

            if (singletonDependencies.Count > 0)
            {
                var updateFromData = lookup.AddMethod("Update", Accessibility.Public).WithReturnType("void");
                updateFromData.AddParameter("ref SystemState", "state");
                updateFromData.AddParameter("SingletonData", "data");
                updateFromData.WithSummary($"Refreshes lookups for {data.TypeName} and updates singleton caches.")
                    .WithParameterDoc("state", "System state used to update handles.")
                    .WithParameterDoc("data", $"Singleton queries used to resolve {data.TypeName} singletons.");

                updateFromData.WithBody(body =>
                {
                    foreach (var dependency in singletonDependencies)
                    {
                        var expression = GetSingletonDataResolveExpression(dependency, "data");
                        body.AppendLine($"var {dependency.ParameterName} = {expression};");
                    }

                    body.NewLine();

                    var arguments = string.Join(", ", singletonDependencies.Select(dependency => $"in {dependency.ParameterName}"));
                    body.AppendLine($"this.Update(ref state, {arguments});");
                });
            }
        }

        private static IReadOnlyList<LookupSlot> GetLookupSlots(IEnumerable<FacetField> fields)
        {
            return fields
                .GroupBy(field => (field.LookupFieldName, field.LookupTypeName))
                .Select(group => new LookupSlot(group.First(), group.All(field => field.IsReadOnly)))
                .ToArray();
        }

        private sealed class LookupSlot
        {
            public LookupSlot(FacetField field, bool isReadOnly)
            {
                this.Field = field;
                this.IsReadOnly = isReadOnly;
            }

            public FacetField Field { get; }

            public bool IsReadOnly { get; }
        }

        private static void AddResolvedChunk(ClassBuilder typeBuilder, FacetData data)
        {
            var resolvedChunk = typeBuilder.AddNestedClass("ResolvedChunk", false, Accessibility.Public)
                .IsStruct()
                .WithSummary($"Chunk-level accessors for {data.TypeName}.");

            var resolvedFields = GetUniqueResolvedFields(data.Fields);

            foreach (var field in resolvedFields)
            {
                resolvedChunk.AddProperty(field.ResolvedFieldName, Accessibility.Public).SetType(field.ResolvedFieldTypeName);
            }

            var tryGet = resolvedChunk.AddMethod("TryGet", Accessibility.Public).WithReturnType("bool");
            tryGet.AddParameter("int", "index");
            tryGet.AddParameter($"out {data.TypeName}", "facet");
            tryGet.WithSummary($"Attempts to get {data.TypeName} for an entity in the chunk by index.")
                .WithParameterDoc("index", "Entity index in the chunk.")
                .WithParameterDoc("facet", "Resolved facet when all required fields are available.");
            tryGet.WithBody(body =>
            {
                body.AppendLine("facet = default;");
                body.NewLine();

                foreach (var field in data.Fields)
                {
                    WriteResolvedAcquisition(body, field, true);
                }

                body.AppendLine($"facet = new {data.TypeName}({string.Join(", ", data.Fields.Select(f => f.ArgumentName))});");
                body.AppendLine("return true;");
            });

            var resolvedIndexer = resolvedChunk
                .AddProperty("this[int index]", Accessibility.Public)
                .SetType(data.TypeName)
                .WithSummary($"Gets the {data.TypeName} for an entity in the chunk by index.");

            resolvedIndexer.WithGetter(getter =>
            {
                getter.AppendLine("#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG");
                getter.AppendLine("if (!this.TryGet(index, out var facet))");
                getter.AppendLine("{");
                getter.AppendLine("    throw new global::System.InvalidOperationException(\"ResolvedChunk indexer could not resolve the requested facet. Use TryGet() when facet availability is optional.\");");
                getter.AppendLine("}");
                getter.AppendLine("#else");
                getter.AppendLine("this.TryGet(index, out var facet);");
                getter.AppendLine("#endif");
                getter.AppendLine("return facet;");
            });
        }

        private static void ResolveResolvedFieldNameConflicts(IReadOnlyList<FacetField> fields)
        {
            var usedNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var field in fields)
            {
                usedNames.Add(field.ResolvedFieldName);
            }

            foreach (var group in fields.GroupBy(field => field.ResolvedFieldName, StringComparer.Ordinal))
            {
                if (group.Select(field => field.ResolvedFieldTypeName).Distinct(StringComparer.Ordinal).Skip(1).Any())
                {
                    var keep = group.FirstOrDefault(field => !IsLookupField(field)) ?? group.First();

                    foreach (var field in group)
                    {
                        if (ReferenceEquals(field, keep))
                        {
                            continue;
                        }

                        var baseName = IsLookupField(field)
                            ? $"{field.ResolvedFieldName}Lookup"
                            : Pascalize(field.FieldName);

                        var uniqueName = GetUniqueName(baseName, usedNames);
                        field.SetResolvedFieldNameOverride(uniqueName);
                    }
                }
            }
        }

        private static IReadOnlyList<FacetField> GetUniqueResolvedFields(IEnumerable<FacetField> fields)
        {
            var result = new List<FacetField>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var field in fields)
            {
                if (seen.Add(field.ResolvedFieldName))
                {
                    result.Add(field);
                }
            }

            return result;
        }

        private static bool IsLookupField(FacetField field)
        {
            return field.IsComponentLookup || field.IsBufferLookup || field.IsEntityStorageInfoLookup;
        }

        private static string GetUniqueName(string baseName, ISet<string> usedNames)
        {
            if (usedNames.Add(baseName))
            {
                return baseName;
            }

            var index = 2;
            string candidate;

            do
            {
                candidate = $"{baseName}{index}";
                index++;
            }
            while (!usedNames.Add(candidate));

            return candidate;
        }

        private static void AddTypeHandle(ClassBuilder typeBuilder, FacetData data)
        {
            var typeHandle = typeBuilder.AddNestedClass("TypeHandle", true, Accessibility.Public)
                .IsStruct()
                .WithSummary($"Maintains type handles for chunk access to {data.TypeName}.");

            var typeHandleFields = data.Fields.Where(f => !f.IsSingleton).ToArray();
            var singletonFields = data.Fields.Where(f => f.IsSingleton).ToArray();
            var singletonDependencies = data.SingletonDependencies;
            var singletonParameterNames = singletonDependencies
                .Where(dependency => dependency.Field.IsSingleton)
                .ToDictionary(dependency => dependency.Field, dependency => dependency.ParameterName);

            foreach (var field in data.Fields)
            {
                var handleField = typeHandle.AddProperty(field.HandleName, Accessibility.Public).SetType(field.HandleTypeName);

                if (field.IsReadOnly && (!field.IsSingleton || field.HasReadOnlyAttribute))
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

                    if (field.IsEntityStorageInfo || field.IsEntityStorageInfoLookup)
                    {
                        body.AppendLine($"this.{field.HandleName} = state.GetEntityStorageInfoLookup();");
                        continue;
                    }

                    if (field.IsComponentLookup)
                    {
                        body.AppendLine($"this.{field.HandleName} = state.GetComponentLookup<{field.ComponentTypeName}>({(field.IsReadOnly ? "true" : string.Empty)});");
                        continue;
                    }

                    if (field.IsBufferLookup)
                    {
                        body.AppendLine($"this.{field.HandleName} = state.GetBufferLookup<{field.ComponentTypeName}>({(field.IsReadOnly ? "true" : string.Empty)});");
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
                    if (!singletonParameterNames.TryGetValue(field, out var parameterName))
                    {
                        parameterName = field.FieldName;
                    }

                    body.AppendLine($"this.{field.HandleName} = {parameterName};");
                }
            });

            if (singletonDependencies.Count > 0)
            {
                var updateFromData = typeHandle.AddMethod("Update", Accessibility.Public).WithReturnType("void");
                updateFromData.AddParameter("ref SystemState", "state");
                updateFromData.AddParameter("SingletonData", "data");
                updateFromData.WithSummary($"Updates type handles for {data.TypeName} and refreshes singleton caches.")
                    .WithParameterDoc("state", "System state used to update handles.")
                    .WithParameterDoc("data", $"Singleton queries used to resolve {data.TypeName} singletons.");

                updateFromData.WithBody(body =>
                {
                    foreach (var dependency in singletonDependencies)
                    {
                        var expression = GetSingletonDataResolveExpression(dependency, "data");
                        body.AppendLine($"var {dependency.ParameterName} = {expression};");
                    }

                    body.NewLine();

                    var arguments = string.Join(", ", singletonDependencies.Select(dependency => $"in {dependency.ParameterName}"));
                    body.AppendLine($"this.Update(ref state, {arguments});");
                });
            }

            var resolve = typeHandle.AddMethod("Resolve", Accessibility.Public).WithReturnType("ResolvedChunk");
            resolve.AddParameter("ArchetypeChunk", "chunk");
            resolve.WithSummary($"Resolves a chunk into {data.TypeName}.ResolvedChunk for job access.")
                .WithParameterDoc("chunk", "Chunk being processed.");

            var resolvedFields = GetUniqueResolvedFields(data.Fields);
            resolve.WithBody(body =>
            {
                // Unity chunk accessors return default handles/arrays when a component is absent,
                // so optional fields remain safe even if the query includes archetypes without them.
                var assignments = resolvedFields.Select(field =>
                {
                    var value = field.IsSingleton
                        ? $"this.{field.HandleName}"
                        : field.IsFacet
                            ? GetFacetResolveExpression(field)
                            : field.IsEntity
                                ? GetEntityResolveExpression(field)
                                : field.IsEntityStorageInfo
                                    ? GetEntityStorageInfoResolveExpression()
                                    : field.IsEntityStorageInfoLookup
                                        ? $"this.{field.HandleName}"
                                        : field.IsComponentLookup || field.IsBufferLookup
                                            ? $"this.{field.HandleName}"
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

        private static void AddSingletonData(ClassBuilder typeBuilder, FacetData data)
        {
            var singletonDependencies = data.SingletonDependencies;
            if (singletonDependencies.Count == 0)
            {
                return;
            }

            var singletonData = typeBuilder.AddNestedClass("SingletonData", true, Accessibility.Public)
                .IsStruct()
                .WithSummary($"Provides singleton queries for {data.TypeName}.");

            foreach (var dependency in singletonDependencies)
            {
                var queryFieldName = GetSingletonDataQueryFieldName(dependency);
                singletonData.AddProperty(queryFieldName, Accessibility.Public)
                    .SetType("EntityQuery");
            }

            var create = singletonData.AddMethod("Create", Accessibility.Public).WithReturnType("void");
            create.AddParameter("ref SystemState", "state");
            create.WithSummary($"Initializes singleton queries for {data.TypeName}.")
                .WithParameterDoc("state", "System state used to build queries.");

            create.WithBody(body =>
            {
                foreach (var dependency in singletonDependencies)
                {
                    var queryFieldName = GetSingletonDataQueryFieldName(dependency);
                    var queryInvocation = GetSingletonQueryBuilderInvocation(dependency.Field);
                    body.AppendLine($"this.{queryFieldName} = new EntityQueryBuilder(Allocator.Temp).{queryInvocation}.Build(ref state);");
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

        private static string GetEntityStorageInfoResolveExpression()
        {
            return "chunk";
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

                case FacetFieldKind.EntityStorageInfo:
                    if (field.IsOptional)
                    {
                        writer.AppendLine($"var {name} = {lookup}.Exists(entity) ? {lookup}[entity] : default;");
                    }
                    else if (inTryGet)
                    {
                        using (writer.Block($"if (!{lookup}.Exists(entity))"))
                        {
                            writer.AppendLine("return false;");
                        }

                        writer.AppendLine($"var {name} = {lookup}[entity];");
                    }
                    else
                    {
                        writer.AppendLine($"var {name} = {lookup}[entity];");
                    }

                    break;

                case FacetFieldKind.EntityStorageInfoLookup:
                    writer.AppendLine($"var {name} = {lookup};");
                    break;

                case FacetFieldKind.ComponentLookup:
                case FacetFieldKind.BufferLookup:
                    writer.AppendLine($"var {name} = {lookup};");
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

        private static void WriteResolvedAcquisition(ICodeWriter writer, FacetField field, bool inTryGet)
        {
            var resolved = $"this.{field.ResolvedFieldName}";
            var name = field.ArgumentName;

            switch (field.Kind)
            {
                case FacetFieldKind.Entity:
                    writer.AppendLine($"var {name} = {resolved}[index];");
                    break;

                case FacetFieldKind.EntityStorageInfo:
                    writer.AppendLine($"var {name} = new EntityStorageInfo {{ Chunk = {resolved}, IndexInChunk = index }};");
                    break;

                case FacetFieldKind.EntityStorageInfoLookup:
                case FacetFieldKind.ComponentLookup:
                case FacetFieldKind.BufferLookup:
                case FacetFieldKind.Singleton:
                    writer.AppendLine($"var {name} = {resolved};");
                    break;

                case FacetFieldKind.Facet:
                    if (field.IsOptional)
                    {
                        writer.AppendLine($"{resolved}.TryGet(index, out var {name});");
                    }
                    else if (inTryGet)
                    {
                        using (writer.Block($"if (!{resolved}.TryGet(index, out var {name}))"))
                        {
                            writer.AppendLine("facet = default;");
                            writer.AppendLine("return false;");
                        }
                    }
                    else
                    {
                        writer.AppendLine($"var {name} = {resolved}[index];");
                    }

                    break;

                case FacetFieldKind.DynamicBuffer:
                    if (field.IsOptional)
                    {
                        writer.AppendLine($"var {name} = {resolved}.Length != 0 ? {resolved}[index] : default;");
                    }
                    else if (inTryGet)
                    {
                        using (writer.Block($"if ({resolved}.Length == 0)"))
                        {
                            writer.AppendLine("facet = default;");
                            writer.AppendLine("return false;");
                        }

                        writer.AppendLine($"var {name} = {resolved}[index];");
                    }
                    else
                    {
                        writer.AppendLine($"var {name} = {resolved}[index];");
                    }

                    break;

                case FacetFieldKind.RefRW:
                case FacetFieldKind.RefRO:
                    var constructor = field.Kind == FacetFieldKind.RefRO
                        ? $"new RefRO<{field.ComponentTypeName}>"
                        : $"new RefRW<{field.ComponentTypeName}>";

                    if (field.IsOptional)
                    {
                        writer.AppendLine($"var {name} = {resolved}.IsCreated ? {constructor}({resolved}, index) : default;");
                    }
                    else if (inTryGet)
                    {
                        using (writer.Block($"if (!{resolved}.IsCreated)"))
                        {
                            writer.AppendLine("facet = default;");
                            writer.AppendLine("return false;");
                        }

                        writer.AppendLine($"var {name} = {constructor}({resolved}, index);");
                    }
                    else
                    {
                        writer.AppendLine($"var {name} = {constructor}({resolved}, index);");
                    }

                    break;

                case FacetFieldKind.EnabledRefRW:
                case FacetFieldKind.EnabledRefRO:
                    var rw = field.Kind == FacetFieldKind.EnabledRefRW ? "RW" : "RO";
                    if (field.IsOptional)
                    {
                        writer.AppendLine($"var {name} = {resolved}.GetOptionalEnabledRef{rw}<{field.ComponentTypeName}>(index);");
                    }
                    else if (inTryGet)
                    {
                        writer.AppendLine($"var {name} = {resolved}.GetOptionalEnabledRef{rw}<{field.ComponentTypeName}>(index);");
                        using (writer.Block($"if (!{name}.IsValid)"))
                        {
                            writer.AppendLine("facet = default;");
                            writer.AppendLine("return false;");
                        }
                    }
                    else
                    {
                        writer.AppendLine($"var {name} = {resolved}.GetEnabledRef{rw}<{field.ComponentTypeName}>(index);");
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

            if (field.IsEntityStorageInfo)
            {
                return $"new EntityStorageInfo {{ Chunk = this.{field.ResolvedFieldName}, IndexInChunk = index }}";
            }

            if (field.IsEntityStorageInfoLookup)
            {
                return $"this.{field.ResolvedFieldName}";
            }

            if (field.IsComponentLookup || field.IsBufferLookup)
            {
                return $"this.{field.ResolvedFieldName}";
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

        private static string GetXmlSafeTypeName(FacetField field)
        {
            return field.ComponentTypeName.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static bool TryGetDynamicBufferElementType(ITypeSymbol typeSymbol, out ITypeSymbol elementType)
        {
            if (typeSymbol is INamedTypeSymbol { Name: "DynamicBuffer", TypeArguments: { Length: 1 } } namedType)
            {
                elementType = namedType.TypeArguments[0];
                return true;
            }

            elementType = null;
            return false;
        }

        private static string GetSingletonRetrievalDoc(FacetField field)
        {
            var typeName = GetXmlSafeTypeName(field);

            if (TryGetDynamicBufferElementType(field.ComponentTypeSymbol, out var elementType))
            {
                var elementTypeName = elementType.ToDisplayString(ShortTypeFormat).Replace("<", "&lt;").Replace(">", "&gt;");
                return $"Singleton value for {typeName} which is typically retrieved via SystemAPI.GetSingletonBuffer&lt;{elementTypeName}&gt;(true).";
            }

            return $"Singleton value for {typeName} which is typically retrieved via SystemAPI.GetSingleton&lt;{typeName}&gt;().";
        }

        private static string GetSingletonDataQueryFieldName(FacetSingletonDependency dependency)
        {
            return $"{Pascalize(dependency.ParameterName)}Query";
        }

        private static string GetSingletonQueryComponentTypeName(FacetField field)
        {
            if (TryGetDynamicBufferElementType(field.ComponentTypeSymbol, out var elementType))
            {
                return elementType.ToDisplayString(ShortTypeFormat);
            }

            return field.ComponentTypeName;
        }

        private static string GetSingletonQueryBuilderInvocation(FacetField field)
        {
            var componentTypeName = GetSingletonQueryComponentTypeName(field);
            // RW dependency without RW access: allows safe writes to native containers stored on the singleton.
            var with = field.HasReadOnlyAttribute ? $"WithAll<{componentTypeName}>()" : $"WithAllRW<{componentTypeName}>()";
            return $"{with}.WithOptions(EntityQueryOptions.IncludeSystems)";
        }

        private static string GetSingletonDataResolveExpression(FacetSingletonDependency dependency, string dataParameterName)
        {
            var queryFieldName = GetSingletonDataQueryFieldName(dependency);

            if (TryGetDynamicBufferElementType(dependency.Field.ComponentTypeSymbol, out var elementType))
            {
                var elementTypeName = elementType.ToDisplayString(ShortTypeFormat);
                return $"{dataParameterName}.{queryFieldName}.GetSingletonBufferNoSync<{elementTypeName}>(true)";
            }

            return $"{dataParameterName}.{queryFieldName}.GetSingleton<{dependency.Field.ComponentTypeName}>()";
        }

        private static string Pascalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return $"{char.ToUpper(value[0], System.Globalization.CultureInfo.InvariantCulture)}{value.Substring(1)}";
        }

        private static string Camelize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return $"{char.ToLower(value[0], System.Globalization.CultureInfo.InvariantCulture)}{value.Substring(1)}";
        }
    }
}
