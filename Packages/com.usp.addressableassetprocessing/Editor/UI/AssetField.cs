using System;
using System.Collections.Generic;

using UnityEngine.UIElements;

using USP.AddressablesAssetProcessing;

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
    #endregion
}
