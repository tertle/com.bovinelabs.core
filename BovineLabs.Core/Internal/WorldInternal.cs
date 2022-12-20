// <copyright file="WorldInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public unsafe struct SystemStatePtr
    {
        public SystemState* State;

        public static implicit operator SystemState*(SystemStatePtr ptr)
        {
            return ptr.State;
        }

        public static implicit operator SystemStatePtr(SystemState* ptr)
        {
            return new SystemStatePtr { State = ptr };
        }
    }

    public static class WorldInternal
    {
        // public static void GetAllStatesNoAlloc(this World world, NativeList<SystemStatePtr> list)
        // {
        //     list.Clear();
        //
        //     var allUnmanaged = world.Unmanaged.GetImpl().GetAllSystems(Allocator.Temp);
        //     for (int i = 0; i < allUnmanaged.Length; ++i)
        //     {
        //         list.Add((SystemState*)allUnmanaged[i]);
        //     }
        // }
    }
}
