// <copyright file="InputActionGenerator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.InputGenerator
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public class InputActionGenerator : IIncrementalGenerator
    {
        private const string ButtonState = "ButtonState";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidateProvider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: IsSyntaxTargetForGeneration,
                    transform: GetSemanticTargetForGeneration)
                .Where(t => t is { Item1: not null, Item2: { Count: > 0 } });

            var combined = candidateProvider.Combine(context.CompilationProvider);

            context.RegisterSourceOutput(combined, (productionContext, sourceProviderTuple) =>
            {
                var (structDeclarationSyntax, fields, assembly) = (sourceProviderTuple.Left.Item1, sourceProviderTuple.Left.Item2, sourceProviderTuple.Right);
                Execute(productionContext, assembly, structDeclarationSyntax, fields);
            });
        }

        private static void Execute(
            SourceProductionContext context, Compilation compilation, StructDeclarationSyntax candidate, List<FieldData> fields)
        {
            if (!SourceGenHelpers.ShouldRun(compilation, context.CancellationToken))
                return;

            var semanticModel = compilation.GetSemanticModel(candidate.SyntaxTree);

            var structSymbol = semanticModel.GetDeclaredSymbol(candidate);
            if (structSymbol == null)
            {
                return;
            }

            var source = ProcessStruct(structSymbol, fields);
            context.AddSource($"{structSymbol.Name}_Action.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        private static string ProcessStruct(INamedTypeSymbol structSymbol, List<FieldData> fields)
        {
            var ns = structSymbol.ContainingNamespace.ToDisplayString();
            var hasNamespace = !string.IsNullOrWhiteSpace(ns);

            var source = new StringBuilder();
            if (hasNamespace)
            {
                source.Append($@"namespace {ns}
{{
");
            }

            source.Append($@"    using System;
    using BovineLabs.Core.Input;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.InputSystem;

    partial struct {structSymbol.Name}
    {{
");
            GenerateActionComponent(source, fields);
            GenerateSystem(source, structSymbol, fields);
            GenerateSettings(source, structSymbol, fields);

            source.Append("    }\n");

            if (hasNamespace)
            {
                source.Append("}\n");
            }

            return source.ToString();
        }

        private static void GenerateActionComponent(StringBuilder source, List<FieldData> fields)
        {
            source.Append($@"        private struct ActionsGenerated : IComponentData
        {{
");

            foreach (var fieldSymbol in fields)
            {
                source.Append($"            public UnityObjectRef<InputActionReference> {fieldSymbol.Name};\n");
            }

            source.Append("        }\n");
        }

        private static void GenerateSystem(StringBuilder source, INamedTypeSymbol structSymbol, List<FieldData> fields)
        {
            var hasDelta = fields.Any(f => f.Delta);

            source.Append($@"
        [UpdateInGroup(typeof(InputSystemGroup))]
        private partial class System : SystemBase
        {{
            private EntityQuery query;
            private EntityQuery queryActions;
            private {structSymbol.Name} input;
");
            if (hasDelta)
            {
                source.AppendLine("            private float deltaTime;");
            }

            source.Append($@"
            protected override void OnCreate()
            {{
                this.query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<{structSymbol.Name}>().Build(this);
                this.queryActions = new EntityQueryBuilder(Allocator.Temp).WithAll<ActionsGenerated>().Build(this);

                this.RequireForUpdate(this.query);
                this.RequireForUpdate(this.queryActions);
            }}

            protected override void OnUpdate()
            {{
");
            if (hasDelta)
            {
                source.AppendLine("                this.deltaTime = this.World.Time.DeltaTime;");
            }

            source.Append(@"                this.query.SetSingleton(this.input);
");
            foreach (var fieldSymbol in fields)
            {
                if (fieldSymbol.Type == ButtonState)
                {
                    source.Append($@"                this.input.{fieldSymbol.Name}.Reset();
");
                }
            }

            source.Append($@"            }}

            protected override void OnStartRunning()
            {{
                if (!this.queryActions.TryGetSingleton<ActionsGenerated>(out var actions))
                {{
                    Debug.LogError(""ActionsGenerated has not been created. Make sure you update InputCommonSettings."");
                    return;
                }}
");
            foreach (var fieldSymbol in fields)
            {
                source.Append($@"
                if (actions.{fieldSymbol.Name}.Value != null)
                {{
");
                source.AppendLine(fieldSymbol.Type is "bool" or ButtonState
                    ? $"                    actions.{fieldSymbol.Name}.Value.action.started += this.On{fieldSymbol.Name}Started;"
                    : $"                    actions.{fieldSymbol.Name}.Value.action.performed += this.On{fieldSymbol.Name}Performed;");

                source.AppendLine($"                    actions.{fieldSymbol.Name}.Value.action.canceled += this.On{fieldSymbol.Name}Canceled;");
                source.Append($@"                }}
                else
                {{
                    Debug.LogWarning(""InputActionReference for {structSymbol.Name}.{fieldSymbol.Name} has not been assigned."");
                }}
");
            }

            source.Append($@"            }}

            protected override void OnStopRunning()
            {{
                if (this.queryActions.TryGetSingleton<ActionsGenerated>(out var actions))
                {{
");

            foreach (var fieldSymbol in fields)
            {
                source.Append($@"                    if (actions.{fieldSymbol.Name}.Value != null)
                    {{
");
                source.AppendLine(fieldSymbol.Type is "bool" or ButtonState
                    ? $"                        actions.{fieldSymbol.Name}.Value.action.started -= this.On{fieldSymbol.Name}Started;"
                    : $"                        actions.{fieldSymbol.Name}.Value.action.performed -= this.On{fieldSymbol.Name}Performed;");

                source.Append(@$"                        actions.{fieldSymbol.Name}.Value.action.canceled -= this.On{fieldSymbol.Name}Canceled;
                    }}
");
            }

            source.AppendLine("                }");
            source.AppendLine("            }");

            foreach (var fieldSymbol in fields)
            {
                if (fieldSymbol.Type == "bool")
                {
                    source.Append($@"
            private void On{fieldSymbol.Name}Started(InputAction.CallbackContext context)
            {{
                this.input.{fieldSymbol.Name} = true;
            }}

            private void On{fieldSymbol.Name}Canceled(InputAction.CallbackContext context)
            {{
                this.input.{fieldSymbol.Name} = false;
            }}
");
                }
                else if (fieldSymbol.Type == ButtonState)
                {
                    source.Append($@"
            private void On{fieldSymbol.Name}Started(InputAction.CallbackContext context)
            {{
                this.input.{fieldSymbol.Name}.Started();
            }}

            private void On{fieldSymbol.Name}Canceled(InputAction.CallbackContext context)
            {{
                this.input.{fieldSymbol.Name}.Cancelled();
            }}
");
                }
                else
                {
                    source.Append($@"
            private void On{fieldSymbol.Name}Performed(InputAction.CallbackContext context)
            {{
                this.input.{fieldSymbol.Name} = context.ReadValue<{(fieldSymbol.Type == "float" ? "float" : "Vector2")}>(){(fieldSymbol.Delta ? " * this.deltaTime" : "")};
            }}

            private void On{fieldSymbol.Name}Canceled(InputAction.CallbackContext context)
            {{
                this.input.{fieldSymbol.Name} = 0;
            }}
");
                }
            }

            source.AppendLine("        }");
        }

        private static void GenerateSettings(StringBuilder source, INamedTypeSymbol structSymbol, List<FieldData> fields)
        {
            source.Append($@"
        [Serializable]
        private class Settings : IInputSettings
        {{
            [HideInInspector]
            public string Name = ""{structSymbol.Name}"";
");

            foreach (var fieldSymbol in fields)
            {
                source.Append($"            public InputActionReference {fieldSymbol.Name};\n");
            }

            source.Append($@"            public void Bake(IBakerWrapper baker)
            {{
                baker.AddComponent(new ActionsGenerated
                {{
");

            foreach (var fieldSymbol in fields)
            {
                source.Append($"                    {fieldSymbol.Name} = baker.DependsOn(this.{fieldSymbol.Name}),\n");
            }

            source.Append($@"                }});
                baker.AddComponent(default({structSymbol.Name}));
            }}
");

            source.Append("        }\n");
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode syntaxNode, CancellationToken cancellationToken)
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

            // Has IComponentData identifier
            var hasIComponentData = false;
            foreach (var baseType in structDeclarationSyntax.BaseList.Types)

                if (baseType.Type is IdentifierNameSyntax { Identifier: { ValueText: "IComponentData" } })
                {
                    hasIComponentData = true;
                    break;
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

        private static (StructDeclarationSyntax, List<FieldData>) GetSemanticTargetForGeneration(
            GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
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
                            Delta = attribute == AttributeType.InputActionDelta,
                        });
                    }
                }
            }

            return (structDeclarationSyntax, fields);
        }

        private enum AttributeType
        {
            None,
            InputAction,
            InputActionDelta,
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
                    }
                }
            }

            return AttributeType.None;
        }

        private static bool ValidFieldType(string name)
        {
            switch (name)
            {
                case "bool":
                case "float":
                case "float2":
                case ButtonState:
                    return true;
                default:
                    return false;
            }
        }

        private struct FieldData
        {
            public string Name;
            public string Type;
            public bool Delta;
        }
    }
}
