using Codice.CM.Common;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using USP.AddressablesAssetProcessing;

[UxmlElement("ComparisonEntryTreeView")]
public partial class ComparisonEntryTreeView : MultiColumnTreeView
{
    #region Static Methods
    public static List<TreeViewItemData<ComparisonEntry>> Pack(IEnumerable<ComparisonEntry> comparisonEntries)
    {
        var result = new List<TreeViewItemData<ComparisonEntry>>();
        foreach (var comparisonEntry in comparisonEntries)
        {
            var item = Pack(comparisonEntry);
            result.Add(item);
        }

        return result;
    }

    public static TreeViewItemData<ComparisonEntry> Pack(ComparisonEntry comparisonEntry)
    {
        List<TreeViewItemData<ComparisonEntry>> childItems = null;
        if (comparisonEntry.children != null)
        {
            childItems = Pack(comparisonEntry.children);
        }

        return new TreeViewItemData<ComparisonEntry>(comparisonEntry.GetHashCode(), comparisonEntry, childItems);
    }

    private static StyleLength Add(StyleLength leftHand, StyleLength rightHand)
    {
        Length length = Add(leftHand.value, rightHand.value);

        return new StyleLength(length);
    }

    private static Length Add(Length leftHand, Length rightHand)
    {
        if (leftHand.unit != rightHand.unit)
        {
            Debug.LogError("Attempting to add Lengths where units do not match.");

            return Length.None();
        }

        float leftValue = leftHand.value;
        float rightValue = rightHand.value;

        return new Length(leftValue + rightValue, leftHand.unit);
    }
    #endregion

    #region Fields
    private readonly VisualTreeAsset comparisonEntryTreeViewUxml;

    private readonly StyleSheet compareOperandUss;

    private readonly VisualTreeAsset compareOperandUxml;

    private IList<TreeViewItemData<ComparisonEntry>> _itemsSource;
    #endregion

    #region Properties
    public new IList<TreeViewItemData<ComparisonEntry>> itemsSource
    {
        get
        {
            return _itemsSource;
        }
        set
        {
            _itemsSource = value;
            base.SetRootItems(value);
        }
    }
    #endregion

    #region Events
    public event System.Action changed;
    #endregion

    #region Methods
    public ComparisonEntryTreeView()
    {
        comparisonEntryTreeViewUxml = FileHelper.LoadRequired<VisualTreeAsset>("UXML\\ComparisonEntryTreeView.uxml");
        compareOperandUss = FileHelper.LoadRequired<StyleSheet>("StyleSheet\\CompareOperand.uss");
        compareOperandUxml = FileHelper.LoadRequired<VisualTreeAsset>("UXML\\CompareOperand.uxml");
        reorderable = false;
    }

    public virtual void SetRootItems(IList<TreeViewItemData<ComparisonEntry>> rootItems)
    {
        itemsSource = rootItems;
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

        // Peek into the first item.
        ComparisonEntry firstTopEntry = GetItemDataForIndex<ComparisonEntry>(0);

        // Make sure that the current columns have matched size already.
        var columnsCount = firstTopEntry.compareTargets.Count + firstTopEntry.compareOperations.Count;
        while (columns.Count < columnsCount)
        {
            // Using Columns.Add will remove the item from the original parent,
            // so we essentially pop it off like a queue.
            var templateColumn = templateTreeView.columns[0];
            string name = templateColumn.name;

            Column column = columns[name];

            if (column == null)
            {
                column = templateColumn;
                columns.Add(column);
            }
        }

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
            object value = entry.compareTargets[name].Value;
            string textValue = value != null ? value.ToString() : "No Entry";

            textField.label = entry.entryName;
            textField.value = textValue;
        };
    }

    private void ConfigureComparison(string name, VisualTreeAsset compareEntryControlsUxml, StyleSheet comparisonEntryUss)
    {
        //↔→

        Column column = columns[name];

        // Peek into the first item.
        ComparisonEntry firstTopEntry = GetItemDataForIndex<ComparisonEntry>(0);

        // Get the comparison operation.
        CompareOperation operation = firstTopEntry.compareOperations[name];

        // The top level comparison will tell us if any of the entries within it are not a match.
        // If the items are not a match, then we want to provide user with options.
        float width = 0;
        if (!operation.result)
        {
            if (!operation.leftHand.IsReadonly)
            {
                width += 60;
            }
            if (!operation.rightHand.IsReadonly)
            {
                width += 60;
            }

            width = Mathf.Max(60, width);
        }
        else
        {
            width = Mathf.Max(60, column.minWidth.value);
        }
        
        column.minWidth = column.width = width;

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

                return;
            }

            if (operation.leftHand.IsReadonly && operation.rightHand.IsReadonly)
            {
                sameLabel.style.display = DisplayStyle.None;
                differentLabel.style.display = DisplayStyle.Flex;
                differentButtons.style.display = DisplayStyle.None;

                return;
            }

            sameLabel.style.display = DisplayStyle.None;
            differentLabel.style.display = DisplayStyle.None;
            differentButtons.style.display = DisplayStyle.Flex;

            var copyLeftButton = element.Q<Button>("copy-left-button");

            if (operation.leftHand.IsReadonly)
            {
                copyLeftButton.style.display = DisplayStyle.None;
            }
            else
            {
                copyLeftButton.clicked += () =>
                {
                    Copy(operation.rightHand, operation.leftHand);
                };
                copyLeftButton.clicked += changed;
            }

            var copyRightButton = element.Q<Button>("copy-right-button");

            if (operation.rightHand.IsReadonly)
            {
                copyRightButton.style.display = DisplayStyle.None;
            }
            else
            {
                copyRightButton.clicked += () =>
                {
                    Copy(operation.leftHand, operation.rightHand);
                };
                copyRightButton.clicked += changed;
            }
        };
    }

    private void Copy(CompareOperand source, CompareOperand destination)
    {
        ComparisonEntry firstTopEntry = GetItemDataForIndex<ComparisonEntry>(0);

        destination.Value = source.Value;
    }
    #endregion
}
