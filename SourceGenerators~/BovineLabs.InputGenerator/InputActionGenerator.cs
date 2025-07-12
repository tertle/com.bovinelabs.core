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

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidateProvider = context
                .SyntaxProvider
                .CreateSyntaxProvider(predicate: IsSyntaxTargetForGeneration, transform: GetSemanticTargetForGeneration)
                .Where(t => t != null);

            context.RegisterSourceOutput(candidateProvider, Execute);
        }

        private static void Execute(SourceProductionContext context, Data data)
        {
            var builder = ProcessStruct(data);
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

                GenerateActionComponent(builder, data);
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

        private static void GenerateActionComponent(ClassBuilder source, Data data)
        {
            var builder = source.AddNestedClass("ActionsGenerated", false, Accessibility.Private).OfType(TypeKind.Struct).AddInterface("IComponentData");

            foreach (var fieldSymbol in data.Fields)
            {
                builder.AddProperty(fieldSymbol.Name, Accessibility.Public).SetType("UnityObjectRef<InputActionReference>").WithValue(null);
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
            builder.AddProperty("queryActions", Accessibility.Private).SetType("EntityQuery").WithValue(null);
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

                    body.AppendLine("this.queryActions = new EntityQueryBuilder(Allocator.Temp).WithAll<ActionsGenerated>().Build(this);");
                    body.NewLine();
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
                        .If("!this.queryActions.TryGetSingleton<ActionsGenerated>(out var actions)")
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
                            .If($"actions.{fieldSymbol.Name}.Value != null")
                            .WithBody(b =>
                            {
                                if (IsUpDown(fieldSymbol))
                                {
                                    b.AppendLine(fieldSymbol.AttributeType == AttributeType.Down
                                        ? $"actions.{fieldSymbol.Name}.Value.action.started += this.On{fieldSymbol.Name}Started;"
                                        : $"actions.{fieldSymbol.Name}.Value.action.canceled += this.On{fieldSymbol.Name}Canceled;");
                                }
                                else if (IsButton(fieldSymbol))
                                {
                                    b.AppendLine($"actions.{fieldSymbol.Name}.Value.action.started += this.On{fieldSymbol.Name}Started;");
                                    b.AppendLine($"actions.{fieldSymbol.Name}.Value.action.canceled += this.On{fieldSymbol.Name}Canceled;");
                                }
                                else
                                {
                                    b.AppendLine($"actions.{fieldSymbol.Name}.Value.action.performed += this.On{fieldSymbol.Name}Performed;");
                                    b.AppendLine($"actions.{fieldSymbol.Name}.Value.action.canceled += this.On{fieldSymbol.Name}Canceled;");
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
                        .If("this.queryActions.TryGetSingleton<ActionsGenerated>(out var actions)")
                        .WithBody(body =>
                        {
                            foreach (var fieldSymbol in data.Fields)
                            {
                                body
                                    .If($"actions.{fieldSymbol.Name}.Value != null")
                                    .WithBody(b =>
                                    {
                                        if (IsUpDown(fieldSymbol))
                                        {
                                            b.AppendLine(fieldSymbol.AttributeType == AttributeType.Down
                                                ? $"actions.{fieldSymbol.Name}.Value.action.started -= this.On{fieldSymbol.Name}Started;"
                                                : $"actions.{fieldSymbol.Name}.Value.action.canceled -= this.On{fieldSymbol.Name}Canceled;");
                                        }
                                        else if (IsButton(fieldSymbol))
                                        {
                                            b.AppendLine($"actions.{fieldSymbol.Name}.Value.action.started -= this.On{fieldSymbol.Name}Started;");
                                            b.AppendLine($"actions.{fieldSymbol.Name}.Value.action.canceled -= this.On{fieldSymbol.Name}Canceled;");
                                        }
                                        else
                                        {
                                            b.AppendLine($"actions.{fieldSymbol.Name}.Value.action.performed -= this.On{fieldSymbol.Name}Performed;");
                                            b.AppendLine($"actions.{fieldSymbol.Name}.Value.action.canceled -= this.On{fieldSymbol.Name}Canceled;");
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
                                $"this.input.{fieldSymbol.Name} = ({fieldSymbol.Type})context.ReadValue<{(isVector2 ? "Vector2" : "float")}>(){(fieldSymbol.AttributeType == AttributeType.InputActionDelta ? " * this.deltaTime" : "")};"));

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
                    b.AppendLine("var ag = new ActionsGenerated();");
                    foreach (var fieldSymbol in data.Fields)
                    {
                        b.AppendLine($"ag.{fieldSymbol.Name} = this.{fieldSymbol.Name};");
                    }

                    b.AppendLine("baker.AddComponent(ag);");

                    if (!data.IsNetCode)
                    {
                        b.AppendLine($"baker.AddComponent(default({data.Symbol.Name}));\n");
                    }
                });
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Is Struct
                if (syntaxNode is not StructDeclarationSyntax structDeclarationSyntax)
                    return false;

                // Has Base List
                if (structDeclarationSyntax.BaseList == null)
                {
                    return false;
                }

                var hasIComponentData = false;
                foreach (var baseType in structDeclarationSyntax.BaseList.Types)

                {
                    var syntax = baseType.Type as IdentifierNameSyntax;
                    if (syntax?.Identifier.ValueText is "IComponentData" or "IInputComponentData")
                    {
                        hasIComponentData = true;
                        break;
                    }
                }

                if (!hasIComponentData)
                    return false;

                // Has Partial keyword
                var hasPartial = false;
                foreach (var m in structDeclarationSyntax.Modifiers)
                {
                    if (m.IsKind(SyntaxKind.PartialKeyword))
                    {
                        hasPartial = true;
                        break;
                    }
                }

                if (!hasPartial)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                SourceGenHelpers.Log(ex.ToString());
                return false;
            }
        }

        private static Data GetSemanticTargetForGeneration(
            GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
        {
            try
            {
                var structDeclarationSyntax = (StructDeclarationSyntax)ctx.Node;

                var fields = new List<FieldData>();

                foreach (var m in structDeclarationSyntax.Members)
                {
                    if (m is not FieldDeclarationSyntax field)
                    {
                        continue;
                    }

                    var type = field.Declaration.Type.GetText().ToString().TrimEnd();

                    if (!ValidFieldType(type))
                    {
                        continue;
                    }

                    var attribute = GetInputActionAttribute(field);

                    if (attribute != AttributeType.None)
                    {
                        foreach (var variable in field.Declaration.Variables)
                        {
                            fields.Add(new FieldData
                            {
                                Name = variable.Identifier.ValueText,
                                Type = type,
                                AttributeType = attribute,
                            });
                        }
                    }
                }

                if (fields.Count == 0)
                {
                    return null;
                }

                var typeSymbol = ctx.SemanticModel.GetDeclaredSymbol(structDeclarationSyntax);
                if (typeSymbol == null)
                {
                    return null;
                }

                var isNetcode = HasInputComponentData(structDeclarationSyntax);
                return new Data(typeSymbol, fields, isNetcode);
            }
            catch (Exception ex)
            {
                SourceGenHelpers.Log(ex.ToString());
                return null;
            }
        }

        private static bool HasInputComponentData(StructDeclarationSyntax structDeclarationSyntax)
        {
            foreach (var baseType in structDeclarationSyntax.BaseList!.Types)
            {
                var syntax = baseType.Type as IdentifierNameSyntax;
                if (syntax?.Identifier.ValueText is "IInputComponentData")
                {
                    return true;
                }
            }

            return false;
        }

        private enum AttributeType
        {
            None,
            InputAction,
            InputActionDelta,
            Down,
            Up
        }

        private static AttributeType GetInputActionAttribute(FieldDeclarationSyntax field)
        {
            foreach (var al in field.AttributeLists)
            {
                foreach (var a in al.Attributes)
                {
                    var n = a.Name.ToFullString();
                    switch (n)
                    {
                        case "InputAction":
                        case "InputActionAttribute":
                            return AttributeType.InputAction;
                        case "InputActionDelta":
                        case "InputActionDeltaAttribute":
                            return AttributeType.InputActionDelta;
                        case "InputActionDown":
                        case "InputActionDownAttribute":
                            return AttributeType.Down;
                        case "InputActionUp":
                        case "InputActionUpAttribute":
                            return AttributeType.Up;
                    }
                }
            }

            return AttributeType.None;
        }

        private static bool ValidFieldType(string name)
        {
            switch (name)
            {
                case Bool:
                case Float:
                // case "int":
                // case "short":
                case Float2:
                case Half:
                case ButtonState:
                case InputEvent:
                    return true;
                default:
                    return false;
            }
        }

        private class Data
        {
            public readonly INamedTypeSymbol Symbol;
            public readonly List<FieldData> Fields;
            public readonly bool IsNetCode;

            public Data(INamedTypeSymbol symbol, List<FieldData> fields, bool isNetCode)
            {
                this.Symbol = symbol;
                this.Fields = fields;
                this.IsNetCode = isNetCode;
            }
        }

        private struct FieldData
        {
            public string Name;
            public string Type;
            public AttributeType AttributeType;
        }
    }
}