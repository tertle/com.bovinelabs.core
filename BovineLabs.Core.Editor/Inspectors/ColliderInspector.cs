// <copyright file="ColliderInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Editor.Inspectors
{
    using BovineLabs.Core.Internal;
    using Unity.Entities;
    using Unity.Physics;
    using Unity.Platforms.UI;
    using UnityEngine;
    using UnityEngine.UIElements;
    using BoxCollider = Unity.Physics.BoxCollider;
    using CapsuleCollider = Unity.Physics.CapsuleCollider;
    using Collider = Unity.Physics.Collider;
    using SphereCollider = Unity.Physics.SphereCollider;

    internal unsafe class ColliderInspector : PropertyInspector<BlobAssetReference<Collider>>
    {
        /// <inheritdoc/>
        public override VisualElement Build()
        {
            var root = new VisualElement();
            if (this.Target.IsCreated)
            {
                root.Add(CreateDisabled<LongField, long>("Hash", this.Target.GetHash()));
                root.Add(CreateDisabled<Toggle, bool>("RespondsToCollision", this.Target.Value.GetRespondsToCollision()));

                var collider = new Foldout { text = this.Target.Value.Type.ToString() };
                root.Add(collider);
                CreateCollider(collider, (Collider*)this.Target.GetUnsafePtr());

                // TODO be nice if this was in popup format
                var filter = this.Target.Value.GetCollisionFilter();

                var filterFoldout = new Foldout { text = "filter" };
                root.Add(filterFoldout);
                filterFoldout.Add(CreateDisabled<LongField, long>(nameof(filter.BelongsTo), filter.BelongsTo));
                filterFoldout.Add(CreateDisabled<LongField, long>(nameof(filter.CollidesWith), filter.CollidesWith));
                filterFoldout.Add(CreateDisabled<LongField, long>(nameof(filter.GroupIndex), filter.GroupIndex));

                CreateMassProperties(root, ref this.Target.Value);
            }

            return root;
        }

        private static void CreateCollider(VisualElement parent, Collider* collider)
        {
            switch (collider->Type)
            {
                case ColliderType.Convex:
                    break;
                case ColliderType.Sphere:
                    CreateSphere(parent, collider);
                    break;
                case ColliderType.Capsule:
                    CreateCapsule(parent, collider);
                    break;
                case ColliderType.Triangle:
                case ColliderType.Quad:
                    CreatePolygon(parent, collider);
                    break;
                case ColliderType.Box:
                    CreateBox(parent, collider);
                    break;
                case ColliderType.Cylinder:
                    CreateCylinder(parent, collider);
                    break;
                case ColliderType.Mesh:
                    break;
                case ColliderType.Compound:
                    CreateCompound(parent, collider);
                    break;
                case ColliderType.Terrain:
                    break;
            }
        }

        private static void CreateMassProperties(VisualElement parent, ref Collider collider)
        {
            var massProperties = new Foldout { text = nameof(collider.MassProperties), value = false };
            parent.Add(massProperties);

            var massDistribution = new Foldout { text = nameof(collider.MassProperties.MassDistribution) };
            massProperties.Add(massDistribution);

            massProperties.Add(CreateDisabled<FloatField, float>(nameof(collider.MassProperties.Volume), collider.MassProperties.Volume));
            massProperties.Add(
                CreateDisabled<FloatField, float>(nameof(collider.MassProperties.AngularExpansionFactor), collider.MassProperties.AngularExpansionFactor));
        }

        private static void CreateSphere(VisualElement parent, Collider* collider)
        {
            var sphere = (SphereCollider*)collider;
            parent.Add(CreateDisabled<Vector3Field, Vector3>("Center", sphere->Center));
            parent.Add(CreateDisabled<FloatField, float>("Radius", sphere->Radius));
        }

        private static void CreateCapsule(VisualElement parent, Collider* collider)
        {
            var capsule = (CapsuleCollider*)collider;
            parent.Add(CreateDisabled<Vector3Field, Vector3>("Vertex0", capsule->Vertex0));
            parent.Add(CreateDisabled<Vector3Field, Vector3>("Vertex1", capsule->Vertex1));
            parent.Add(CreateDisabled<FloatField, float>("Radius", capsule->Radius));
        }

        private static void CreatePolygon(VisualElement parent, Collider* collider)
        {
            var polygon = (PolygonCollider*)collider;
            for (var i = 0; i < polygon->Vertices.Length; i++)
            {
                parent.Add(CreateDisabled<Vector3Field, Vector3>($"Vertex{i}", polygon->Vertices[i]));
            }
        }

        private static void CreateBox(VisualElement parent, Collider* collider)
        {
            var box = (BoxCollider*)collider;
            parent.Add(CreateDisabled<Vector3Field, Vector3>("Center", box->Center));
            parent.Add(CreateDisabled<Vector4Field, Vector4>("Orientation", box->Orientation.value));
            parent.Add(CreateDisabled<Vector3Field, Vector3>("Size", box->Size));
            parent.Add(CreateDisabled<FloatField, float>("Bevel Radius", box->BevelRadius));
        }

        private static void CreateCylinder(VisualElement parent, Collider* collider)
        {
            var cylinder = (CylinderCollider*)collider;
            parent.Add(CreateDisabled<Vector3Field, Vector3>("Center", cylinder->Center));
            parent.Add(CreateDisabled<Vector4Field, Vector4>("Orientation", cylinder->Orientation.value));
            parent.Add(CreateDisabled<FloatField, float>("Height", cylinder->Height));
            parent.Add(CreateDisabled<FloatField, float>("Radius", cylinder->Radius));
            parent.Add(CreateDisabled<FloatField, float>("Bevel Radius", cylinder->BevelRadius));
            parent.Add(CreateDisabled<IntegerField, int>("Side Count", cylinder->SideCount));
        }

        private static void CreateCompound(VisualElement parent, Collider* collider)
        {
            var compound = (CompoundCollider*)collider;
            for (var i = 0; i < compound->NumChildren; i++)
            {
                ref var child = ref compound->Children[i];

                var foldout = new Foldout { text = child.Collider->Type.ToString() };
                foldout.SetValueWithoutNotify(false);
                parent.Add(foldout);

                CreateCollider(foldout, child.Collider);
            }
        }

        private static VisualElement CreateDisabled<T, TValue>(string label, TValue value)
            where T : BaseField<TValue>, new()
        {
            var element = new T { label = label };
            element.SetValueWithoutNotify(value);
            element.SetEnabled(false);
            return element;
        }
    }
}
#endif
