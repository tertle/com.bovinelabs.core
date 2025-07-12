// <copyright file="SubSceneSet.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Authoring.SubScenes
{
    using BovineLabs.Core.ObjectManagement;

    [AutoRef(nameof(SubSceneSettings), nameof(SubSceneSettings.SceneSets), nameof(SubSceneSet), "Scenes", createNull:false)]
    public class SubSceneSet : SubSceneSetBase, IUID
    {
        public int ID;

        public bool IsRequired;
        public bool WaitForLoad = true;
        public bool AutoLoad;

        /// <inheritdoc/>
        int IUID.ID
        {
            get => this.ID;
            set => this.ID = value;
        }
    }
}
#endif
