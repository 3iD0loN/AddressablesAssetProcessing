using System;
using System.Collections.Generic;
using Unity.Android.Types;
using UnityEditor.Build.Pipeline;
using UnityEditor.IMGUI.Controls;
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

                if (selectedAsset.ProcessedData.Count == 0)
                {
                    continue;
                }

                selectedAsset.Compare(false);

                var selectedComparisonEntries = selectedAsset.ComparisonEntries;
                if (selectedComparisonEntries == null)
                {
                    continue;
                }

                comparisonEntries.UnionWith(selectedComparisonEntries);
            }

            // If the view is for a folder, then add a parent comparison entry that can control how all items are overwritten ar once.
            IEnumerable<ComparisonEntry> allComparisons = !isAssetView ? ComparisonEntries.CreateEntry(comparisonEntries) : comparisonEntries;

            List<TreeViewItemData<TreeViewElement<ComparisonEntry>>> comparisonEntryItems = ComparisonEntryTreeView.Pack(allComparisons, true, isAssetView);

            comparisonEntryTreeView.SetRootItems(comparisonEntryItems);
            TreeViewExtensions.ExpandItems(comparisonEntryTreeView, comparisonEntryItems, true);
            comparisonEntryTreeView.Rebuild();

        };

        rebuildComparisonEntryTreeView();
        comparisonEntryTreeView.changed += (int selectedIndex) =>
        {
            // Determines whether the asset selected items that are presented by the represent a folder or a single asset.
            bool isAssetView = true;
            foreach (TreeViewItemData<TreeViewElement<Asset>> selectedItem in dataSource)
            {
                Asset selectedAsset = selectedItem.data.Value;
                isAssetView &= selectedAsset is not Folder;
            }

            // If the asset field is focused on a single asset, or the its focused on a folder and the item isn't the first one ("All Entries")
            if (isAssetView || selectedIndex != 0)
            {
                Predicate<TreeViewElement<ComparisonEntry>> match = (TreeViewElement<ComparisonEntry> x) => x.Value.entryType == typeof(MetaAddressables.UserData);

                int rootParentId = TreeViewExtensions.FindFirstRootItemIdByIndex(comparisonEntryTreeView, selectedIndex, match);

                //int rootParentId = TreeViewExtensions.FindRootItemIdByIndex(comparisonEntryTreeView, selectedIndex);

                TreeViewElement<ComparisonEntry> previousComparisonEntryItem = comparisonEntryTreeView.GetItemDataForId<TreeViewElement<ComparisonEntry>>(rootParentId);

                RebuildComparisonEntry(comparisonEntryTreeView, rootParentId, previousComparisonEntryItem, isAssetView);
            }
            else if (!isAssetView && selectedIndex == 0)
            {
                var comparisonEntries = new HashSet<ComparisonEntry>();
                foreach (TreeViewItemData<TreeViewElement<Asset>> selectedItem in dataSource)
                {
                    Asset selectedAsset = selectedItem.data.Value;
                    selectedAsset.Compare(true);

                    var selectedComparisonEntries = selectedAsset.ComparisonEntries;
                    if (selectedComparisonEntries == null)
                    {
                        continue;
                    }

                    comparisonEntries.UnionWith(selectedComparisonEntries);
                }

                // If the view is for a folder, then add a parent comparison entry that can control how all items are overwritten ar once.
                IEnumerable<ComparisonEntry> allComparisons = ComparisonEntries.CreateEntry(comparisonEntries);

                List<TreeViewItemData<TreeViewElement<ComparisonEntry>>> comparisonEntryItems = ComparisonEntryTreeView.Pack(allComparisons, true, isAssetView);

                comparisonEntryTreeView.SetRootItems(comparisonEntryItems);
                TreeViewExtensions.ExpandItems(comparisonEntryTreeView, comparisonEntryItems, true);
                comparisonEntryTreeView.Rebuild();
            }
        };

        FocusActions focusActions = this.Q<FocusActions>("focus-actions");
        focusActions.dataSource = new List<TreeViewItemData<TreeViewElement<Asset>>>(dataSource);
        focusActions.changed += (bool elementsChanged, int collectedCount, int processedCount) =>
        {
            changed?.Invoke(elementsChanged, collectedCount, processedCount);

            // Rebuild the comparison tree view elements if they need to be rebuilt.
            rebuildComparisonEntryTreeView();
        };
        focusActions.Rebuild();
    }

    private static void RebuildComparisonEntry(BaseTreeView treeView, int id, TreeViewElement<ComparisonEntry> previousComparisonEntryItem, bool isExpanded)
    {
        // Get the asset path of the previous comparison item.
        string assetFilePath = previousComparisonEntryItem.Value.entryName;

        // Attempt to find the asset that was associated with the asset file path for comparisons.
        Asset.ByComparisonEntryAssetPath.TryGetValue(assetFilePath, out Asset asset);

        // Rebuild and overwrite the comparison entries associated with the asset.
        asset.Compare(true);

        /// For every comparisomn associated with the asset, perform the following:
        foreach (var comparisonEntry in asset.ComparisonEntries)
        {
            // Rebuild the presentation data for this entry.
            TreeViewItemData<TreeViewElement<ComparisonEntry>> comparisonEntryItem = ComparisonEntryTreeView.Pack(comparisonEntry, isExpanded, true);

            int parentId = treeView.viewController.GetParentId(id);

            // Replace the item in the tree view hierarchy.
            TreeViewExtensions.ReplaceItem(treeView, id, comparisonEntryItem, parentId);

            // Set the foldout values.
            TreeViewExtensions.ExpandItem(treeView, comparisonEntryItem, true);
        }
    }
    #endregion
}
