// <copyright file="EntitySelection.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Internal
{
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Entities.Editor;
    using UnityEditor;
    using UnityEngine;

    public static class EntitySelection
    {
        public static bool IsSelected => Selection.activeObject is EntitySelectionProxy;

        public static World World => ((EntitySelectionProxy)Selection.activeObject).World;

        public static Entity Entity => ((EntitySelectionProxy)Selection.activeObject).Entity;

        public static IEnumerable<(World World, Entity Entity)> GetAllSelections()
        {
            foreach (var s in Selection.objects)
            {
                if (s is EntitySelectionProxy proxy)
                {
                    yield return (proxy.World, proxy.Entity);
                }
            }
        }

        public static IEnumerable<Entity> GetAllSelectionsInWorld(World world)
        {
            foreach (var s in Selection.objects)
            {
                switch (s)
                {
                    case EntitySelectionProxy proxy:
                    {
                        if (proxy.World == world)
                        {
                            yield return proxy.Entity;
                        }

                        break;
                    }

                    case GameObject go:
                    {
                        var entity = world.EntityManager.Debug.GetPrimaryEntityForAuthoringObject(go);
                        if (entity != Entity.Null)
                        {
                            yield return entity;
                        }

                        break;
                    }
                }
            }
        }

        public static void GetAllSelectionsInWorld(World world, NativeList<Entity> entities, NativeList<int> instanceIds)
        {
            foreach (var s in Selection.objects)
            {
                switch (s)
                {
                    case EntitySelectionProxy proxy:
                    {
                        if (proxy.World == world)
                        {
                            entities.Add(proxy.Entity);
                        }

                        break;
                    }

                    case GameObject go:
                    {
                        instanceIds.Add(go.GetInstanceID());
                        break;
                    }
                }
            }
        }

        public static void UnSelect()
        {
            if (Selection.activeObject is EntitySelectionProxy)
            {
                Selection.activeObject = null;
            }
        }
    }
}
