// namespace BovineLabs.Basics.Editor.UI
// {
//     using System;
//     using System.Collections.Generic;
//     using UnityEditor;
//     using UnityEditorInternal;
//     using UnityEngine;
//
//     public class ReorderableListViewSet<T> : ReorderableListViewBase<T>
//     {
//         private List<string> textList = new List<string>();
//
//         public ReorderableListViewSet(List<T> dataList, string header, bool allowReorder = true)
//             : base(dataList, header, allowReorder)
//         {
//         }
//
//         public bool AllowDuplicates { get; set; }
//
//         // list of options for the drop down menu when the user clicks the add button
//         public delegate List<T> GetAddMenuOptionsDelegate();
//         public GetAddMenuOptionsDelegate GetAddMenuOptions;
//
//         protected override void OnAddDropdownMenu(Rect buttonRect, ReorderableList list)
//         {
//             // created the drop down menu on add item from the listview
//             var addMenuOptions = this.GetAddMenuOptions != null ? this.GetAddMenuOptions() : new List<T>();
//
//             var menu = new GenericMenu();
//             for (int optionIndex = 0; optionIndex < addMenuOptions.Count; optionIndex++)
//             {
//                 var option = addMenuOptions[optionIndex];
//                 var displayName = this.GetDisplayName(option);
//
//                 // disable any option that is already in the text list
//                 var optionEnabled = this.AllowDuplicates || !this.textList.Contains(displayName);
//
//                 if (optionEnabled)
//                 {
//                     menu.AddItem(new GUIContent(displayName), false, () => this.OnAddMenuClicked(option));
//                 }
//                 else
//                 {
//                     menu.AddDisabledItem(new GUIContent(displayName));
//                 }
//             }
//
//             menu.ShowAsContext();
//         }
//
//         /// <inheritdoc/>
//         protected override void OnChange()
//         {
//             this.textList.Clear();
//             try
//             {
//                 foreach (var data in this.DataList)
//                 {
//                     this.textList.Add(this.GetDisplayName(data));
//                 }
//             }
//             catch (Exception e)
//             {
//                 Debug.Log($"Exception: {e} while handling ReorderableListView of type: {typeof(T)}");
//             }
//         }
//     }
// }