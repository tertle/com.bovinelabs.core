namespace BovineLabs.FacetGenerator
{
    using Microsoft.CodeAnalysis;

    [Generator]
    public partial class FacetGenerator : IIncrementalGenerator
    {
        internal static readonly SymbolDisplayFormat ShortTypeFormat = CreateShortTypeFormat();

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

        private static SymbolDisplayFormat CreateShortTypeFormat()
        {
            var format = SymbolDisplayFormat.MinimallyQualifiedFormat;

            return new SymbolDisplayFormat(
                format.GlobalNamespaceStyle,
                SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                format.GenericsOptions,
                format.MemberOptions,
                format.DelegateStyle,
                format.ExtensionMethodStyle,
                format.ParameterOptions,
                format.PropertyStyle,
                format.LocalOptions,
                format.KindOptions,
                format.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
        }
    }
}
