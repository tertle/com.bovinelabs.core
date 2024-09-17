// <copyright file="EventHandler.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    public delegate void EventHandler<in TSender, in TArgs>(TSender sender, TArgs args);
}
