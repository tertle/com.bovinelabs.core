// <copyright file="DynamicHashMapNetCodeGenerator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.DynamicGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using CodeGenHelpers;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public sealed class DynamicHashMapNetCodeGenerator : IIncrementalGenerator
    {
        private const string AttributeMetadataName = "BovineLabs.Core.Iterators.GhostDynamicHashMapAttribute";
        private const string StructLayoutAttributeMetadataName = "System.Runtime.InteropServices.StructLayoutAttribute";
        private const string FieldOffsetAttributeMetadataName = "System.Runtime.InteropServices.FieldOffsetAttribute";
        private const string DefaultHashMapGeneratedDisplayName = "DynamicHashMap Generated Compact";
        private const string DefaultHashMapRawDisplayName = "DynamicHashMap Raw Compact";
        private const string DefaultMultiHashMapGeneratedDisplayName = "DynamicMultiHashMap Generated Compact";
        private const string DefaultMultiHashMapRawDisplayName = "DynamicMultiHashMap Raw Compact";
        private const int LayoutKindExplicit = 2;

        private static readonly SymbolDisplayFormat QualifiedFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidates = context.SyntaxProvider
                .CreateSyntaxProvider(IsSyntaxTargetForGeneration, GetCandidate)
                .Where(static candidate => candidate != null);

            context.RegisterSourceOutput(candidates, static (productionContext, candidate) => Execute(productionContext, candidate));
        }

        private static void Execute(SourceProductionContext context, DynamicHashMapNetCodeCandidate candidate)
        {
            try
            {
                var result = GetSemanticTargetForGeneration(candidate, context.CancellationToken);
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

                context.AddSource(result.Data.HintName, GenerateSource(result.Data));
            }
            catch (Exception ex)
            {
                SourceGenHelpers.Log(ex.ToString());
            }
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (syntaxNode is not TypeDeclarationSyntax typeDeclaration || typeDeclaration.AttributeLists.Count == 0 ||
                typeDeclaration.Kind() != SyntaxKind.StructDeclaration)
            {
                return false;
            }

            foreach (var attributeList in typeDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var name = attribute.Name.ToString();
                    if (name.EndsWith("GhostDynamicHashMap", StringComparison.Ordinal) ||
                        name.EndsWith("GhostDynamicHashMapAttribute", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static DynamicHashMapNetCodeCandidate GetCandidate(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
        {
            var typeDeclaration = (TypeDeclarationSyntax)ctx.Node;
            var typeSymbol = ctx.SemanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
            if (typeSymbol == null)
            {
                return null;
            }

            foreach (var attribute in typeSymbol.GetAttributes())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsDynamicHashMapGhostSerializerAttribute(attribute))
                {
                    return new DynamicHashMapNetCodeCandidate(typeDeclaration, typeSymbol, attribute);
                }
            }

            return null;
        }

        private static DynamicHashMapNetCodeResult GetSemanticTargetForGeneration(DynamicHashMapNetCodeCandidate candidate, CancellationToken cancellationToken)
        {
            var typeSymbol = candidate.TypeSymbol;
            var typeSyntax = candidate.TypeSyntax;
            var diagnostics = new List<Diagnostic>();
            var location = typeSyntax.Identifier.GetLocation();

            if (typeSymbol.TypeKind != TypeKind.Struct)
            {
                diagnostics.Add(DynamicDiagnostics.NonStruct(typeSymbol, location));
                return new DynamicHashMapNetCodeResult(null, diagnostics);
            }

            if (typeSymbol.ContainingType != null)
            {
                diagnostics.Add(DynamicDiagnostics.NestedType(typeSymbol, location));
                return new DynamicHashMapNetCodeResult(null, diagnostics);
            }

            if (typeSymbol.TypeParameters.Length != 0)
            {
                diagnostics.Add(DynamicHashMapNetCodeDiagnostics.GenericMarker(typeSymbol, location));
                return new DynamicHashMapNetCodeResult(null, diagnostics);
            }

            var mapInterfaces = GetDynamicHashMapInterfaces(typeSymbol, cancellationToken);
            if (mapInterfaces.Count == 0)
            {
                diagnostics.Add(DynamicHashMapNetCodeDiagnostics.UnsupportedMarker(typeSymbol, location));
                return new DynamicHashMapNetCodeResult(null, diagnostics);
            }

            if (mapInterfaces.Count != 1)
            {
                diagnostics.Add(DynamicHashMapNetCodeDiagnostics.AmbiguousMarker(typeSymbol, location));
                return new DynamicHashMapNetCodeResult(null, diagnostics);
            }

            var mapInterface = mapInterfaces[0];
            var collectionKind = GetCollectionKind(mapInterface);
            if (!HasValidMarkerStorage(typeSymbol, mapInterface))
            {
                diagnostics.Add(DynamicHashMapNetCodeDiagnostics.MarkerFields(typeSymbol, location));
                return new DynamicHashMapNetCodeResult(null, diagnostics);
            }

            var keyType = mapInterface.TypeArguments[0];
            var valueType = mapInterface.TypeArguments[1];
            var codecMode = GetCodecMode(candidate.Attribute);
            var serializerSuffix = GetSerializerSuffix(collectionKind);
            var serializerName = typeSymbol.Name + serializerSuffix + "GhostSerializer";
            var codecNamespace = typeSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : typeSymbol.ContainingNamespace.ToDisplayString();
            var codecPlans = new List<DynamicHashMapValueCodecPlan>();
            var codecCache = new Dictionary<ITypeSymbol, DynamicHashMapValueCodecPlan>(SymbolEqualityComparer.Default);

            DynamicHashMapValueCodecPlan keyCodec;
            DynamicHashMapValueCodecPlan valueCodec;
            string mapCodecTypeName;
            string defaultDisplayName;

            if (codecMode == GhostDynamicHashMapCodecMode.RawStable)
            {
                if (!TryGetRawSize(keyType, out var keyBytes))
                {
                    diagnostics.Add(DynamicHashMapNetCodeDiagnostics.UnsupportedType(keyType, location));
                    return new DynamicHashMapNetCodeResult(null, diagnostics);
                }

                if (!TryGetRawSize(valueType, out var valueBytes))
                {
                    diagnostics.Add(DynamicHashMapNetCodeDiagnostics.UnsupportedType(valueType, location));
                    return new DynamicHashMapNetCodeResult(null, diagnostics);
                }

                keyCodec = DynamicHashMapValueCodecPlan.Raw(keyType, keyBytes);
                valueCodec = DynamicHashMapValueCodecPlan.Raw(valueType, valueBytes);
                mapCodecTypeName = "global::BovineLabs.Core.Iterators." + serializerSuffix + "RawGhostCodec<" +
                    keyType.ToDisplayString(QualifiedFormat) + ", " + valueType.ToDisplayString(QualifiedFormat) + ">";
                defaultDisplayName = collectionKind == DynamicHashMapCollectionKind.HashMap
                    ? DefaultHashMapRawDisplayName
                    : DefaultMultiHashMapRawDisplayName;
            }
            else if (codecMode == GhostDynamicHashMapCodecMode.Generated)
            {
                keyCodec = BuildGeneratedCodecPlan(keyType, serializerName, "Key", codecNamespace, codecCache, codecPlans, diagnostics, location);
                valueCodec = BuildGeneratedCodecPlan(valueType, serializerName, "Value", codecNamespace, codecCache, codecPlans, diagnostics, location);

                if (keyCodec == null || valueCodec == null)
                {
                    return new DynamicHashMapNetCodeResult(null, diagnostics);
                }

                mapCodecTypeName = "global::BovineLabs.Core.Iterators." + serializerSuffix + "GeneratedGhostCodec<" +
                    keyType.ToDisplayString(QualifiedFormat) + ", " +
                    valueType.ToDisplayString(QualifiedFormat) + ", " +
                    keyCodec.CodecTypeName + ", " +
                    valueCodec.CodecTypeName + ">";
                defaultDisplayName = collectionKind == DynamicHashMapCollectionKind.HashMap
                    ? DefaultHashMapGeneratedDisplayName
                    : DefaultMultiHashMapGeneratedDisplayName;
            }
            else
            {
                diagnostics.Add(DynamicHashMapNetCodeDiagnostics.UnsupportedCodec(typeSymbol, null, location));
                return new DynamicHashMapNetCodeResult(null, diagnostics);
            }

            var data = new DynamicHashMapNetCodeData(
                collectionKind,
                typeSymbol,
                keyType,
                valueType,
                keyCodec,
                valueCodec,
                mapCodecTypeName,
                codecPlans,
                codecMode == GhostDynamicHashMapCodecMode.RawStable,
                GetNamedBool(candidate.Attribute, "IsDefault", false),
                GetNamedString(candidate.Attribute, "DisplayName", defaultDisplayName),
                GetNamedBool(candidate.Attribute, "SendDataForChildEntity", false),
                GetNamedEnumExpression(candidate.Attribute, "PrefabType", "Unity.NetCode.GhostPrefabType", "All"),
                GetNamedEnumExpression(candidate.Attribute, "SendTypeOptimization", "Unity.NetCode.GhostSendType", "AllClients"),
                GetNamedEnumExpression(candidate.Attribute, "OwnerSendType", "Unity.NetCode.SendToOwnerType", "All"));

            return new DynamicHashMapNetCodeResult(data, diagnostics);
        }

        private static SourceText GenerateSource(DynamicHashMapNetCodeData data)
        {
            var source = new StringBuilder();
            var serializerName = data.TypeSymbol.Name + data.SerializerSuffix + "GhostSerializer";
            var registrationSystemName = data.TypeSymbol.Name + data.SerializerSuffix + "GhostSerializerRegistrationSystem";
            var variantTypeFullName = data.Namespace.Length == 0 ? serializerName + "Variant" : data.Namespace + "." + serializerName + "Variant";
            var sendChild = data.SendDataForChildEntity ? "1" : "0";
            var isDefaultSerializer = data.IsDefault ? "1" : "0";
            var variantHash = data.IsDefault
                ? $"Unity.NetCode.GhostVariantsUtility.CalculateVariantHashForComponent(Unity.Entities.ComponentType.ReadWrite<{data.TypeName}>())"
                : $"Unity.NetCode.GhostVariantsUtility.UncheckedVariantHashNBC(VariantTypeFullName, typeof({data.TypeName}).FullName)";
            var serializerType = data.UseRawSerializerPath
                ? "BovineLabs.Core.Iterators." + data.SerializerSuffix + "NetCodeSerializer<" +
                    data.TypeName + ", " + data.KeyTypeName + ", " + data.ValueTypeName + ">"
                : "BovineLabs.Core.Iterators." + data.SerializerSuffix + "NetCodeSerializer<" +
                    data.TypeName + ", " + data.KeyTypeName + ", " + data.ValueTypeName + ", " + data.KeyCodec.CodecTypeName + ", " +
                    data.ValueCodec.CodecTypeName + ">";

            source.AppendLine("#if UNITY_NETCODE");
            source.AppendLine("// <auto-generated/>");

            if (data.Namespace.Length != 0)
            {
                source.Append("namespace ");
                source.AppendLine(data.Namespace);
                source.AppendLine("{");
            }

            foreach (var codec in data.GeneratedCodecs)
            {
                source.Append(codec.Source);
                source.AppendLine();
            }

            source.AppendLine("    [System.Runtime.CompilerServices.CompilerGenerated]");
            source.Append("    public static class ");
            source.AppendLine(serializerName);
            source.AppendLine("    {");
            source.Append("        public const string VariantTypeFullName = \"");
            source.Append(EscapeString(variantTypeFullName));
            source.AppendLine("\";");
            source.Append("        public const string DisplayName = \"");
            source.Append(EscapeString(data.DisplayName));
            source.AppendLine("\";");
            source.Append("        public const int EncodedKeySize = ");
            source.Append(data.KeyCodec.EncodedSize);
            source.AppendLine(";");
            source.Append("        public const int EncodedValueSize = ");
            source.Append(data.ValueCodec.EncodedSize);
            source.AppendLine(";");
            source.AppendLine("        public const int ScratchStride = 1;");
            source.AppendLine();
            source.Append("        public static void AddToCollection(ref Unity.NetCode.GhostComponentSerializerCollectionData collectionData, ");
            source.AppendLine("ref Unity.Entities.SystemState state)");
            source.AppendLine("        {");
            source.Append("            var variantHash = ");
            source.Append(variantHash);
            source.AppendLine(";");
            source.Append("            ");
            source.Append(serializerType);
            source.AppendLine(".AddToCollection(");
            source.AppendLine("                ref collectionData,");
            source.AppendLine("                ref state,");
            source.AppendLine("                new Unity.Collections.FixedString64Bytes(DisplayName),");
            source.AppendLine("                variantHash,");
            source.Append("                typeof(");
            source.Append(data.MapCodecTypeName);
            source.AppendLine(").FullName,");
            source.Append("                ");
            source.Append(data.PrefabTypeExpression);
            source.AppendLine(",");
            source.Append("                ");
            source.Append(data.SendTypeExpression);
            source.AppendLine(",");
            source.Append("                ");
            source.Append(sendChild);
            source.AppendLine(",");
            source.Append("                ");
            source.Append(data.OwnerSendTypeExpression);
            source.AppendLine(",");
            source.Append("                ");
            source.Append(isDefaultSerializer);
            source.AppendLine(");");
            source.AppendLine("        }");
            source.AppendLine();
            source.AppendLine("        internal static void CopyToSnapshot(");
            source.AppendLine("            System.IntPtr stateData, System.IntPtr snapshotData, int snapshotOffset, int snapshotStride,");
            source.AppendLine("            System.IntPtr componentData, int componentStride, int count)");
            source.AppendLine("        {");
            source.Append("            ");
            source.Append(serializerType);
            source.AppendLine(".CopyToSnapshot(stateData, snapshotData, snapshotOffset, snapshotStride, componentData, componentStride, count);");
            source.AppendLine("        }");
            source.AppendLine();
            source.AppendLine("        internal static void CopyFromSnapshot(");
            source.AppendLine("            System.IntPtr stateData, System.IntPtr snapshotData, int snapshotOffset, int snapshotStride,");
            source.AppendLine("            System.IntPtr componentData, int componentStride, int count)");
            source.AppendLine("        {");
            source.Append("            ");
            source.Append(serializerType);
            source.AppendLine(".CopyFromSnapshot(stateData, snapshotData, snapshotOffset, snapshotStride, componentData, componentStride, count);");
            source.AppendLine("        }");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    [Unity.Burst.BurstCompile]");
            source.AppendLine("    [System.Runtime.CompilerServices.CompilerGenerated]");
            source.AppendLine("    [Unity.Entities.UpdateInGroup(typeof(Unity.NetCode.GhostComponentSerializerCollectionSystemGroup))]");
            source.AppendLine("    [Unity.Entities.CreateAfter(typeof(Unity.NetCode.GhostComponentSerializerCollectionSystemGroup))]");
            source.AppendLine("    [Unity.Entities.CreateBefore(typeof(Unity.NetCode.DefaultVariantSystemGroup))]");
            source.AppendLine("    [Unity.Entities.BakingVersion(true)]");
            source.Append("    public partial struct ");
            source.Append(registrationSystemName);
            source.AppendLine(" : Unity.Entities.ISystem, Unity.NetCode.IGhostComponentSerializerRegistration");
            source.AppendLine("    {");
            source.AppendLine("        public void OnCreate(ref Unity.Entities.SystemState state)");
            source.AppendLine("        {");
            source.Append("            using var builder = new Unity.Entities.EntityQueryBuilder(Unity.Collections.Allocator.Temp)");
            source.AppendLine(".WithAllRW<Unity.NetCode.GhostComponentSerializerCollectionData>();");
            source.AppendLine("            using var query = state.EntityManager.CreateEntityQuery(builder);");
            source.AppendLine("            ref var data = ref query.GetSingletonRW<Unity.NetCode.GhostComponentSerializerCollectionData>().ValueRW;");
            source.AppendLine();
            source.Append("            ");
            source.Append(serializerName);
            source.AppendLine(".AddToCollection(ref data, ref state);");
            source.AppendLine("        }");
            source.AppendLine();
            source.AppendLine("        [Unity.Burst.BurstCompile]");
            source.AppendLine("        public void OnUpdate(ref Unity.Entities.SystemState state)");
            source.AppendLine("        {");
            source.AppendLine("            state.Enabled = false;");
            source.AppendLine("        }");
            source.AppendLine("    }");

            if (data.Namespace.Length != 0)
            {
                source.AppendLine("}");
            }

            source.AppendLine("#endif");
            return SourceText.From(source.ToString(), Encoding.UTF8);
        }

        private static DynamicHashMapValueCodecPlan BuildGeneratedCodecPlan(
            ITypeSymbol typeSymbol,
            string serializerName,
            string role,
            string codecNamespace,
            Dictionary<ITypeSymbol, DynamicHashMapValueCodecPlan> cache,
            List<DynamicHashMapValueCodecPlan> codecPlans,
            List<Diagnostic> diagnostics,
            Location location)
        {
            if (cache.TryGetValue(typeSymbol, out var cached))
            {
                return cached;
            }

            if (!typeSymbol.IsUnmanagedType)
            {
                diagnostics.Add(DynamicHashMapNetCodeDiagnostics.UnsupportedType(typeSymbol, location));
                return null;
            }

            var codecName = serializerName + role + SanitizeTypeName(typeSymbol) + "DynamicGhostValueCodec";
            var codecTypeName = codecNamespace.Length == 0 ? "global::" + codecName : "global::" + codecNamespace + "." + codecName;
            var typeName = typeSymbol.ToDisplayString(QualifiedFormat);
            var plan = CreateGeneratedCodecPlan(
                typeSymbol, codecName, codecTypeName, typeName, serializerName, codecNamespace, cache, codecPlans, diagnostics, location);
            if (plan == null)
            {
                return null;
            }

            cache[typeSymbol] = plan;
            if (plan.Source.Length != 0)
            {
                codecPlans.Add(plan);
            }

            return plan;
        }

        private static DynamicHashMapValueCodecPlan CreateGeneratedCodecPlan(
            ITypeSymbol typeSymbol,
            string codecName,
            string codecTypeName,
            string typeName,
            string serializerName,
            string codecNamespace,
            Dictionary<ITypeSymbol, DynamicHashMapValueCodecPlan> cache,
            List<DynamicHashMapValueCodecPlan> codecPlans,
            List<Diagnostic> diagnostics,
            Location location)
        {
            if (TryGetPrimitiveCodec(typeSymbol, out var primitive))
            {
                return DynamicHashMapValueCodecPlan.Generated(
                    typeSymbol, codecTypeName, primitive.EncodedSize, primitive.SchemaHash, GeneratePrimitiveCodecSource(codecName, typeName, primitive));
            }

            if (typeSymbol is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType &&
                TryGetPrimitiveCodec(enumType.EnumUnderlyingType, out var enumPrimitive))
            {
                var enumSchemaHash = CombineFnv1A64(
                    Fnv1A64("enum:" + typeSymbol.ToDisplayString(QualifiedFormat)), enumPrimitive.SchemaHash);
                return DynamicHashMapValueCodecPlan.Generated(
                    typeSymbol,
                    codecTypeName,
                    enumPrimitive.EncodedSize,
                    enumSchemaHash,
                    GenerateEnumCodecSource(codecName, typeName, enumType.EnumUnderlyingType.ToDisplayString(QualifiedFormat), enumPrimitive, enumSchemaHash));
            }

            if (IsUnsupportedGeneratedType(typeSymbol) || typeSymbol.TypeKind != TypeKind.Struct)
            {
                diagnostics.Add(DynamicHashMapNetCodeDiagnostics.UnsupportedType(typeSymbol, location));
                return null;
            }

            if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
            {
                diagnostics.Add(DynamicHashMapNetCodeDiagnostics.UnsupportedType(typeSymbol, location));
                return null;
            }

            if (HasExplicitLayout(namedTypeSymbol))
            {
                diagnostics.Add(DynamicHashMapNetCodeDiagnostics.ExplicitLayout(typeSymbol, typeSymbol.Locations.FirstOrDefault() ?? location));
                return null;
            }

            var instanceFields = namedTypeSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(static field => !field.IsStatic && !field.IsConst)
                .ToArray();

            if (instanceFields.Length == 0)
            {
                diagnostics.Add(DynamicHashMapNetCodeDiagnostics.UnsupportedType(typeSymbol, location));
                return null;
            }

            foreach (var field in instanceFields)
            {
                if (field.IsImplicitlyDeclared)
                {
                    var property = field.AssociatedSymbol as IPropertySymbol;
                    diagnostics.Add(DynamicHashMapNetCodeDiagnostics.UnsupportedStorage(
                        typeSymbol,
                        property?.Name ?? field.Name,
                        property?.Locations.FirstOrDefault() ?? field.Locations.FirstOrDefault() ?? location));
                    return null;
                }

                if (HasFieldOffset(field))
                {
                    diagnostics.Add(DynamicHashMapNetCodeDiagnostics.ExplicitLayout(
                        typeSymbol, field.Locations.FirstOrDefault() ?? typeSymbol.Locations.FirstOrDefault() ?? location));
                    return null;
                }

                if (field.IsFixedSizeBuffer)
                {
                    diagnostics.Add(DynamicHashMapNetCodeDiagnostics.UnsupportedField(
                        typeSymbol, field, field.Locations.FirstOrDefault() ?? location));
                    return null;
                }

                if (field.DeclaredAccessibility != Accessibility.Public || field.IsReadOnly)
                {
                    diagnostics.Add(DynamicHashMapNetCodeDiagnostics.UnsupportedField(typeSymbol, field, field.Locations.FirstOrDefault() ?? location));
                    return null;
                }
            }

            var fields = instanceFields
                .OrderBy(static field => field.Locations.Length == 0 ? int.MaxValue : field.Locations[0].SourceSpan.Start)
                .ToArray();

            var fieldPlans = new List<DynamicHashMapFieldCodecPlan>(fields.Length);
            var encodedSize = 0;
            var schemaHash = Fnv1A64("struct:" + typeSymbol.ToDisplayString(QualifiedFormat));
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var fieldPlan = BuildGeneratedCodecPlan(field.Type, serializerName, "Field" + i, codecNamespace, cache, codecPlans, diagnostics, location);
                if (fieldPlan == null)
                {
                    return null;
                }

                fieldPlans.Add(new DynamicHashMapFieldCodecPlan(field, fieldPlan, encodedSize));
                encodedSize += fieldPlan.EncodedSize;
                schemaHash = CombineFnv1A64(schemaHash, Fnv1A64(field.Name));
                schemaHash = CombineFnv1A64(schemaHash, fieldPlan.SchemaHash);
            }

            var source = GenerateStructCodecSource(codecName, typeName, encodedSize, schemaHash, fieldPlans);
            return DynamicHashMapValueCodecPlan.Generated(typeSymbol, codecTypeName, encodedSize, schemaHash, source);
        }

        private static string GeneratePrimitiveCodecSource(string codecName, string typeName, PrimitiveCodec primitive)
        {
            var source = new StringBuilder();
            AppendCodecHeader(source, codecName, typeName, primitive.EncodedSize, primitive.SchemaHash);
            source.AppendLine("        {");
            source.Append("            global::BovineLabs.Core.Iterators.DynamicGhostPrimitiveCodec.");
            source.Append(primitive.WriteMethod);
            source.Append("(destination, value");
            if (primitive.EncodeCast.Length != 0)
            {
                source.Insert(source.Length - "value".Length, primitive.EncodeCast + "(");
                source.Append(")");
            }

            source.AppendLine(");");
            source.AppendLine("        }");
            source.AppendLine();
            source.Append("        public void Decode(ref global::BovineLabs.Core.Iterators.DynamicGhostDecodeContext context, byte* source, out ");
            source.Append(typeName);
            source.AppendLine(" value)");
            source.AppendLine("        {");
            source.Append("            value = ");
            if (primitive.DecodeCast.Length != 0)
            {
                source.Append("(");
                source.Append(typeName);
                source.Append(")");
            }

            source.Append("global::BovineLabs.Core.Iterators.DynamicGhostPrimitiveCodec.");
            source.Append(primitive.ReadMethod);
            source.AppendLine("(source);");
            source.AppendLine("        }");
            source.AppendLine("    }");
            return source.ToString();
        }

        private static string GenerateEnumCodecSource(
            string codecName, string typeName, string underlyingTypeName, PrimitiveCodec primitive, ulong schemaHash)
        {
            var source = new StringBuilder();
            AppendCodecHeader(source, codecName, typeName, primitive.EncodedSize, schemaHash);
            source.AppendLine("        {");
            source.Append("            global::BovineLabs.Core.Iterators.DynamicGhostPrimitiveCodec.");
            source.Append(primitive.WriteMethod);
            source.Append("(destination, (");
            source.Append(underlyingTypeName);
            source.AppendLine(")value);");
            source.AppendLine("        }");
            source.AppendLine();
            source.Append("        public void Decode(ref global::BovineLabs.Core.Iterators.DynamicGhostDecodeContext context, byte* source, out ");
            source.Append(typeName);
            source.AppendLine(" value)");
            source.AppendLine("        {");
            source.Append("            value = (");
            source.Append(typeName);
            source.Append(")global::BovineLabs.Core.Iterators.DynamicGhostPrimitiveCodec.");
            source.Append(primitive.ReadMethod);
            source.AppendLine("(source);");
            source.AppendLine("        }");
            source.AppendLine("    }");
            return source.ToString();
        }

        private static string GenerateStructCodecSource(
            string codecName,
            string typeName,
            int encodedSize,
            ulong schemaHash,
            IReadOnlyList<DynamicHashMapFieldCodecPlan> fieldPlans)
        {
            var source = new StringBuilder();
            AppendCodecHeader(source, codecName, typeName, encodedSize, schemaHash);
            source.AppendLine("        {");
            for (var i = 0; i < fieldPlans.Count; i++)
            {
                var field = fieldPlans[i];
                var fieldName = EscapeIdentifier(field.Field.Name);
                source.Append("            var field");
                source.Append(i);
                source.Append(" = value.");
                source.Append(fieldName);
                source.AppendLine(";");
                source.Append("            var field");
                source.Append(i);
                source.AppendLine("Codec = default(" + field.Codec.CodecTypeName + ");");
                source.Append("            field");
                source.Append(i);
                source.Append("Codec.Encode(ref context, in field");
                source.Append(i);
                source.Append(", destination + ");
                source.Append(field.Offset);
                source.AppendLine(");");
            }

            source.AppendLine("        }");
            source.AppendLine();
            source.Append("        public void Decode(ref global::BovineLabs.Core.Iterators.DynamicGhostDecodeContext context, byte* source, out ");
            source.Append(typeName);
            source.AppendLine(" value)");
            source.AppendLine("        {");
            source.AppendLine("            value = default;");
            for (var i = 0; i < fieldPlans.Count; i++)
            {
                var field = fieldPlans[i];
                source.Append("            var field");
                source.Append(i);
                source.Append("Codec = default(");
                source.Append(field.Codec.CodecTypeName);
                source.AppendLine(");");
                source.Append("            field");
                source.Append(i);
                source.Append("Codec.Decode(ref context, source + ");
                source.Append(field.Offset);
                source.Append(", out value.");
                source.Append(EscapeIdentifier(field.Field.Name));
                source.AppendLine(");");
            }

            source.AppendLine("        }");
            source.AppendLine("    }");
            return source.ToString();
        }

        private static void AppendCodecHeader(StringBuilder source, string codecName, string typeName, int encodedSize, ulong schemaHash)
        {
            source.AppendLine("    [System.Runtime.CompilerServices.CompilerGenerated]");
            source.Append("    internal unsafe struct ");
            source.Append(codecName);
            source.Append(" : global::BovineLabs.Core.Iterators.IDynamicGhostValueCodec<");
            source.Append(typeName);
            source.AppendLine(">");
            source.AppendLine("    {");
            source.Append("        public int EncodedSize => ");
            source.Append(encodedSize);
            source.AppendLine(";");
            source.Append("        public ulong SchemaHash => 0x");
            source.Append(schemaHash.ToString("X16"));
            source.AppendLine("UL;");
            source.AppendLine();
            source.Append("        public void Encode(ref global::BovineLabs.Core.Iterators.DynamicGhostEncodeContext context, in ");
            source.Append(typeName);
            source.AppendLine(" value, byte* destination)");
        }

        private static bool TryGetPrimitiveCodec(ITypeSymbol typeSymbol, out PrimitiveCodec codec)
        {
            switch (typeSymbol.SpecialType)
            {
                case SpecialType.System_Boolean:
                    codec = new PrimitiveCodec(1, "WriteBool", "ReadBool", string.Empty, string.Empty, "bool");
                    return true;
                case SpecialType.System_Byte:
                    codec = new PrimitiveCodec(1, "WriteByte", "ReadByte", string.Empty, string.Empty, "byte");
                    return true;
                case SpecialType.System_SByte:
                    codec = new PrimitiveCodec(1, "WriteSByte", "ReadSByte", string.Empty, string.Empty, "sbyte");
                    return true;
                case SpecialType.System_Char:
                    codec = new PrimitiveCodec(2, "WriteChar", "ReadChar", string.Empty, string.Empty, "char");
                    return true;
                case SpecialType.System_Int16:
                    codec = new PrimitiveCodec(2, "WriteInt16", "ReadInt16", string.Empty, string.Empty, "short");
                    return true;
                case SpecialType.System_UInt16:
                    codec = new PrimitiveCodec(2, "WriteUInt16", "ReadUInt16", string.Empty, string.Empty, "ushort");
                    return true;
                case SpecialType.System_Int32:
                    codec = new PrimitiveCodec(4, "WriteInt32", "ReadInt32", string.Empty, string.Empty, "int");
                    return true;
                case SpecialType.System_UInt32:
                    codec = new PrimitiveCodec(4, "WriteUInt32", "ReadUInt32", string.Empty, string.Empty, "uint");
                    return true;
                case SpecialType.System_Int64:
                    codec = new PrimitiveCodec(8, "WriteInt64", "ReadInt64", string.Empty, string.Empty, "long");
                    return true;
                case SpecialType.System_UInt64:
                    codec = new PrimitiveCodec(8, "WriteUInt64", "ReadUInt64", string.Empty, string.Empty, "ulong");
                    return true;
                case SpecialType.System_Single:
                    codec = new PrimitiveCodec(4, "WriteFloat32", "ReadFloat32", string.Empty, string.Empty, "float");
                    return true;
                case SpecialType.System_Double:
                    codec = new PrimitiveCodec(8, "WriteFloat64", "ReadFloat64", string.Empty, string.Empty, "double");
                    return true;
                default:
                    codec = default;
                    return false;
            }
        }

        private static bool IsUnsupportedGeneratedType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is IPointerTypeSymbol || typeSymbol.TypeKind == TypeKind.Pointer || typeSymbol.TypeKind == TypeKind.FunctionPointer)
            {
                return true;
            }

            var display = typeSymbol.ToDisplayString();
            return display == "Unity.Entities.Entity" ||
                display.StartsWith("Unity.Entities.BlobAssetReference<", StringComparison.Ordinal) ||
                display.StartsWith("Unity.Collections.Native", StringComparison.Ordinal);
        }

        private static bool HasExplicitLayout(INamedTypeSymbol typeSymbol)
        {
            foreach (var attribute in typeSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != StructLayoutAttributeMetadataName || attribute.ConstructorArguments.Length == 0)
                {
                    continue;
                }

                var value = attribute.ConstructorArguments[0].Value;
                if (value != null && Convert.ToInt32(value) == LayoutKindExplicit)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasFieldOffset(IFieldSymbol fieldSymbol)
        {
            foreach (var attribute in fieldSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == FieldOffsetAttributeMetadataName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDynamicHashMapGhostSerializerAttribute(AttributeData attribute)
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();
            return attributeName == AttributeMetadataName;
        }

        private static DynamicHashMapCollectionKind GetCollectionKind(INamedTypeSymbol mapInterface)
        {
            return mapInterface.OriginalDefinition.Name == "IDynamicMultiHashMap"
                ? DynamicHashMapCollectionKind.MultiHashMap
                : DynamicHashMapCollectionKind.HashMap;
        }

        private static string GetSerializerSuffix(DynamicHashMapCollectionKind collectionKind)
        {
            return collectionKind == DynamicHashMapCollectionKind.HashMap ? "DynamicHashMap" : "DynamicMultiHashMap";
        }

        private static IReadOnlyList<INamedTypeSymbol> GetDynamicHashMapInterfaces(INamedTypeSymbol typeSymbol, CancellationToken cancellationToken)
        {
            var matches = new List<INamedTypeSymbol>();
            foreach (var interfaceSymbol in typeSymbol.AllInterfaces)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var original = interfaceSymbol.OriginalDefinition;
                if ((original.Name == "IDynamicHashMap" || original.Name == "IDynamicMultiHashMap") && original.TypeParameters.Length == 2 &&
                    original.ContainingNamespace.ToDisplayString() == "BovineLabs.Core.Iterators")
                {
                    matches.Add(interfaceSymbol);
                }
            }

            return matches;
        }

        private static bool HasValidMarkerStorage(INamedTypeSymbol typeSymbol, INamedTypeSymbol mapInterface)
        {
            var instanceFields = typeSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(static field => !field.IsStatic && !field.IsConst)
                .ToArray();
            if (instanceFields.Length != 1)
            {
                return false;
            }

            var field = instanceFields[0];
            if (!field.IsImplicitlyDeclared || field.Type.SpecialType != SpecialType.System_Byte ||
                field.AssociatedSymbol is not IPropertySymbol property)
            {
                return false;
            }

            return IsDynamicHashMapValueImplementation(property, mapInterface);
        }

        private static bool IsDynamicHashMapValueImplementation(IPropertySymbol property, INamedTypeSymbol mapInterface)
        {
            if (property.Type.SpecialType != SpecialType.System_Byte)
            {
                return false;
            }

            var mapValueProperty = mapInterface.GetMembers("Value").OfType<IPropertySymbol>().FirstOrDefault();
            if (mapValueProperty == null)
            {
                return false;
            }

            foreach (var explicitImplementation in property.ExplicitInterfaceImplementations)
            {
                if (SymbolEqualityComparer.Default.Equals(explicitImplementation, mapValueProperty) ||
                    SymbolEqualityComparer.Default.Equals(explicitImplementation.OriginalDefinition, mapValueProperty.OriginalDefinition))
                {
                    return true;
                }
            }

            return property.Name == "Value" &&
                property.DeclaredAccessibility == Accessibility.Public &&
                property.Parameters.Length == 0;
        }

        private static GhostDynamicHashMapCodecMode GetCodecMode(AttributeData attribute)
        {
            var result = GhostDynamicHashMapCodecMode.Generated;
            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == "CodecMode" && argument.Value.Value != null)
                {
                    result = (GhostDynamicHashMapCodecMode)Convert.ToInt32(argument.Value.Value);
                }
            }

            return result;
        }

        private static bool TryGetRawSize(ITypeSymbol typeSymbol, out int size)
        {
            if (typeSymbol is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
            {
                return TryGetRawSize(enumType.EnumUnderlyingType, out size);
            }

            switch (typeSymbol.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                    size = 1;
                    return true;
                case SpecialType.System_Char:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                    size = 2;
                    return true;
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Single:
                    size = 4;
                    return true;
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Double:
                    size = 8;
                    return true;
                default:
                    size = 0;
                    return false;
            }
        }

        private static bool GetNamedBool(AttributeData attribute, string name, bool defaultValue)
        {
            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == name && argument.Value.Value is bool value)
                {
                    return value;
                }
            }

            return defaultValue;
        }

        private static string GetNamedString(AttributeData attribute, string name, string defaultValue)
        {
            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == name)
                {
                    return argument.Value.Value as string ?? defaultValue;
                }
            }

            return defaultValue;
        }

        private static string GetNamedEnumExpression(AttributeData attribute, string name, string typeName, string defaultName)
        {
            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == name && argument.Value.Value != null)
                {
                    return $"({typeName}){Convert.ToInt32(argument.Value.Value)}";
                }
            }

            return $"{typeName}.{defaultName}";
        }

        private static string EscapeString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string EscapeIdentifier(string identifier)
        {
            return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ||
                SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
                    ? "@" + identifier
                    : identifier;
        }

        private static string SanitizeTypeName(ITypeSymbol typeSymbol)
        {
            var text = typeSymbol.ToDisplayString(QualifiedFormat);
            var builder = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                builder.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            return builder.ToString().Replace("global__", string.Empty);
        }

        private static ulong Fnv1A64(string text)
        {
            var result = 14695981039346656037UL;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                result = 1099511628211UL * (result ^ (byte)(c & 0xff));
                result = 1099511628211UL * (result ^ (byte)(c >> 8));
            }

            return result;
        }

        private static ulong CombineFnv1A64(ulong hash, ulong value)
        {
            return (hash ^ value) * 1099511628211UL;
        }

        private enum GhostDynamicHashMapCodecMode
        {
            Generated,
            RawStable,
        }

        private enum DynamicHashMapCollectionKind
        {
            HashMap,
            MultiHashMap,
        }

        private readonly struct PrimitiveCodec
        {
            public PrimitiveCodec(int encodedSize, string writeMethod, string readMethod, string encodeCast, string decodeCast, string schemaName)
            {
                this.EncodedSize = encodedSize;
                this.WriteMethod = writeMethod;
                this.ReadMethod = readMethod;
                this.EncodeCast = encodeCast;
                this.DecodeCast = decodeCast;
                this.SchemaHash = Fnv1A64("primitive:" + schemaName);
            }

            public int EncodedSize { get; }

            public string WriteMethod { get; }

            public string ReadMethod { get; }

            public string EncodeCast { get; }

            public string DecodeCast { get; }

            public ulong SchemaHash { get; }
        }

        private sealed class DynamicHashMapNetCodeCandidate
        {
            public DynamicHashMapNetCodeCandidate(TypeDeclarationSyntax typeSyntax, INamedTypeSymbol typeSymbol, AttributeData attribute)
            {
                this.TypeSyntax = typeSyntax;
                this.TypeSymbol = typeSymbol;
                this.Attribute = attribute;
            }

            public TypeDeclarationSyntax TypeSyntax { get; }

            public INamedTypeSymbol TypeSymbol { get; }

            public AttributeData Attribute { get; }
        }

        private sealed class DynamicHashMapValueCodecPlan
        {
            private DynamicHashMapValueCodecPlan(ITypeSymbol typeSymbol, string codecTypeName, int encodedSize, ulong schemaHash, string source)
            {
                this.TypeSymbol = typeSymbol;
                this.CodecTypeName = codecTypeName;
                this.EncodedSize = encodedSize;
                this.SchemaHash = schemaHash;
                this.Source = source;
            }

            public ITypeSymbol TypeSymbol { get; }

            public string CodecTypeName { get; }

            public int EncodedSize { get; }

            public ulong SchemaHash { get; }

            public string Source { get; }

            public static DynamicHashMapValueCodecPlan Raw(ITypeSymbol typeSymbol, int encodedSize)
            {
                var typeName = typeSymbol.ToDisplayString(QualifiedFormat);
                var codecTypeName = "global::BovineLabs.Core.Iterators.DynamicGhostRawValueCodec<" + typeName + ">";
                var schemaHash = CombineFnv1A64(Fnv1A64("raw:" + typeName), (ulong)encodedSize);
                return new DynamicHashMapValueCodecPlan(typeSymbol, codecTypeName, encodedSize, schemaHash, string.Empty);
            }

            public static DynamicHashMapValueCodecPlan Generated(
                ITypeSymbol typeSymbol, string codecTypeName, int encodedSize, ulong schemaHash, string source)
            {
                return new DynamicHashMapValueCodecPlan(typeSymbol, codecTypeName, encodedSize, schemaHash, source);
            }
        }

        private sealed class DynamicHashMapFieldCodecPlan
        {
            public DynamicHashMapFieldCodecPlan(IFieldSymbol field, DynamicHashMapValueCodecPlan codec, int offset)
            {
                this.Field = field;
                this.Codec = codec;
                this.Offset = offset;
            }

            public IFieldSymbol Field { get; }

            public DynamicHashMapValueCodecPlan Codec { get; }

            public int Offset { get; }
        }

        private sealed class DynamicHashMapNetCodeData
        {
            public DynamicHashMapNetCodeData(
                DynamicHashMapCollectionKind collectionKind,
                INamedTypeSymbol typeSymbol,
                ITypeSymbol keyType,
                ITypeSymbol valueType,
                DynamicHashMapValueCodecPlan keyCodec,
                DynamicHashMapValueCodecPlan valueCodec,
                string mapCodecTypeName,
                IReadOnlyList<DynamicHashMapValueCodecPlan> generatedCodecs,
                bool useRawSerializerPath,
                bool isDefault,
                string displayName,
                bool sendDataForChildEntity,
                string prefabTypeExpression,
                string sendTypeExpression,
                string ownerSendTypeExpression)
            {
                this.CollectionKind = collectionKind;
                this.SerializerSuffix = GetSerializerSuffix(collectionKind);
                this.TypeSymbol = typeSymbol;
                this.Namespace = typeSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : typeSymbol.ContainingNamespace.ToDisplayString();
                this.TypeName = typeSymbol.ToDisplayString(QualifiedFormat);
                this.KeyTypeName = keyType.ToDisplayString(QualifiedFormat);
                this.ValueTypeName = valueType.ToDisplayString(QualifiedFormat);
                this.KeyCodec = keyCodec;
                this.ValueCodec = valueCodec;
                this.MapCodecTypeName = mapCodecTypeName;
                this.GeneratedCodecs = generatedCodecs.Where(static codec => codec.Source.Length != 0).Distinct().ToArray();
                this.UseRawSerializerPath = useRawSerializerPath;
                this.IsDefault = isDefault;
                this.DisplayName = displayName;
                this.SendDataForChildEntity = sendDataForChildEntity;
                this.PrefabTypeExpression = prefabTypeExpression;
                this.SendTypeExpression = sendTypeExpression;
                this.OwnerSendTypeExpression = ownerSendTypeExpression;
                this.HintName = typeSymbol.ToDisplayString(QualifiedFormat)
                    .Replace("global::", string.Empty)
                    .Replace(".", "_")
                    .Replace("<", "_")
                    .Replace(">", "_")
                    .Replace(",", "_")
                    .Replace(" ", string.Empty) + "." + this.SerializerSuffix + "GhostSerializer.g.cs";
            }

            public DynamicHashMapCollectionKind CollectionKind { get; }

            public string SerializerSuffix { get; }

            public INamedTypeSymbol TypeSymbol { get; }

            public string Namespace { get; }

            public string TypeName { get; }

            public string KeyTypeName { get; }

            public string ValueTypeName { get; }

            public DynamicHashMapValueCodecPlan KeyCodec { get; }

            public DynamicHashMapValueCodecPlan ValueCodec { get; }

            public string MapCodecTypeName { get; }

            public IReadOnlyList<DynamicHashMapValueCodecPlan> GeneratedCodecs { get; }

            public bool UseRawSerializerPath { get; }

            public bool IsDefault { get; }

            public string DisplayName { get; }

            public bool SendDataForChildEntity { get; }

            public string PrefabTypeExpression { get; }

            public string SendTypeExpression { get; }

            public string OwnerSendTypeExpression { get; }

            public string HintName { get; }
        }

        private sealed class DynamicHashMapNetCodeResult
        {
            public DynamicHashMapNetCodeResult(DynamicHashMapNetCodeData data, IReadOnlyList<Diagnostic> diagnostics)
            {
                this.Data = data;
                this.Diagnostics = diagnostics;
            }

            public DynamicHashMapNetCodeData Data { get; }

            public IReadOnlyList<Diagnostic> Diagnostics { get; }
        }
    }
}
