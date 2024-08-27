// <copyright file="UIGameBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension.UI
{
    using BovineLabs.Core.UI;
    using Unity.Properties;

    public class UIGameBinding : IBindingObject<UIGameBinding.Data>
    {
        private Data data;

        public ref Data Value => ref this.data;

        [CreateProperty]
        public bool Quit
        {
            get => this.data.Quit.Value;
            set => this.data.Quit.TryProduce(value);
        }

        public struct Data : IBindingObject
        {
            public ButtonEvent Quit;
        }
    }
}
