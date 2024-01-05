// <copyright file="QualityToolbarBindings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.UI;
    using Unity.Properties;
    using UnityEngine;

    public class QualityToolbarBindings : IBindingObject<QualityToolbarBindings.Data>
    {
        private Data value;
        private List<string>? choices;

        public ref Data Value => ref this.value;

        [CreateProperty]
        public int QualityValue
        {
            get => QualitySettings.GetQualityLevel();
            set => QualitySettings.SetQualityLevel(value);
        }

        [CreateProperty]
        public List<string> QualityChoices => this.choices ??= QualitySettings.names.ToList();

        public struct Data
        {
        }
    }
}
#endif
