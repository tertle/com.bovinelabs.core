// <copyright file="AssemblyDefinitionTemplate.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.AssemblyBuilder
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "match expected json.")]
    [SuppressMessage("ReSharper", "NotAccessedField.Local", Justification = "match expected json.")]
    [SuppressMessage("ReSharper", "SA1307", Justification = "match expected json.")]
    [SuppressMessage("ReSharper", "CollectionNeverQueried.Local", Justification = "match expected json.")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local", Justification = "match expected json.")]
    [SuppressMessage("ReSharper", "NotAccessedField.Global", Justification = "match expected json.")]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "match expected json.")]
    [SuppressMessage("ReSharper", "SA1600", Justification = "match expected json.")]
    [SuppressMessage("ReSharper", "CollectionNeverQueried.Global", Justification = "match expected json.")]
    public struct AssemblyDefinitionTemplate
    {
        public string name;
        public string rootNamespace;
        public List<string> references;
        public List<string> optionalUnityReferences;
        public List<string> includePlatforms;
        public List<string> excludePlatforms;
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public List<string> precompiledReferences;
        public bool autoReferenced;
        public List<string> defineConstraints;
        public List<string> versionDefines;
        public bool noEngineReferences;

        public static AssemblyDefinitionTemplate New()
        {
            return new AssemblyDefinitionTemplate
            {
                references = new List<string>(),
                optionalUnityReferences = new List<string>(),
                includePlatforms = new List<string>(),
                excludePlatforms = new List<string>(),
                allowUnsafeCode = false,
                overrideReferences = false,
                precompiledReferences = new List<string>(),
                autoReferenced = false,
                defineConstraints = new List<string>(),
                versionDefines = new List<string>(),
                noEngineReferences = false,
            };
        }
    }
}
