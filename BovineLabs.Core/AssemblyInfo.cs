// <copyright file="AssemblyInfo.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BovineLabs.Core.Editor")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Authoring")]
[assembly: InternalsVisibleTo("BovineLabs.Nerve")]
[assembly: InternalsVisibleTo("BovineLabs.Nerve.Tests")]
[assembly: InternalsVisibleTo("BovineLabs.Nerve.Authoring")]
[assembly: InternalsVisibleTo("BovineLabs.Nerve.Debug")]
[assembly: InternalsVisibleTo("BovineLabs.Nerve.Editor")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Tests")]
[assembly: InternalsVisibleTo("BovineLabs.Testing")]

[assembly:
    SuppressMessage("Code Quality", "CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.",
        Justification = "Unity")]
