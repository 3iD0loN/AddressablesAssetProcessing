using System;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline;
using UnityEngine.UIElements;

using USP.AddressablesAssetProcessing;
using USP.MetaAddressables;

[UxmlElement("AssetField")]
public partial class AssetField : VisualElement
{
    #region Fields
    private readonly VisualTreeAsset assetFieldUxml;
    #endregion

    #region Properties
    public new IEnumerable<TreeViewItemData<TreeViewElement<Asset>>> dataSource { get; set; }
    #endregion

    #region Events
    public event Action<bool, int, int> changed;
    #endregion

    #region Methods
    public AssetField()
    {
        assetFieldUxml = FileHelper.LoadRequired<VisualTreeAsset>("UXML\\AssetField.uxml");
    }

    public void Rebuild()
    {
        this.Clear();
        assetFieldUxml.CloneTree(this);

        ComparisonEntryTreeView comparisonEntryTreeView = this.Q<ComparisonEntryTreeView>("comparison-entry");

        Action rebuildComparisonEntryTreeView = () =>
        {
            bool isAssetView = true;
            var comparisonEntries = new HashSet<ComparisonEntry>();
            foreach (TreeViewItemData<TreeViewElement<Asset>> selectedItem in dataSource)
            {
                Asset selectedAsset = selectedItem.data.Value;
                isAssetView &= selectedAsset is not Folder;
                selectedAsset.Compare(false);
                var selectedComparisonEntries = selectedAsset.ComparisonEntries;
                if (selectedComparisonEntries == null)
                {
                    continue;
                }

                comparisonEntries.UnionWith(selectedComparisonEntries);
            }

            ComparisonEntry allComparisons = CreateEntry(comparisonEntries);

            List<TreeViewItemData<TreeViewElement<ComparisonEntry>>> comparisonEntryItems = ComparisonEntryTreeView.Pack(comparisonEntries, isAssetView, true);

            comparisonEntryTreeView.SetRootItems(comparisonEntryItems);
            TreeViewExtensions.ExpandItems(comparisonEntryTreeView, comparisonEntryItems, true);
            comparisonEntryTreeView.Rebuild();

        };

        rebuildComparisonEntryTreeView();
        comparisonEntryTreeView.changed += (int selectedIndex) =>
        {
            bool isAssetView = true;
            foreach (TreeViewItemData<TreeViewElement<Asset>> selectedItem in dataSource)
            {
                Asset selectedAsset = selectedItem.data.Value;
                isAssetView &= selectedAsset is not Folder;
            }

            int rootParentId = TreeViewExtensions.FindRootItemIdByIndex(comparisonEntryTreeView, selectedIndex);

            TreeViewElement<ComparisonEntry> oldComparisonEntryItem = comparisonEntryTreeView.GetItemDataForId<TreeViewElement<ComparisonEntry>>(rootParentId);
            string assetFilePath = oldComparisonEntryItem.Value.entryName;

            Asset.ByComparisonEntryAssetPath.TryGetValue(assetFilePath, out Asset asset);

            asset.Compare(true);

            var comparisonEntries = asset.ComparisonEntries;
            foreach (var comparisonEntry in comparisonEntries)
            {
                TreeViewItemData<TreeViewElement<ComparisonEntry>> comparisonEntryItem = ComparisonEntryTreeView.Pack(comparisonEntry, isAssetView, true);
                TreeViewExtensions.ReplaceItem(comparisonEntryTreeView, rootParentId, comparisonEntryItem);
                TreeViewExtensions.ExpandItem(comparisonEntryTreeView, comparisonEntryItem, true);
            }
        };

        FocusActions focusActions = this.Q<FocusActions>("focus-actions");
        focusActions.dataSource = new List<TreeViewItemData<TreeViewElement<Asset>>>(dataSource);
        focusActions.changed += (bool elementsChanged, int collectedCount, int processedCount) =>
        {
            /// Rebuild the comparison tree view elements if they need to be rebuilt.
            rebuildComparisonEntryTreeView();

            changed?.Invoke(elementsChanged, collectedCount, processedCount);
        };
        focusActions.Rebuild();
    }

    private static ComparisonEntry CreateEntry(HashSet<ComparisonEntry> comparisonEntries)
    {
        var result = new ComparisonEntry();
        result.entryName = "All Entries";
        result.entryType = typeof(HashSet<ComparisonEntry>);
        result.compareTargets = PopulateTargets(comparisonEntries);
        result.compareOperations = PopulateCompare(result.compareTargets);
        result.children = comparisonEntries;

        return result;
    }

    private static IReadOnlyDictionary<string, CompareOperand> PopulateTargets(IReadOnlyCollection<ComparisonEntry> comparisonEntries)
    {
        var properties = new Dictionary<string, CompareOperand>(3);
        var operations = new Dictionary<string, CompareOperation>(2);

        foreach (ComparisonEntry comparisonEntry in comparisonEntries)
        {
            bool isSame = true;
            foreach ((string key, CompareOperation operation) in comparisonEntry.compareOperations)
            {
                isSame &= operation.result;

                if (!operation.result)
                {
                    bool found = operations.TryGetValue(key, out CompareOperation allOperations);
                    if (!found)
                    {
                        allOperations = new CompareOperation(null, operandsList, getter, setter);
                        operations.Add(key, allOperations);
                    }
                }
            }

            foreach ((string key, CompareOperand operand) in comparisonEntry.compareTargets)
            {
                bool found = properties.TryGetValue(key, out CompareOperand allOperands);
                if (!found)
                {
                    var operandsList = new List<CompareOperand>(comparisonEntries.Count);
                    Func<object, object> getter = target => target;
                    Action<object, object> setter = (target, newValue) =>
                    {
                        var destination = target as List<CompareOperand>;
                        var source = target as List<CompareOperand>;

                        var d = destination.GetEnumerator();
                        var s = source.GetEnumerator();

                        while (d.MoveNext() && s.MoveNext())
                        {
                            d.Current.Value = s.Current.Value;
                        }
                    };

                    allOperands = new CompareOperand(null, operandsList, getter, setter);
                    properties.Add(key, allOperands);
                }

                if (allOperands.parentValue is List<CompareOperand> operands)
                {
                    operands.Add(operand);
                }
            }

            var processingData = compareTargets[ProcessingDataKey];
            var metafileData = compareTargets[MetafileDataKey];
            var addressablesData = compareTargets[AddressablesDataKey];

            var result = new Dictionary<string, CompareOperation>(2);
            result["processing-metafile-compare"] = new CompareOperation(comparer, processingData, metafileData);
            result["metafile-addressables-compare"] = new CompareOperation(comparer, metafileData, addressablesData);
        }

        return properties;
    }
    #endregion
}
