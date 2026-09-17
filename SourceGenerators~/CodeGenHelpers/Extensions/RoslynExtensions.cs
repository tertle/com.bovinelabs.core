using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGenHelpers.Extensions
{
#nullable enable
    public static class RoslynExtensions
    {
        public static TSymbol? GetSymbol<TSymbol>(this Compilation compilation, BaseTypeDeclarationSyntax declarationSyntax)
            where TSymbol : ISymbol
        {
            var model = compilation.GetSemanticModel(declarationSyntax.SyntaxTree);
            return (TSymbol?)model.GetDeclaredSymbol(declarationSyntax);
        }

        public static TypedConstant GetAttributeValueByName(this AttributeData attribute, string name)
        {
            return attribute.NamedArguments.SingleOrDefault(arg => arg.Key == name).Value;
        }


        public static string GetAttributeValueByNameAsString(this AttributeData attribute, string name, string placeholder = "null")
        {
            var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

            return data.Value is null ? placeholder : data.Value.ToString();
        }
    }
}
