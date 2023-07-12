// <copyright file="IUID.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    public interface IUID
    {
        protected internal int ID { get; set; }
    }
}
#endif
