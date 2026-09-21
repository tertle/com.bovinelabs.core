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
        private readonly PropertyElement _content;
        private readonly List<TElement> _autoRefreshList = new();

        private bool _autoRefresh;

        protected DynamicListElement(object inspector, int refreshRate = 250)
            : base(inspector)
        {
            _content = InitializeContent();
            Add(_content);

            Add(InitializeRefreshButton());

            UpdateElement();

            if (refreshRate > 0)
            {
                schedule.Execute(Update).Every(refreshRate);
            }
        }

        public void Update()
        {
            if (!IsValid())
            {
                return;
            }

            if (!_autoRefresh)
            {
                return;
            }

            ForceUpdate();
            OnRefresh();
        }

        public void ForceUpdate()
        {
            var target = _content.GetTarget<Inspected>().Value;

            _autoRefreshList.Clear();
            PopulateList(_autoRefreshList);
            if (_autoRefreshList.SequenceEqual(target))
            {
                return;
            }

            target.Clear();
            target.AddRange(_autoRefreshList);
            Rebuild();
        }

        protected virtual void OnRefresh() {}

        protected abstract void PopulateList(List<TElement> list);

        protected abstract void OnValueChanged(NativeArray<TElement> newValues);

        private void Rebuild()
        {
            _content.ForceReload();
            UpdateElement();
        }

        private void UpdateElement()
        {
            var addButton = _content.Q<Button>(className: "unity-platforms__list-element__add-item-button");
            addButton.RemoveFromHierarchy();
            StylingUtility.AlignInspectorLabelWidth(_content);
        }

        private unsafe void OnComponentChanged(BindingContextElement element, PropertyPath path)
        {
            if (!IsValid())
            {
                return;
            }

            if (Context.IsReadOnly || _autoRefresh)
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

            OnValueChanged(nativeArray);
            handle.Free();

            var inspected = _content.GetTarget<Inspected>().Value;
            inspected.Clear();
            PopulateList(inspected);
            Rebuild();
        }

        private PropertyElement InitializeContent()
        {
            var propertyElement = new PropertyElement();
            var list = new List<TElement>();
            PopulateList(list);

            // Something about live updating, taken from BufferElement
            if (EditorApplication.isPlaying)
            {
                propertyElement.userData = propertyElement;
            }

            propertyElement.AddContext(Context.Context);
            propertyElement.SetTarget(new Inspected { Value = list });
            propertyElement.OnChanged += OnComponentChanged;
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
            button.RegisterValueChangedCallback(evt => _autoRefresh = evt.newValue);
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
