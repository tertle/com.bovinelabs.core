// <copyright file="IInputSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    public interface IInputSettings
    {
        void Bake(IBakerWrapper baker);
    }
}
#endif
