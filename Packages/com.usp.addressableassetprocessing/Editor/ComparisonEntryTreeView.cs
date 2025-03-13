using DocumentFormat.OpenXml.Drawing.Charts;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

using USP.AddressablesAssetProcessing;

[UxmlElement("ComparisonEntryTreeView")]
public partial class ComparisonEntryTreeView : MultiColumnTreeView
{
    #region Fields
    private readonly VisualTreeAsset comparisonEntryTreeViewUxml;

    private readonly StyleSheet comparisonEntryUss;

    private readonly VisualTreeAsset compareEntryControlsUxml;
    #endregion

    #region Methods
    public ComparisonEntryTreeView()
    {
        comparisonEntryTreeViewUxml = Helper.LoadRequired<VisualTreeAsset>("UXML\\ComparisonEntryTreeView.uxml");
        comparisonEntryUss = Helper.LoadRequired<StyleSheet>("StyleSheet\\ComparisonEntry.uss");
        compareEntryControlsUxml = Helper.LoadRequired<VisualTreeAsset>("UXML\\LeftRightCompareEntryControls.uxml");
    }

    public virtual void SetRootItems(IList<TreeViewItemData<ComparisonEntry>> rootItems)
    {
        base.SetRootItems(rootItems);
    }

    public virtual bool HasValidDataAndBindings()
    {
        return viewController != null && itemsSource != null;
    }

    public new void Rebuild()
    {
        if (itemsSource.Count == 0)
        {
            return;
        }

        var temp = new VisualElement();
        comparisonEntryTreeViewUxml.CloneTree(temp);
        var templateTreeView = temp.Q<MultiColumnTreeView>();

        // Using Columns.Add will remove the item from the original parent,
        // so we essentially pop it off like a queue. 
        while (templateTreeView.columns.Count != 0)
        {
            var templateColumn = templateTreeView.columns[0];
            string name = templateColumn.name;

            Column column = columns[name];

            if (column == null)
            {
                column = templateColumn;
                columns.Add(column);
            }
        }

        // Peek into the first item.
        ComparisonEntry firstTopEntry = GetItemDataForIndex<ComparisonEntry>(0);

        var columnsCount = firstTopEntry.compareTargets.Count + firstTopEntry.compareOperations.Count;
        Debug.Assert(columns.Count == columnsCount);

        foreach (string compareTarget in firstTopEntry.compareTargets.Keys)
        {
            ConfigureUserData(compareTarget);
        }

        foreach (string compareOperation in firstTopEntry.compareOperations.Keys)
        {
            ConfigureComparison(compareOperation, compareEntryControlsUxml, comparisonEntryUss);
        }

        base.Rebuild();
    }

    private void ConfigureUserData(string name)
    {
        Column column = columns[name];
        column.makeCell = () =>
        {
            var result = new TextField();
            result.SetEnabled(false);
            return result;
        };
        column.bindCell = (VisualElement element, int index) =>
        {
            if (element is not TextField textField)
            {
                return;
            }

            ComparisonEntry entry = GetItemDataForIndex<ComparisonEntry>(index);
            object value = entry.compareTargets[name].value;
            string textValue = value != null ? value.ToString() : "No Entry";

            textField.label = entry.entryName;
            textField.value = textValue;
        };
    }

    private void ConfigureComparison(string name, VisualTreeAsset compareEntryControlsUxml, StyleSheet comparisonEntryUss)
    {
        //↔→

        Column column = columns[name];
        column.makeCell = () =>
        {
            var result = new VisualElement();
            compareEntryControlsUxml.CloneTree(result);
            result.styleSheets.Add(comparisonEntryUss);

            return result;
        };
        column.bindCell = (VisualElement element, int index) =>
        {
            var sameLabel = element.Q<Label>("same-label");
            var differentButtons = element.Q<VisualElement>("different-buttons");

            var copyLeftButton = element.Q<Button>("copy-left-button");
            var copyRightButton = element.Q<Button>("copy-right-button");

            //copyLeftButton.clicked -=
            //copyLeftButton.clicked +=

            //copyRightButton.clicked -=
            //copyRightButton.clicked +=

            var entry = GetItemDataForIndex<ComparisonEntry>(index);
            CompareOperation operation = entry.compareOperations[name];
            if (operation.result)
            {
                sameLabel.style.display = DisplayStyle.Flex;
                differentButtons.style.display = DisplayStyle.None;
            }
            else
            {
                sameLabel.style.display = DisplayStyle.None;
                differentButtons.style.display = DisplayStyle.Flex;
            }
        };
    }
    #endregion
}
