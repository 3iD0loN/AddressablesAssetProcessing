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

    private readonly StyleSheet compareOperandUss;

    private readonly VisualTreeAsset compareOperandUxml;
    #endregion

    #region Methods
    public ComparisonEntryTreeView()
    {
        comparisonEntryTreeViewUxml = Helper.LoadRequired<VisualTreeAsset>("UXML\\ComparisonEntryTreeView.uxml");
        compareOperandUss = Helper.LoadRequired<StyleSheet>("StyleSheet\\CompareOperand.uss");
        compareOperandUxml = Helper.LoadRequired<VisualTreeAsset>("UXML\\CompareOperand.uxml");
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
            ConfigureComparison(compareOperation, compareOperandUxml, compareOperandUss);
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
            var entry = GetItemDataForIndex<ComparisonEntry>(index);
            CompareOperation operation = entry.compareOperations[name];

            var sameLabel = element.Q<Label>("same-label");
            var differentLabel = element.Q<Label>("different-label");
            var differentButtons = element.Q<VisualElement>("different-buttons");

            if (operation.result)
            {
                sameLabel.style.display = DisplayStyle.Flex;
                differentLabel.style.display = DisplayStyle.None;
                differentButtons.style.display = DisplayStyle.None;

                element.style.width = sameLabel.style.width;

                return;
            }

            if (operation.leftHand.isReadonly && operation.rightHand.isReadonly)
            {
                sameLabel.style.display = DisplayStyle.None;
                differentLabel.style.display = DisplayStyle.Flex;
                differentButtons.style.display = DisplayStyle.None;

                element.style.width = differentLabel.style.width;

                return;
            }

            sameLabel.style.display = DisplayStyle.None;
            differentLabel.style.display = DisplayStyle.None;
            differentButtons.style.display = DisplayStyle.Flex;

            var copyLeftButton = element.Q<Button>("copy-left-button");

            //element.style.width = 0;
            if (operation.leftHand.isReadonly)
            {
                copyLeftButton.style.display = DisplayStyle.None;
            }
            else
            {
                //element.width.value += copyLeftButton.style.width.value;
                //copyLeftButton.clicked =
            }

            var copyRightButton = element.Q<Button>("copy-right-button");

            if (operation.rightHand.isReadonly)
            {
                copyRightButton.style.display = DisplayStyle.None;
            }
            else
            {
                //element.width.value += copyRightButton.style.width.value;
                //copyRightButton.clicked =
            }
        };
    }
    #endregion
}
