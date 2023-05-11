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

        [ObjectCategories]
        [SerializeField]
        private ObjectCategory categories;

        [SerializeField]
        [UsedImplicitly]
        private string description = string.Empty;

        [SerializeField]
        private string friendlyName = string.Empty;

        [SerializeField]
        private GameObject? prefab;

        public GameObject? Prefab
        {
            get => this.prefab;
            internal set => this.prefab = value;
        }

        public ObjectCategory Categories => this.categories;

        public string FriendlyName => this.friendlyName;

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
