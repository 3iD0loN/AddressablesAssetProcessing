using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using USP.AddressablesAssetProcessing;

[UxmlElement("ComparisonEntryTreeView")]
public partial class ComparisonEntryTreeView : MultiColumnTreeView
{
    #region Static Methods
    public static List<TreeViewItemData<TreeViewElement<ComparisonEntry>>> Pack(IEnumerable<ComparisonEntry> comparisonEntries, bool isExpanded = true, bool isChildrenExpanded = true)
    {
        var result = new List<TreeViewItemData<TreeViewElement<ComparisonEntry>>>();
        foreach (var comparisonEntry in comparisonEntries)
        {
            var item = Pack(comparisonEntry, isExpanded, isChildrenExpanded);
            result.Add(item);
        }

        return result;
    }

    public static TreeViewItemData<TreeViewElement<ComparisonEntry>> Pack(ComparisonEntry comparisonEntry, bool isExpanded = true, bool isChildrenExpanded = true)
    {
        List<TreeViewItemData<TreeViewElement<ComparisonEntry>>> childItems = null;
        if (comparisonEntry.children != null)
        {
            childItems = Pack(comparisonEntry.children, isChildrenExpanded, isChildrenExpanded);
        }

        var data = new TreeViewElement<ComparisonEntry>(isExpanded, comparisonEntry);
        return new TreeViewItemData<TreeViewElement<ComparisonEntry>>(comparisonEntry.GetHashCode(), data, childItems);
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

    private IList<TreeViewItemData<TreeViewElement<ComparisonEntry>>> _itemsSource;
    #endregion

    #region Properties
    public new IList<TreeViewItemData<TreeViewElement<ComparisonEntry>>> itemsSource
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
    public event System.Action<int> changed;
    #endregion

    #region Methods
    public ComparisonEntryTreeView()
    {
        comparisonEntryTreeViewUxml = FileHelper.LoadRequired<VisualTreeAsset>("UXML\\ComparisonEntryTreeView.uxml");
        compareOperandUss = FileHelper.LoadRequired<StyleSheet>("StyleSheet\\CompareOperand.uss");
        compareOperandUxml = FileHelper.LoadRequired<VisualTreeAsset>("UXML\\CompareOperand.uxml");
        reorderable = false;
    }

    public virtual void SetRootItems(IList<TreeViewItemData<TreeViewElement<ComparisonEntry>>> rootItems)
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
        TreeViewElement<ComparisonEntry> firstTopEntry = GetItemDataForIndex<TreeViewElement<ComparisonEntry>>(0);

        // Make sure that the current columns have matched size already.
        var columnsCount = firstTopEntry.Value.compareTargets.Count + firstTopEntry.Value.compareOperations.Count;
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

        foreach (string compareTarget in firstTopEntry.Value.compareTargets.Keys)
        {
            ConfigureUserData(compareTarget);
        }

        foreach (string compareOperation in firstTopEntry.Value.compareOperations.Keys)
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

            TreeViewElement<ComparisonEntry> entry = GetItemDataForIndex<TreeViewElement<ComparisonEntry>>(index);
            
            CompareOperand compareOperand = entry.Value.compareTargets[name];

            string textValue = compareOperand != null ?
                compareOperand.Value != null ?
                    compareOperand.Value.ToString() :
                    "No Entry" :
                string.Empty;

            textField.label = entry.Value.entryName;
            textField.value = textValue;
        };
    }

    private void ConfigureComparison(string name, VisualTreeAsset compareEntryControlsUxml, StyleSheet comparisonEntryUss)
    {
        //↔→

        Column column = columns[name];

        // Peek into the first item.
        TreeViewElement<ComparisonEntry> firstTopEntry = GetItemDataForIndex<TreeViewElement<ComparisonEntry>>(0);

        // Get the comparison operation.
        CompareOperation operation = firstTopEntry.Value.compareOperations[name];

        // The top level comparison will tell us if any of the entries within it are not a match.
        // If the items are not a match, then we want to provide user with options.
        float width = 0;
        if (!operation.result)
        {
            if (!operation.leftHand.IsReadOnly)
            {
                width += 60;
            }
            if (!operation.rightHand.IsReadOnly)
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

        column.makeCell = () => new CompareOperationField();

        column.bindCell = (VisualElement element, int index) =>
        {
            if (element is not CompareOperationField compareOperationField)
            {
                return;
            }

            var entry = GetItemDataForIndex<TreeViewElement<ComparisonEntry>>(index);
            CompareOperation operation = entry.Value.compareOperations[name];

            compareOperationField.dataSource = operation;
            compareOperationField.Rebuild(() => changed?.Invoke(index));
        };
    }
    #endregion
}
