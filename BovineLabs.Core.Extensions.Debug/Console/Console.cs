// <copyright file="Console.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Console
{
    using System.Collections.Generic;
    using UnityEngine.UI;
    using UnityEngine.UIElements;

    [UxmlElement]
    public partial class Console : VisualElement
    {
        private readonly ListView listView;
        private readonly TextField textField;

        private List<string> items = new List<string>();

        public Console()
        {
            this.listView = new ListView();
            this.textField = new TextField();

            this.Add(this.listView);
            this.Add(this.textField);

            this.style.flexGrow = 1;

            this.listView.style.flexGrow = 1;
            this.listView.makeItem = () => new Label();
            this.listView.bindItem = (element, i) => ((Label)element).text = this.items[i];
            this.listView.itemsSource = this.items;

            this.listView.horizontalScrollingEnabled = true;

            this.items.Add("Test 1");
            this.items.Add("Test 2");
            this.items.Add("Test 312341234 1234123412 23r41234 123412 34123 412341234123");
            this.items.Add("Test 4");
        }
    }
}
