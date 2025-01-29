// <copyright file="ComponentDependencyWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Dependency
{
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.SearchWindow;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine.UIElements;

    internal class ComponentDependencyWindow : DOTSSearchWindow
    {
        private readonly List<string> output = new();
        private bool isReaders;

        private TypeIndex typeIndex;

        protected override string WindowName { get; } = L10n.Tr("Component Dependencies");

        protected override string DefaultButtonText => "Components";

        [MenuItem("BovineLabs/Tools/Component Dependencies")]
        public static void OpenWindow()
        {
            GetWindow<ComponentDependencyWindow>().Show();
        }

        protected override void PopulateItems(List<SearchView.Item> items)
        {
            foreach (var t in TypeManager.AllTypes)
            {
                if (t.Category != TypeManager.TypeCategory.ComponentData && t.Category != TypeManager.TypeCategory.BufferData)
                {
                    continue;
                }

                if (t.TypeIndex.IsManagedComponent || t.Type == null)
                {
                    continue;
                }

                items.Add(new SearchView.Item
                {
                    Path = t.DebugTypeName.ToString().Replace('.', '/'),
                    Data = t,
                });
            }
        }

        protected override void SearchWindowOnOnSelection(SearchView.Item item)
        {
            var typeInfo = (TypeManager.TypeInfo)item.Data;

            this.typeIndex = typeInfo.TypeIndex;
            this.Rebuild();

            this.Button.text = item.Name;
        }

        protected override void Rebuild()
        {
            this.View.Clear();

            var initialization = this.World!.GetExistingSystemManaged<InitializationSystemGroup>();
            if (initialization != null)
            {
                this.FindAllDependencies(initialization);
            }

            var simulation = this.World.GetExistingSystemManaged<SimulationSystemGroup>();
            if (simulation != null)
            {
                this.FindAllDependencies(simulation);
            }

            var presentation = this.World.GetExistingSystemManaged<PresentationSystemGroup>();
            if (presentation != null)
            {
                this.FindAllDependencies(presentation);
            }

            this.Write();
        }

        /// <inheritdoc />
        protected override VisualElement CreateView()
        {
            return new ScrollView();
        }

        private unsafe void FindAllDependencies(ComponentSystemGroup systemGroup)
        {
            var masterUpdateList = systemGroup.m_MasterUpdateList;
            var updateListLength = masterUpdateList.Length;
            for (var i = 0; i < updateListLength; ++i)
            {
                var index = masterUpdateList[i];

                if (!index.IsManaged)
                {
                    var handle = systemGroup.m_UnmanagedSystemsToUpdate[index.Index];
                    ref var state = ref systemGroup.World.Unmanaged.ResolveSystemStateRef(handle);
                    GetDependencies(ref state);
                }
                else
                {
                    var sys = systemGroup.m_managedSystemsToUpdate[index.Index];
                    GetDependencies(ref *sys.CheckedState());

                    if (sys is ComponentSystemGroup subSystemGroup)
                    {
                        this.FindAllDependencies(subSystemGroup);
                    }
                }
            }

            void GetDependencies(ref SystemState state)
            {
                for (var i = 0; i < state.m_JobDependencyForWritingSystems.Length; i++)
                {
                    if (state.m_JobDependencyForWritingSystems[i] == this.typeIndex)
                    {
                        if (this.isReaders)
                        {
                            this.Write();
                            this.isReaders = false;
                        }

                        this.output.Add(state.DebugName.ToString());
                        return;
                    }
                }

                for (var i = 0; i < state.m_JobDependencyForReadingSystems.Length; i++)
                {
                    if (state.m_JobDependencyForReadingSystems[i] == this.typeIndex)
                    {
                        if (!this.isReaders)
                        {
                            this.Write();
                            this.isReaders = true;
                        }

                        this.output.Add(state.DebugName.ToString());
                        return;
                    }
                }
            }
        }

        private void Write()
        {
            if (this.output.Count <= 0)
            {
                return;
            }

            var type = this.isReaders ? "Readers" : "Writers";

            var readerString = string.Join("\n  ", this.output);
            this.View.Add(new Label($"{type}:\n  {readerString}"));

            this.output.Clear();
        }
    }
}
