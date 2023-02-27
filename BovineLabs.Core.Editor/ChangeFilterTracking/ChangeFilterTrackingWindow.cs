// <copyright file="ChangeFilterTrackingWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ChangeFilterTracking
{
    using System.Collections.Generic;
    using BovineLabs.Core.Extensions;
    using Unity.Entities;
    using Unity.Entities.Editor;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEngine.UIElements;
    using UITemplate = BovineLabs.Core.Editor.UI.UITemplate;

    internal class ChangeFilterTrackingWindow : DOTSEditorWindow
    {
        private const float UpdateRate = 0.5f;
        private const string RootUIPath = "Packages/com.bovinelabs.core/Editor Default Resources/ChangeFilterTracking/";
        private static readonly UITemplate TreeViewHeader = new(RootUIPath + "header");
        private static readonly UITemplate TreeViewItemTemplate = new(RootUIPath + "tree-view-item");

        private static readonly string WindowName = L10n.Tr("Change Filters");

        private readonly List<ComponentData> sources = new();

        private readonly List<VisualElement> elements = new();

        private double lastUpdateTime;
        private ListView listView;

        private VisualElement root;
        private World world;

        public ChangeFilterTrackingWindow()
            : base(Analytics.Window.Unknown)
        {
        }

        [MenuItem("BovineLabs/Tools/Change Filter")]
        public static void OpenWindow()
        {
            var window = GetWindow<ChangeFilterTrackingWindow>();
            window.Show();
        }

        public void OnEnable()
        {
            Resources.AddCommonVariables(this.rootVisualElement);

            this.titleContent = EditorGUIUtility.TrTextContent(WindowName, EditorIcons.System);
            this.minSize = Constants.MinWindowSize;

            this.root = new VisualElement();
            this.root.AddToClassList(UssClasses.SystemScheduleWindow.WindowRoot);
            this.rootVisualElement.Add(this.root);

            Resources.Templates.SystemSchedule.AddStyles(this.root);
            Resources.Templates.DotsEditorCommon.AddStyles(this.root);

            this.CreateToolBar(this.root);
            this.CreateListView(this.root);
        }

        protected override void OnUpdate()
        {
            if (this.world == null)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < this.lastUpdateTime + UpdateRate)
            {
                return;
            }

            this.lastUpdateTime = EditorApplication.timeSinceStartup;

            var systemHandle = this.world.Unmanaged.GetExistingUnmanagedSystem<ChangeFilterTrackingSystem>();
            if (systemHandle == SystemHandle.Null)
            {
                return;
            }

            ref var systemState = ref this.world.Unmanaged.GetExistingSystemState<ChangeFilterTrackingSystem>();
            ref var system = ref this.world.Unmanaged.GetUnsafeSystemRef<ChangeFilterTrackingSystem>(systemHandle);
            systemState.Dependency.Complete();

            var typeTracks = system.TypeTracks;

            var rebuild = this.sources.Count != typeTracks.Length;

            if (rebuild)
            {
                this.sources.Clear();

                // Initial setup
                foreach (var t in typeTracks)
                {
                    this.sources.Add(new ComponentData
                    {
                        Name = t.TypeName.ToString(),
                        Short = t.Short.Value,
                        Long = t.Long.Value,
                    });
                }

                this.listView.Rebuild();
            }
            else
            {
                for (var index = 0; index < typeTracks.Length; index++)
                {
                    var t = typeTracks[index];
                    var source = this.sources[index];

                    if (math.abs(source.Short - t.Short.Value) > float.Epsilon ||
                        math.abs(source.Long - t.Long.Value) > float.Epsilon)
                    {
                        source.Short = t.Short.Value;
                        source.Long = t.Long.Value;
                        ApplyElement(this.elements[index], source);
                    }
                }
            }
        }

        protected override void OnWorldSelected(World newWorld)
        {
            this.world = newWorld;
            this.sources.Clear();
            this.listView.Rebuild();
        }

        private static void ApplyElement(VisualElement element, ComponentData itemData)
        {
            element.Q<Label>("column1").text = itemData.Name;
            element.Q<Label>("column2").text = $"{itemData.Short:0.00%}";
            element.Q<Label>("column3").text = $"{itemData.Long:0.00%}";
        }

        private void CreateToolBar(VisualElement rootElement)
        {
            var toolbar = new VisualElement();
            Resources.Templates.SystemScheduleToolbar.Clone(toolbar);
            var leftSide = toolbar.Q(className: UssClasses.SystemScheduleWindow.Toolbar.LeftSide);

            var worldSelector = this.CreateWorldSelector();
            leftSide.Add(worldSelector);
            worldSelector.SetVisibility(true);

            rootElement.Add(toolbar);
        }

        private void CreateListView(VisualElement rootElement)
        {
            var headerRoot = new VisualElement();
            TreeViewHeader.Clone(headerRoot);
            rootElement.Add(headerRoot);

            this.listView = headerRoot.Q<ListView>("ListView");
            this.listView.itemsSource = this.sources;
            this.listView.makeItem = () => TreeViewItemTemplate.Clone();
            this.listView.bindItem = (element, item) =>
            {
                if (item >= this.elements.Count)
                {
                    this.elements.Resize(item + 1);
                }

                this.elements[item] = element;
                ApplyElement(element, this.sources[item]);
            };
        }

        private class ComponentData
        {
            public float Long;
            public string Name;
            public float Short;
        }
    }
}
