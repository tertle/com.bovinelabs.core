// <copyright file="ListViewCountTracker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_2023_3_OR_NEWER
namespace BovineLabs.Core.UI
{
    using System.Collections.Generic;
    using UnityEngine.UIElements;

    [UxmlObject]
    public partial class ListViewCountTrackerBinding : CustomBinding
    {
        private readonly Dictionary<ListView, int> m_CachedCount = new();

        protected override void OnActivated(in BindingActivationContext context)
        {
            if (context.targetElement is not ListView listView)
                return;

            // Ensures the refresh will be called on the next update
            this.m_CachedCount[listView] = -1;
        }

        protected override void OnDeactivated(in BindingActivationContext context)
        {
            if (context.targetElement is not ListView listView)
                return;

            this.m_CachedCount.Remove(listView);
        }

        protected override BindingResult Update(in BindingContext context)
        {
            if (context.targetElement is not ListView listView)
                return new BindingResult(BindingStatus.Failure, "'ListViewCountTracker' should only be added to a 'ListView'");

            if (!this.m_CachedCount.TryGetValue(listView, out var previousCount) || previousCount == listView.itemsSource?.Count)
                return new BindingResult(BindingStatus.Failure, "");

            listView.RefreshItems();
            this.m_CachedCount[listView] = listView.itemsSource?.Count ?? -1;

            return new BindingResult(BindingStatus.Success);
        }
    }
}
#endif
