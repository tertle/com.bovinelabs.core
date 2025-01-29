// <copyright file="IUIDGlobal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ObjectManagement
{
    /// <summary>
    /// Marking a scriptable object with this interface will automatically generate a branch safe unique ID for all objects regardless of type.
    /// </summary>
    public interface IUIDGlobal : IUID
    {
    }
}
