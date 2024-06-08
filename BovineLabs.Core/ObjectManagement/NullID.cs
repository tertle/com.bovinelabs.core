// <copyright file="NullID.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ObjectManagement
{
    using UnityEngine;

    internal class NullID : ScriptableObject, IUIDGlobal
    {
        [field: SerializeField]
        [field: HideInInspector]
        public int ID { get; set; }
    }
}
