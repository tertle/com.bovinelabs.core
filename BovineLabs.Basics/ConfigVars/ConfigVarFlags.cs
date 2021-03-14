// <copyright file="ConfigVarFlags.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.ConfigVars
{
    using System;
    using JetBrains.Annotations;
    using UnityEngine;

    /// <summary> Flags used for filtering vars. </summary>
    [Flags]
    public enum ConfigVarFlags
    {
        /// <summary> None. </summary>
        None = 0x0,

        /// <summary> Causes the config var to be save to <see cref="PlayerPrefs" />. </summary>
        /// <remarks>
        /// In editor all config vars are saved using EditorPrefs.
        /// </remarks>
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        Save = 0x1,

        // /// <summary> Consider this a cheat var. Can only be set if cheats enabled. </summary>
        // Cheat = 0x2,
        //
        // /// <summary> These vars are sent to server when connecting and when changed. </summary>
        // ServerInfo = 0x4,
        //
        // /// <summary> These vars are sent to clients when connecting and when changed. </summary>
        // ClientInfo = 0x8,
        //
        // /// <summary>  User created variable. </summary>
        // User = 0x10,
    }
}