// <copyright file="InitializeAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    /// <summary>
    /// An attribute that will work like RuntimeInitializeOnLoadMethodAttribute in editor, but as RuntimeInitializeOnLoadMethodAttribute in builds
    /// to ensure that the method is always executed
    /// </summary>
#if UNITY_EDITOR
    public class InitializeAttribute : UnityEditor.InitializeOnLoadMethodAttribute
    {
    }
#else
    public class InitializeAttribute : UnityEngine.RuntimeInitializeOnLoadMethodAttribute
    {
        public InitializeAttribute()
            : base(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)
        {
        }
    }
#endif
}
