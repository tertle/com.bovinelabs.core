// <copyright file="AssemblyInfo.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BovineLabs.Core.Editor")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions.Authoring")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions.Debug")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Tests")]

[assembly:
    SuppressMessage("Code Quality", "CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.",
        Justification = "Unity")]
