// <copyright file="NoTransform.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary> Added NoTransform will ensure all Transform components are excluded from an entity and all its children. </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StaticOptimizeEntity))]
    public class NoTransform : MonoBehaviour
    {
        [SerializeField]
        private bool removeFromChildren = true;

        public bool RemoveFromChildren => this.removeFromChildren;
    }
}
