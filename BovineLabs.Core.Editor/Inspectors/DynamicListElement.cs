// <copyright file="DynamicListElement.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Utility;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities.UI;
    using Unity.Properties;
    using UnityEditor;
    using UnityEngine.UIElements;

    public abstract class DynamicListElement<T, TElement> : EntityInspector<T>
        where TElement : unmanaged
    {
        private readonly PropertyElement content;
        private readonly List<TElement> autoRefreshList = new();

        private bool autoRefresh;

        protected DynamicListElement(object inspector, int refreshRate = 250)
            : base(inspector)
        {
            this.content = this.InitializeContent();
            this.Add(this.content);

            this.Add(this.InitializeRefreshButton());

            this.UpdateElement();

            if (refreshRate > 0)
            {
                this.schedule.Execute(this.Update).Every(refreshRate);
            }
        }

        public void Update()
        {
            if (!this.IsValid())
            {
                return;
            }

            if (!this.autoRefresh)
            {
                return;
            }

            this.ForceUpdate();
            this.OnRefresh();
        }

        public void ForceUpdate()
        {
            var target = this.content.GetTarget<Inspected>().Value;

            this.autoRefreshList.Clear();
            this.PopulateList(this.autoRefreshList);
            if (this.autoRefreshList.SequenceEqual(target))
            {
                return;
            }

            target.Clear();
            target.AddRange(this.autoRefreshList);
            this.Rebuild();
        }

        protected virtual void OnRefresh() {}

        protected abstract void PopulateList(List<TElement> list);

        protected abstract void OnValueChanged(NativeArray<TElement> newValues);

        private void Rebuild()
        {
            this.content.ForceReload();
            this.UpdateElement();
        }

        private void UpdateElement()
        {
            var addButton = this.content.Q<Button>(className: "unity-platforms__list-element__add-item-button");
            addButton.RemoveFromHierarchy();
            StylingUtility.AlignInspectorLabelWidth(this.content);
        }

        private unsafe void OnComponentChanged(BindingContextElement element, PropertyPath path)
        {
            if (!this.IsValid())
            {
                return;
            }

            if (this.Context.IsReadOnly || this.autoRefresh)
            {
                return;
            }

            var valueList = element.GetTarget<Inspected>().Value;
            var array = NoAllocHelpers.ExtractArrayFromList(valueList);

            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<TElement>(
                    (void*)handle.AddrOfPinnedObject(), valueList.Count, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, AtomicSafetyHandle.Create());
#endif

            this.OnValueChanged(nativeArray);
            handle.Free();

            var inspected = this.content.GetTarget<Inspected>().Value;
            inspected.Clear();
            this.PopulateList(inspected);
            this.Rebuild();
        }

        private PropertyElement InitializeContent()
        {
            var propertyElement = new PropertyElement();
            var list = new List<TElement>();
            this.PopulateList(list);

            // Something about live updating, taken from BufferElement
            if (EditorApplication.isPlaying)
            {
                propertyElement.userData = propertyElement;
            }

            propertyElement.AddContext(this.Context.Context);
            propertyElement.SetTarget(new Inspected { Value = list });
            propertyElement.OnChanged += this.OnComponentChanged;
            return propertyElement;
        }

        private VisualElement InitializeRefreshButton()
        {
            var button = new Toggle
            {
                text = "Auto Refresh",
                tooltip = "Auto refresh the display. This may cause performance issues.",
            };

            button.AddToClassList("unity-platforms__list-element__add-item-button");
            button.RegisterValueChangedCallback(evt => this.autoRefresh = evt.newValue);
            return button;
        }

        private struct Inspected
        {
            [InspectorOptions(HideResetToDefault = true)]
            [Pagination]
            public List<TElement> Value;
        }
    }
}
