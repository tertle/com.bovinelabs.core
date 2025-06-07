// <copyright file="ChangeFilterTrackingWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#pragma warning disable CS8632

namespace BovineLabs.Core.Editor.ChangeFilterTracking
{
    using System.Collections.Generic;
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

        private readonly Dictionary<int, VisualElement> elements = new();

        private double lastUpdateTime;

        private ListView? listView;
        private VisualElement? root;
        private World? world;

        public ChangeFilterTrackingWindow()
            : base(Analytics.Window.Unknown)
        {
        }

        [MenuItem(EditorMenus.RootMenuTools + "Change Filter")]
        public static void OpenWindow()
        {
            var window = GetWindow<ChangeFilterTrackingWindow>();
            window.Show();
        }

        protected override void OnCreate()
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

        /// <inheritdoc />
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
                    this.sources.Add(new ComponentData(t.TypeName.ToString(), t.Short.Value, t.Long.Value));
                }

                this.listView!.Rebuild();
            }
            else
            {
                for (var index = 0; index < typeTracks.Length; index++)
                {
                    var t = typeTracks[index];
                    var source = this.sources[index];

                    if (math.abs(source.Short - t.Short.Value) > float.Epsilon || math.abs(source.Long - t.Long.Value) > float.Epsilon)
                    {
                        source.Short = t.Short.Value;
                        source.Long = t.Long.Value;
                        ApplyElement(this.elements[index], source);
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void OnWorldSelected(World newWorld)
        {
            this.world = newWorld;
            this.sources.Clear();
            this.listView!.Rebuild();
        }

        private static void ApplyElement(VisualElement element, ComponentData itemData)
        {
            element.Q<Label>("column1").text = itemData.Name;
            element.Q<Label>("column2").text = $"{itemData.Short:0.00%}";
            element.Q<Label>("column3").text = $"{itemData.Long:0.00%}";
        }

        private void CreateToolBar(VisualElement rootElement)
        {
            Resources.Templates.SystemScheduleToolbar.Clone(rootElement);
            var leftSide = rootElement.Q(className: UssClasses.SystemScheduleWindow.Toolbar.LeftSide);

            var worldSelector = this.CreateWorldSelector();
            leftSide.Add(worldSelector);
            worldSelector.SetVisibility(true);
        }

        private void CreateListView(VisualElement rootElement)
        {
            TreeViewHeader.Clone(rootElement);

            this.listView = rootElement.Q<ListView>("ListView");
            this.listView.itemsSource = this.sources;
            this.listView.makeItem = () => TreeViewItemTemplate.Clone();
            this.listView.bindItem = (element, item) =>
            {
                this.elements[item] = element;
                ApplyElement(element, this.sources[item]);
            };
        }

        private record ComponentData(string Name, float Short, float Long)
        {
            public string Name { get; } = Name;

            public float Short { get; set; } = Short;

            public float Long { get; set; } = Long;
        }
    }
}
