// <copyright file="FacetComposition.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Facet
{
    using BovineLabs.Core;

    public partial struct FacetComposition : IFacet
    {
        [Facet]
        private TestFacet testFacet;
    }
}
