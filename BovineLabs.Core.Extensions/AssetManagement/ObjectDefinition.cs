// <copyright file="ObjectDefinition.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.AssetManagement
{
    using JetBrains.Annotations;
    using UnityEngine;

    [CreateAssetMenu(menuName = "BovineLabs/Object Definition", fileName = "Definition", order = -1000)]
    public sealed class ObjectDefinition : ScriptableObject
    {
        [HideInInspector] // So the field is not editable even in debug mode.
        [SerializeField]
        private ObjectId id;

        [SerializeField]
        [UsedImplicitly]
        private string description = string.Empty;

        [SerializeField]
        private string friendlyName = string.Empty;

        [ObjectCategories]
        [SerializeField]
        private byte category;

        [SerializeField]
        private GameObject? prefab;

        public GameObject? Prefab
        {
            get => this.prefab;
            internal set => this.prefab = value;
        }

        public string FriendlyName => this.friendlyName;

        public byte Category => this.category;

        internal ObjectId ID
        {
            get => this.id;
            set => this.id = value;
        }

        public static implicit operator ObjectId(ObjectDefinition definition)
        {
            return definition != null ? definition.id : default;
        }
    }
}
#endif
