// <copyright file="IUID.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ObjectManagement
{
    /// <summary>
    /// Marking a scriptable object with this interface will automatically generate a branch safe unique ID for all objects of the same type.
    /// </summary>
    public interface IUID
    {
        /// <summary> Gets or sets an ID that is unique to all scriptable objects of the same type.. </summary>
        int ID { get; set; }
    }
}
