// <copyright file="SystemDependencyWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Dependency
{
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.SearchWindow;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine.UIElements;

    internal class SystemDependencyWindow : DOTSSearchWindow
    {
        private readonly List<string> output = new();
        private readonly HashSet<TypeIndex> readOrWriteTypes = new();
        private readonly HashSet<TypeIndex> writeTypes = new();
        private bool isAfter;

        private SystemHandle system;

        private ListView ListView => (ListView)this.View;

        /// <inheritdoc/>
        protected override string WindowName { get; } = L10n.Tr("System Dependencies");

        /// <inheritdoc/>
        protected override string DefaultButtonText => "Systems";

        [MenuItem(EditorMenus.RootMenuTools + "System Dependencies")]
        public static void OpenWindow()
        {
            GetWindow<SystemDependencyWindow>().Show();
        }

        /// <inheritdoc/>
        protected override void PopulateItems(List<SearchView.Item> items)
        {
            var initialization = this.World!.GetExistingSystemManaged<InitializationSystemGroup>();
            if (initialization != null)
            {
                this.FindAllDependencies(initialization, items);
            }

            var simulation = this.World.GetExistingSystemManaged<SimulationSystemGroup>();
            if (simulation != null)
            {
                this.FindAllDependencies(simulation, items);
            }

            var presentation = this.World.GetExistingSystemManaged<PresentationSystemGroup>();
            if (presentation != null)
            {
                this.FindAllDependencies(presentation, items);
            }
        }

        /// <inheritdoc/>
        protected override unsafe void SearchWindowOnOnSelection(SearchView.Item item)
        {
            this.system = (SystemHandle)item.Data;
            this.Button.text = item.Name;

            this.readOrWriteTypes.Clear();
            this.writeTypes.Clear();

            if (this.World != null)
            {
                var state = this.World.Unmanaged.ResolveSystemState(this.system);

                for (var index = 0; index < state->m_JobDependencyForReadingSystems.Length; index++)
                {
                    this.readOrWriteTypes.Add(state->m_JobDependencyForReadingSystems[index]);
                }

                for (var index = 0; index < state->m_JobDependencyForWritingSystems.Length; index++)
                {
                    this.writeTypes.Add(state->m_JobDependencyForWritingSystems[index]);
                    this.readOrWriteTypes.Add(state->m_JobDependencyForWritingSystems[index]);
                }

                this.Rebuild();
            }
        }

        /// <inheritdoc/>
        protected override void Rebuild()
        {
            this.ClearItems();
            this.RebuildInternal();
        }

        /// <inheritdoc />
        protected override VisualElement CreateView()
        {
            var listView = new ListView();

            listView.makeItem = () => new Label();
            listView.bindItem = (element, i) =>
            {
                var label = (Label)element;
                label.text = this.output[i];
            };

            listView.itemsSource = this.output;

            return listView;
        }

        private void RebuildInternal()
        {
            this.View.Clear();

            this.isAfter = false;
            this.output.Clear();

            var initialization = this.World!.GetExistingSystemManaged<InitializationSystemGroup>();
            if (initialization != null)
            {
                this.IterateAll(initialization);
            }

            var simulation = this.World.GetExistingSystemManaged<SimulationSystemGroup>();
            if (simulation != null)
            {
                this.IterateAll(simulation);
            }

            var presentation = this.World.GetExistingSystemManaged<PresentationSystemGroup>();
            if (presentation != null)
            {
                this.IterateAll(presentation);
            }

            // Rare case when last system
            if (!this.isAfter)
            {
                this.output.Add(this.GetName(this.system));
            }

            this.ListView.Rebuild();
        }

        private unsafe void FindAllDependencies(ComponentSystemGroup systemGroup, List<SearchView.Item> items)
        {
            var masterUpdateList = systemGroup.m_MasterUpdateList;
            var updateListLength = masterUpdateList.Length;
            for (var i = 0; i < updateListLength; ++i)
            {
                var index = masterUpdateList[i];

                if (!index.IsManaged)
                {
                    var handle = systemGroup.m_UnmanagedSystemsToUpdate[index.Index];
                    var state = systemGroup.World.Unmanaged.ResolveSystemStateChecked(handle);
                    items.Add(new SearchView.Item
                    {
                        Path = SearchView.Item.ConvertTypeToPath(state->DebugName.ToString()),
                        Data = handle,
                    });
                }
                else
                {
                    var sys = systemGroup.m_managedSystemsToUpdate[index.Index];
                    var state = sys.CheckedState();
                    items.Add(new SearchView.Item
                    {
                        Path = SearchView.Item.ConvertTypeToPath(state->DebugName.ToString()),
                        Data = sys.SystemHandle,
                    });

                    if (sys is ComponentSystemGroup subSystemGroup)
                    {
                        this.FindAllDependencies(subSystemGroup, items);
                    }
                }
            }
        }

        private void IterateAll(ComponentSystemGroup systemGroup)
        {
            var masterUpdateList = systemGroup.m_MasterUpdateList;
            var updateListLength = masterUpdateList.Length;
            for (var i = 0; i < updateListLength; ++i)
            {
                var index = masterUpdateList[i];

                if (!index.IsManaged)
                {
                    var handle = systemGroup.m_UnmanagedSystemsToUpdate[index.Index];
                    CheckDependencies(handle);
                }
                else
                {
                    var sys = systemGroup.m_managedSystemsToUpdate[index.Index];

                    CheckDependencies(sys.SystemHandle);

                    if (sys is ComponentSystemGroup subSystemGroup)
                    {
                        this.IterateAll(subSystemGroup);
                    }
                }
            }

            void CheckDependencies(SystemHandle handle)
            {
                if (handle == this.system)
                {
                    this.output.Add(this.GetName(handle));
                    this.isAfter = true;
                }
                else if (this.HasDependency(handle))
                {
                    this.output.Add($"  {this.GetName(handle)}");
                }
            }
        }

        private unsafe bool HasDependency(SystemHandle handle)
        {
            var state = this.World!.Unmanaged.ResolveSystemState(handle);

            for (var index = 0; index < state->m_JobDependencyForReadingSystems.Length; index++)
            {
                if (this.writeTypes.Contains(state->m_JobDependencyForReadingSystems[index]))
                {
                    return true;
                }
            }

            for (var index = 0; index < state->m_JobDependencyForWritingSystems.Length; index++)
            {
                if (this.readOrWriteTypes.Contains(state->m_JobDependencyForWritingSystems[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private unsafe string GetName(SystemHandle handle)
        {
            var state = this.World!.Unmanaged.ResolveSystemState(handle);
            return state == null ? string.Empty : state->DebugName.ToString();
        }
    }
}
