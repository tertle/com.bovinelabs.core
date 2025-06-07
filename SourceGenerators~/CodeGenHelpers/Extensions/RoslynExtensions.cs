using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGenHelpers.Extensions
{
#nullable enable
    public static class RoslynExtensions
    {
        /// <summary>
        /// Method to get the <see cref="ISymbol"/> from a <see cref="BaseTypeDeclarationSyntax"/>
        /// </summary>
        /// <typeparam name="TSymbol">The symbol that you want that derives from <see cref="ISymbol"/></typeparam>
        /// <param name="compilation">The <see cref="Compilation"/></param>
        /// <param name="declarationSyntax">The <see cref="BaseTypeDeclarationSyntax"/>that you want to convert to symbol.</param>
        /// <returns>The desired Symbol.</returns>
        public static TSymbol? GetSymbol<TSymbol>(this Compilation compilation, BaseTypeDeclarationSyntax declarationSyntax)
            where TSymbol : ISymbol
        {
            var model = compilation.GetSemanticModel(declarationSyntax.SyntaxTree);
            return (TSymbol?)model.GetDeclaredSymbol(declarationSyntax);
        }

        /// <summary>
        /// Returns the value of a named attribute as a <see cref="TypedConstant"/>. If no attribute with the given name is found, this method returns <c>null</c>.
        /// </summary>
        /// <param name="attribute">The attribute whose value is to be returned.</param>
        /// <param name="name">The name of the attribute to be returned.</param>
        /// <returns>The value of the named attribute, or <c>null</c> if no such attribute is found.</returns>
        public static TypedConstant GetAttributeValueByName(this AttributeData attribute, string name)
        {
            return attribute.NamedArguments.SingleOrDefault(arg => arg.Key == name).Value;
        }


        /// <summary>
        /// Returns the value of an attribute with the given name as a string. If no attribute with the specified name is found, the default value of "null" is returned.
        /// </summary>
        /// <param name="attribute">The attribute to inspect.</param>
        /// <param name="name">The name of the attribute value to retrieve.</param>
        /// <param name="placeholder">The default value to return if the attribute value is null.</param>
        /// <returns>The attribute value as a string, or the specified default value if the attribute is not found or its value is null.</returns>
        public static string GetAttributeValueByNameAsString(this AttributeData attribute, string name, string placeholder = "null")
        {
            var data = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == name).Value;

            return data.Value is null ? placeholder : data.Value.ToString();
        }
    }
}
