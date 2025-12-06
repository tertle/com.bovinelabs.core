// <copyright file="InputActionGenerator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.InputGenerator
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
    public class InputActionGenerator : IIncrementalGenerator
    {
        private const string Bool = "bool";
        private const string Float = "float";
        private const string Float2 = "float2";
        private const string Half = "half";
        private const string ButtonState = "ButtonState";
        private const string InputEvent = "InputEvent";

        internal static readonly SymbolDisplayFormat ShortTypeFormat =
            SymbolDisplayFormat.MinimallyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var symbolsProvider = context.CompilationProvider.Select((c, _) => InputSymbols.Create(c));

            var candidates = context
                .SyntaxProvider
                .CreateSyntaxProvider(predicate: IsSyntaxTargetForGeneration, transform: GetCandidate)
                .Where(static t => t != null);

            var inputs = candidates
                .Combine(symbolsProvider)
                .Select((pair, cancellationToken) => GetSemanticTargetForGeneration(pair.Left, pair.Right, cancellationToken))
                .Where(static r => r != null);

            context.RegisterSourceOutput(inputs, static (ctx, result) => Execute(ctx, result));
        }

        private static void Execute(SourceProductionContext context, InputResult result)
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            if (result.Data == null)
            {
                return;
            }

            var builder = ProcessStruct(result.Data);
            if (builder == null)
            {
                return;
            }

            try
            {
                context.AddSource(builder);
            }
            catch (Exception ex)
            {
                SourceGenHelpers.Log(ex.ToString());
            }
        }

        private static ClassBuilder ProcessStruct(Data data)
        {
            try
            {
                var builder = CodeBuilder
                    .Create(data.Symbol)
                    .AddNamespaceImport("System")
                    .AddNamespaceImport("BovineLabs.Core.Input")
                    .AddNamespaceImport("JetBrains.Annotations")
                    .AddNamespaceImport("Unity.Collections")
                    .AddNamespaceImport("Unity.Entities")
                    .AddNamespaceImport("Unity.Mathematics")
                    .AddNamespaceImport("UnityEngine")
                    .AddNamespaceImport("UnityEngine.InputSystem");

                if (data.HasInputEvent)
                {
                    builder.AddNamespaceImport("Unity.NetCode");
                }

                GenerateSystem(builder, data);
                GenerateSettings(builder, data);

                return builder;
            }
            catch (Exception ex)
            {
                SourceGenHelpers.Log(ex.ToString());
                return null;
            }
        }

        private static void GenerateSystem(ClassBuilder source, Data data)
        {
            var hasDelta = data.Fields.Any(f => f.AttributeType == AttributeType.InputActionDelta);

            var builder = source
                .AddNestedClass("System", true, Accessibility.Private)
                .SetBaseClass("SystemBase")
                .AddAttribute("UpdateInGroup(typeof(InputSystemGroup))");

            builder.AddProperty("query", Accessibility.Private).SetType("EntityQuery").WithValue(null);
            builder.AddProperty("input", Accessibility.Private).SetType(data.Symbol.Name).WithValue(null);

            if (hasDelta)
            {
                builder.AddProperty("deltaTime", Accessibility.Private).SetType<float>().WithValue(null);
            }

            builder
                .AddMethod("OnCreate", Accessibility.Protected)
                .Override()
                .WithBody(body =>
                {
                    body.AppendLine(
                        $"this.query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<{data.Symbol.Name}>(){(data.IsNetCode ? ".WithAll<Unity.NetCode.GhostOwnerIsLocal>()" : string.Empty)}.Build(this);");
                    body.AppendLine("this.RequireForUpdate(this.query);");
                });

            builder
                .AddMethod("OnUpdate", Accessibility.Protected)
                .Override()
                .WithBody(body =>
                {
                    if (hasDelta)
                    {
                        body.AppendLine("this.deltaTime = this.World.Time.DeltaTime;");
                    }

                    body.AppendLine("this.query.CompleteDependency();");
                    body.AppendLine("this.query.SetSingleton(this.input);");

                    foreach (var fieldSymbol in data.Fields)
                    {
                        if (fieldSymbol.Type == ButtonState)
                        {
                            body.AppendLine($"this.input.{fieldSymbol.Name}.Reset();");
                        }
                        else if (fieldSymbol.AttributeType is AttributeType.Down or AttributeType.Up)
                        {
                            body.AppendLine($"this.input.{fieldSymbol.Name} = default;");
                        }
                    }
                });

            builder
                .AddMethod("OnStartRunning", Accessibility.Protected)
                .Override()
                .WithBody(body =>
                {
                    body
                        .If("!InputCommonSettings.I.TryGetSettings<Settings>(out var actions)")
                        .WithBody(i =>
                        {
                            i.AppendLine("UnityEngine.Debug.LogError(\"ActionsGenerated has not been created. Make sure you update InputCommonSettings.\");");
                            i.AppendLine("return;");
                        })
                        .EndIf();

                    body.NewLine();

                    foreach (var fieldSymbol in data.Fields)
                    {
                        body
                            .If($"actions.{fieldSymbol.Name} != null")
                            .WithBody(b =>
                            {
                                if (IsUpDown(fieldSymbol))
                                {
                                    b.AppendLine(fieldSymbol.AttributeType == AttributeType.Down
                                        ? $"actions.{fieldSymbol.Name}.action.started += this.On{fieldSymbol.Name}Started;"
                                        : $"actions.{fieldSymbol.Name}.action.canceled += this.On{fieldSymbol.Name}Canceled;");
                                }
                                else if (IsButton(fieldSymbol))
                                {
                                    b.AppendLine($"actions.{fieldSymbol.Name}.action.started += this.On{fieldSymbol.Name}Started;");
                                    b.AppendLine($"actions.{fieldSymbol.Name}.action.canceled += this.On{fieldSymbol.Name}Canceled;");
                                }
                                else
                                {
                                    b.AppendLine($"actions.{fieldSymbol.Name}.action.performed += this.On{fieldSymbol.Name}Performed;");
                                    b.AppendLine($"actions.{fieldSymbol.Name}.action.canceled += this.On{fieldSymbol.Name}Canceled;");
                                }
                            })
                            .Else(e =>
                            {
                                e.AppendLine($"BovineLabs.Core.BLGlobalLogger.LogWarningString(\"InputActionReference for {data.Symbol.Name}.{fieldSymbol.Name} has not been assigned.\");");
                            })
                            .EndIf();
                    }
                });

            builder
                .AddMethod("OnStopRunning", Accessibility.Protected)
                .Override()
                .WithBody(sr =>
                {
                    sr
                        .If("InputCommonSettings.I.TryGetSettings<Settings>(out var actions)")
                        .WithBody(body =>
                        {
                            foreach (var fieldSymbol in data.Fields)
                            {
                                body
                                    .If($"actions.{fieldSymbol.Name} != null")
                                    .WithBody(b =>
                                    {
                                        if (IsUpDown(fieldSymbol))
                                        {
                                            b.AppendLine(fieldSymbol.AttributeType == AttributeType.Down
                                                ? $"actions.{fieldSymbol.Name}.action.started -= this.On{fieldSymbol.Name}Started;"
                                                : $"actions.{fieldSymbol.Name}.action.canceled -= this.On{fieldSymbol.Name}Canceled;");
                                        }
                                        else if (IsButton(fieldSymbol))
                                        {
                                            b.AppendLine($"actions.{fieldSymbol.Name}.action.started -= this.On{fieldSymbol.Name}Started;");
                                            b.AppendLine($"actions.{fieldSymbol.Name}.action.canceled -= this.On{fieldSymbol.Name}Canceled;");
                                        }
                                        else
                                        {
                                            b.AppendLine($"actions.{fieldSymbol.Name}.action.performed -= this.On{fieldSymbol.Name}Performed;");
                                            b.AppendLine($"actions.{fieldSymbol.Name}.action.canceled -= this.On{fieldSymbol.Name}Canceled;");
                                        }
                                    })
                                    .EndIf();
                            }
                        })
                        .EndIf();
                });

            foreach (var fieldSymbol in data.Fields)
            {
                if (IsUpDown(fieldSymbol))
                {
                    if (fieldSymbol.AttributeType == AttributeType.Down)
                    {
                        if (fieldSymbol.Type == InputEvent)
                        {
                            builder
                                .AddMethod($"On{fieldSymbol.Name}Started", Accessibility.Private)
                                .AddParameter("InputAction.CallbackContext", "context")
                                .WithBody(b => b.AppendLine($"this.input.{fieldSymbol.Name}.Set();"));
                        }
                        else if (fieldSymbol.Type == Bool)
                        {
                            builder
                                .AddMethod($"On{fieldSymbol.Name}Started", Accessibility.Private)
                                .AddParameter("InputAction.CallbackContext", "context")
                                .WithBody(b => b.AppendLine($"this.input.{fieldSymbol.Name} = true;"));
                        }
                    }
                    else
                    {
                        if (fieldSymbol.Type == InputEvent)
                        {
                            builder
                                .AddMethod($"On{fieldSymbol.Name}Canceled", Accessibility.Private)
                                .AddParameter("InputAction.CallbackContext", "context")
                                .WithBody(b => b.AppendLine($"this.input.{fieldSymbol.Name}.Set();"));
                        }
                        else if (fieldSymbol.Type == Bool)
                        {
                            builder
                                .AddMethod($"On{fieldSymbol.Name}Canceled", Accessibility.Private)
                                .AddParameter("InputAction.CallbackContext", "context")
                                .WithBody(b => b.AppendLine($"this.input.{fieldSymbol.Name} = true;"));
                        }
                    }
                }
                else
                {
                    if (fieldSymbol.Type == Bool)
                    {
                        builder
                            .AddMethod($"On{fieldSymbol.Name}Started", Accessibility.Private)
                            .AddParameter("InputAction.CallbackContext", "context")
                            .WithBody(b => b.AppendLine($"this.input.{fieldSymbol.Name} = true;"));

                        builder
                            .AddMethod($"On{fieldSymbol.Name}Canceled", Accessibility.Private)
                            .AddParameter("InputAction.CallbackContext", "context")
                            .WithBody(b => b.AppendLine($"this.input.{fieldSymbol.Name} = false;"));
                    }
                    else if (fieldSymbol.Type == ButtonState)
                    {
                        builder
                            .AddMethod($"On{fieldSymbol.Name}Started", Accessibility.Private)
                            .AddParameter("InputAction.CallbackContext", "context")
                            .WithBody(b => b.AppendLine($"this.input.{fieldSymbol.Name}.Started();"));

                        builder
                            .AddMethod($"On{fieldSymbol.Name}Canceled", Accessibility.Private)
                            .AddParameter("InputAction.CallbackContext", "context")
                            .WithBody(b => b.AppendLine($"this.input.{fieldSymbol.Name}.Cancelled();"));
                    }
                    else
                    {
                        var isVector2 = fieldSymbol.Type == Float2;

                        builder
                            .AddMethod($"On{fieldSymbol.Name}Performed", Accessibility.Private)
                            .AddParameter("InputAction.CallbackContext", "context")
                            .WithBody(b => b.AppendLine(
                                $"this.input.{fieldSymbol.Name} = ({fieldSymbol.Type})context.ReadValue<{(isVector2 ? "Vector2" : "float")}>(){(fieldSymbol.AttributeType == AttributeType.InputActionDelta ? " * this.deltaTime" : string.Empty)};"));

                        builder
                            .AddMethod($"On{fieldSymbol.Name}Canceled", Accessibility.Private)
                            .AddParameter("InputAction.CallbackContext", "context")
                            .WithBody(b => b.AppendLine($"this.input.{fieldSymbol.Name} = default;"));
                    }
                }
            }
        }

        private static bool IsButton(FieldData fieldSymbol)
        {
            return fieldSymbol.Type is Bool or ButtonState;
        }

        private static bool IsUpDown(FieldData fieldSymbol)
        {
            return fieldSymbol.Type is Bool or InputEvent && fieldSymbol.AttributeType is AttributeType.Up or AttributeType.Down;
        }

        private static void GenerateSettings(ClassBuilder source, Data data)
        {
            var builder = source
                .AddNestedClass("Settings", Accessibility.Private)
                .AddAttribute("Serializable")
                .AddInterface("IInputSettings")
                .AddProperty("Name", Accessibility.Public)
                .AddAttribute("HideInInspector")
                .AddAttribute("UsedImplicitly")
                .SetType<string>()
                .WithValue($"\"{data.Symbol.Name}\"");

            foreach (var fieldSymbol in data.Fields)
            {
                builder.AddProperty(fieldSymbol.Name, Accessibility.Public).SetType("InputActionReference").WithValue(null);
            }

            builder
                .AddMethod("Bake", Accessibility.Public)
                .AddParameter("IBakerWrapper", "baker")
                .WithBody(b =>
                {
                    if (!data.IsNetCode)
                    {
                        b.AppendLine($"baker.AddComponent(default({data.Symbol.Name}));\n");
                    }
                });
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

                if (baseType.Type is IdentifierNameSyntax { Identifier: { ValueText: "IComponentData" or "IInputComponentData" } })
                {
                    return true;
                }

                if (baseType.Type is QualifiedNameSyntax { Right: IdentifierNameSyntax { Identifier: { ValueText: "IComponentData" or "IInputComponentData" } } })
                {
                    return true;
                }
            }

            return false;
        }

        private static InputCandidate GetCandidate(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
        {
            var typeDeclaration = (TypeDeclarationSyntax)ctx.Node;
            var typeSymbol = ctx.SemanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
            if (typeSymbol == null)
            {
                return null;
            }

            return new InputCandidate(typeDeclaration, typeSymbol);
        }

        private static InputResult GetSemanticTargetForGeneration(InputCandidate candidate, InputSymbols symbols, CancellationToken cancellationToken)
        {
            var typeSymbol = candidate.TypeSymbol;
            var typeSyntax = candidate.TypeSyntax;

            if (!ImplementsInputInterface(typeSymbol, symbols))
            {
                return null;
            }

            var diagnostics = new List<Diagnostic>();

            var fields = new List<FieldData>();
            var hasAnnotatedField = false;

            foreach (var fieldSymbol in typeSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (fieldSymbol.IsStatic)
                {
                    continue;
                }

                var attribute = GetInputActionAttribute(fieldSymbol, symbols);
                if (attribute == AttributeType.None)
                {
                    continue;
                }

                hasAnnotatedField = true;

                var fieldResult = CreateFieldData(fieldSymbol, attribute, symbols);
                if (fieldResult.Diagnostic != null)
                {
                    diagnostics.Add(fieldResult.Diagnostic);
                    continue;
                }

                fields.Add(fieldResult.Field);
            }

            if (!hasAnnotatedField)
            {
                return null;
            }

            if (typeSymbol.TypeKind != TypeKind.Struct)
            {
                diagnostics.Add(InputDiagnostics.NonStruct(typeSymbol, typeSyntax.Identifier.GetLocation()));
                return new InputResult(null, diagnostics);
            }

            if (!typeSyntax.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                diagnostics.Add(InputDiagnostics.MissingPartial(typeSymbol, typeSyntax.Identifier.GetLocation()));
            }

            if (fields.Count == 0)
            {
                diagnostics.Add(InputDiagnostics.NoSupportedFields(typeSymbol, typeSyntax.Identifier.GetLocation()));

                if (diagnostics.Count == 0)
                {
                    return null;
                }

                return new InputResult(null, diagnostics);
            }

            var isNetcode = symbols.InputComponentData != null &&
                typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, symbols.InputComponentData));
            var hasInputEvent = fields.Any(f => f.Type == InputEvent);

            var data = new Data(typeSymbol, fields, isNetcode, hasInputEvent);
            return new InputResult(data, diagnostics);
        }

        private static FieldResult CreateFieldData(IFieldSymbol fieldSymbol, AttributeType attributeType, InputSymbols symbols)
        {
            if (!TryGetFieldType(fieldSymbol, symbols, out var typeName))
            {
                return new FieldResult(default, InputDiagnostics.UnsupportedField(fieldSymbol, fieldSymbol.Locations.FirstOrDefault()));
            }

            if (attributeType is AttributeType.Down or AttributeType.Up && !IsUpDownType(typeName))
            {
                return new FieldResult(default, InputDiagnostics.InvalidUpDown(fieldSymbol, fieldSymbol.Locations.FirstOrDefault()));
            }

            if (attributeType == AttributeType.InputActionDelta && !IsDeltaCompatible(typeName))
            {
                return new FieldResult(default, InputDiagnostics.InvalidDelta(fieldSymbol, fieldSymbol.Locations.FirstOrDefault()));
            }

            return new FieldResult(new FieldData(fieldSymbol.Name, typeName, attributeType), null);
        }

        private static bool ImplementsInputInterface(INamedTypeSymbol typeSymbol, InputSymbols symbols)
        {
            if (symbols.ComponentData == null && symbols.InputComponentData == null)
            {
                return false;
            }

            foreach (var i in typeSymbol.AllInterfaces)
            {
                if (symbols.ComponentData != null && SymbolEqualityComparer.Default.Equals(i, symbols.ComponentData))
                {
                    return true;
                }

                if (symbols.InputComponentData != null && SymbolEqualityComparer.Default.Equals(i, symbols.InputComponentData))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetFieldType(IFieldSymbol fieldSymbol, InputSymbols symbols, out string typeName)
        {
            var type = fieldSymbol.Type;

            if (type.SpecialType == SpecialType.System_Boolean)
            {
                typeName = Bool;
                return true;
            }

            if (type.SpecialType == SpecialType.System_Single)
            {
                typeName = Float;
                return true;
            }

            if (symbols.Float2 != null && SymbolEqualityComparer.Default.Equals(symbols.Float2, type))
            {
                typeName = Float2;
                return true;
            }

            if (symbols.Half != null && SymbolEqualityComparer.Default.Equals(symbols.Half, type))
            {
                typeName = Half;
                return true;
            }

            if (symbols.ButtonState != null && SymbolEqualityComparer.Default.Equals(symbols.ButtonState, type))
            {
                typeName = ButtonState;
                return true;
            }

            if (symbols.InputEvent != null && SymbolEqualityComparer.Default.Equals(symbols.InputEvent, type))
            {
                typeName = InputEvent;
                return true;
            }

            typeName = type.ToDisplayString(ShortTypeFormat);
            return false;
        }

        private static bool IsUpDownType(string type)
        {
            return type is Bool or InputEvent;
        }

        private static bool IsDeltaCompatible(string type)
        {
            return type is Float or Float2 or Half;
        }

        private static AttributeType GetInputActionAttribute(IFieldSymbol field, InputSymbols symbols)
        {
            foreach (var attribute in field.GetAttributes())
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass == null)
                {
                    continue;
                }

                if (symbols.InputActionAttribute != null && SymbolEqualityComparer.Default.Equals(attributeClass, symbols.InputActionAttribute))
                {
                    return AttributeType.InputAction;
                }

                if (symbols.InputActionDeltaAttribute != null && SymbolEqualityComparer.Default.Equals(attributeClass, symbols.InputActionDeltaAttribute))
                {
                    return AttributeType.InputActionDelta;
                }

                if (symbols.InputActionDownAttribute != null && SymbolEqualityComparer.Default.Equals(attributeClass, symbols.InputActionDownAttribute))
                {
                    return AttributeType.Down;
                }

                if (symbols.InputActionUpAttribute != null && SymbolEqualityComparer.Default.Equals(attributeClass, symbols.InputActionUpAttribute))
                {
                    return AttributeType.Up;
                }

                switch (attributeClass.Name)
                {
                    case "InputActionAttribute":
                        return AttributeType.InputAction;
                    case "InputActionDeltaAttribute":
                        return AttributeType.InputActionDelta;
                    case "InputActionDownAttribute":
                        return AttributeType.Down;
                    case "InputActionUpAttribute":
                        return AttributeType.Up;
                }
            }

            return AttributeType.None;
        }

        private enum AttributeType
        {
            None,
            InputAction,
            InputActionDelta,
            Down,
            Up
        }

        private class InputCandidate
        {
            public InputCandidate(TypeDeclarationSyntax typeSyntax, INamedTypeSymbol typeSymbol)
            {
                this.TypeSyntax = typeSyntax;
                this.TypeSymbol = typeSymbol;
            }

            public TypeDeclarationSyntax TypeSyntax { get; }

            public INamedTypeSymbol TypeSymbol { get; }
        }

        private class InputResult
        {
            public InputResult(Data data, IReadOnlyList<Diagnostic> diagnostics)
            {
                this.Data = data;
                this.Diagnostics = diagnostics;
            }

            public Data Data { get; }

            public IReadOnlyList<Diagnostic> Diagnostics { get; }
        }

        private class Data
        {
            public Data(INamedTypeSymbol symbol, IReadOnlyList<FieldData> fields, bool isNetCode, bool hasInputEvent)
            {
                this.Symbol = symbol;
                this.Fields = fields;
                this.IsNetCode = isNetCode;
                this.HasInputEvent = hasInputEvent;
            }

            public INamedTypeSymbol Symbol { get; }

            public IReadOnlyList<FieldData> Fields { get; }

            public bool IsNetCode { get; }

            public bool HasInputEvent { get; }
        }

        private class FieldResult
        {
            public FieldResult(FieldData field, Diagnostic diagnostic)
            {
                this.Field = field;
                this.Diagnostic = diagnostic;
            }

            public FieldData Field { get; }

            public Diagnostic Diagnostic { get; }
        }

        private struct FieldData
        {
            public FieldData(string name, string type, AttributeType attributeType)
            {
                this.Name = name;
                this.Type = type;
                this.AttributeType = attributeType;
            }

            public string Name { get; }

            public string Type { get; }

            public AttributeType AttributeType { get; }
        }

        private class InputSymbols
        {
            public InputSymbols(
                INamedTypeSymbol componentData,
                INamedTypeSymbol inputComponentData,
                ITypeSymbol float2,
                ITypeSymbol half,
                ITypeSymbol buttonState,
                ITypeSymbol inputEvent,
                INamedTypeSymbol inputActionAttribute,
                INamedTypeSymbol inputActionDeltaAttribute,
                INamedTypeSymbol inputActionDownAttribute,
                INamedTypeSymbol inputActionUpAttribute)
            {
                this.ComponentData = componentData;
                this.InputComponentData = inputComponentData;
                this.Float2 = float2;
                this.Half = half;
                this.ButtonState = buttonState;
                this.InputEvent = inputEvent;
                this.InputActionAttribute = inputActionAttribute;
                this.InputActionDeltaAttribute = inputActionDeltaAttribute;
                this.InputActionDownAttribute = inputActionDownAttribute;
                this.InputActionUpAttribute = inputActionUpAttribute;
            }

            public INamedTypeSymbol ComponentData { get; }

            public INamedTypeSymbol InputComponentData { get; }

            public ITypeSymbol Float2 { get; }

            public ITypeSymbol Half { get; }

            public ITypeSymbol ButtonState { get; }

            public ITypeSymbol InputEvent { get; }

            public INamedTypeSymbol InputActionAttribute { get; }

            public INamedTypeSymbol InputActionDeltaAttribute { get; }

            public INamedTypeSymbol InputActionDownAttribute { get; }

            public INamedTypeSymbol InputActionUpAttribute { get; }

            public static InputSymbols Create(Compilation compilation)
            {
                return new InputSymbols(
                    compilation.GetTypeByMetadataName("Unity.Entities.IComponentData"),
                    compilation.GetTypeByMetadataName("Unity.NetCode.IInputComponentData"),
                    compilation.GetTypeByMetadataName("Unity.Mathematics.float2"),
                    compilation.GetTypeByMetadataName("Unity.Mathematics.half"),
                    compilation.GetTypeByMetadataName("BovineLabs.Core.Input.ButtonState"),
                    compilation.GetTypeByMetadataName("Unity.NetCode.InputEvent"),
                    compilation.GetTypeByMetadataName("BovineLabs.Core.Input.InputActionAttribute"),
                    compilation.GetTypeByMetadataName("BovineLabs.Core.Input.InputActionDeltaAttribute"),
                    compilation.GetTypeByMetadataName("BovineLabs.Core.Input.InputActionDownAttribute"),
                    compilation.GetTypeByMetadataName("BovineLabs.Core.Input.InputActionUpAttribute"));
            }
        }
    }
}
