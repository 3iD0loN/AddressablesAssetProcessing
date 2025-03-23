using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UIElements;

using USP.AddressablesAssetProcessing;
using static AddressablesProcessingWindow;

[UxmlElement("FocusActions")]
public partial class FocusActions : VisualElement
{
    #region Static
    public static List<TreeViewItemData<Asset>> Pack(IEnumerable<Asset> assets)
    {
        var result = new List<TreeViewItemData<Asset>>();
        foreach (Asset asset in assets)
        {
            TreeViewItemData<Asset> item = Pack(asset);
            result.Add(item);
        }

        return result;
    }

    public static TreeViewItemData<Asset> Pack(Asset asset)
    {
        List<TreeViewItemData<Asset>> childItems = null;
        if (asset is Folder folderState)
        {
            childItems = Pack(folderState.Children);
        }

        return new TreeViewItemData<Asset>(asset.Id.GetHashCode(), asset, childItems);
    }
    #endregion

    #region Fields
    private readonly VisualTreeAsset focusActionsUxml;

    //private readonly StyleSheet focusActionsUss;
    #endregion

    #region Properties
    public new List<TreeViewItemData<Asset>> dataSource { get; set; }
    #endregion

    #region Events
    public event System.Action<IEnumerable<TreeViewItemData<Asset>>, int, int> changed;
    #endregion

    #region Methods
    public FocusActions()
    {
        focusActionsUxml = FileHelper.LoadRequired<VisualTreeAsset>("UXML\\FocusActions.uxml");
        //focusActionsUss = Helper.LoadRequired<StyleSheet>("StyleSheet\\CompareOperand.uss");
    }

    public void Rebuild()
    {
        focusActionsUxml.CloneTree(this);
        //this.styleSheets.Add(focusActionsUss);

        var collectTargetButton = this.Q<Button>("collect-button");
        var processTargetButton = this.Q<Button>("process-button");
        var collectAndProcessTargetButton = this.Q<Button>("collect-and-process-button");

        string name = GetName();
        collectTargetButton.text = string.Format(collectTargetButton.text, name);
        processTargetButton.text = string.Format(processTargetButton.text, name);
        collectAndProcessTargetButton.text = string.Format(collectAndProcessTargetButton.text, name);

        collectTargetButton.clicked += () =>
        {
            IdentifySelectedAssets(dataSource);

            UpdateVisual(dataSource, collectTargetButton, processTargetButton, collectAndProcessTargetButton);
        };

        processTargetButton.clicked += () =>
        {
            ProcessSelectedAssets(dataSource);
            CompareSelectedAssets(dataSource);

            UpdateVisual(dataSource, collectTargetButton, processTargetButton, collectAndProcessTargetButton);
        };

        collectAndProcessTargetButton.clicked += () =>
        {
            IdentifySelectedAssets(dataSource);
            ProcessSelectedAssets(dataSource);
            CompareSelectedAssets(dataSource);

            UpdateVisual(dataSource, collectTargetButton, processTargetButton, collectAndProcessTargetButton);            
        };

        UpdateVisual(dataSource, collectTargetButton, processTargetButton, collectAndProcessTargetButton);
    }

    private void UpdateVisual(IEnumerable<TreeViewItemData<Asset>> dataItems, VisualElement collectTargetButton, VisualElement processTargetButton, VisualElement collectAndProcessTargetButton)
    {
        int collectedCount = 0;
        int processedCount = 0;
        
        foreach (TreeViewItemData<Asset> dataItem in dataItems)
        {
            collectedCount += dataItem.data.IdentifiedCount;

            /// TODO: change this so that the rule processor has the result of the operation for those assets.
            processedCount += dataItem.data.ProcessedData.Count;
        }

        collectTargetButton.style.display = Show(collectedCount == 0);
        processTargetButton.style.display = Show(collectedCount != 0 && processedCount == 0);
        collectAndProcessTargetButton.style.display = Show(collectedCount == 0);

        changed?.Invoke(dataSource, collectedCount, processedCount);
    }

    private string GetName()
    {
        var length = dataSource.Count;

        if (length == 1)
        {
            return dataSource[0].data.Id;
        }

        string result = string.Empty;
        int lastIndex = length - 1;
        for (int i = 0; i < lastIndex; ++i)
        {
            result += dataSource[i].data.Id + ", ";
        }

        result += "and " + dataSource[lastIndex].data.Id;

        return result;
    }

    private DisplayStyle Show(bool value)
    {
        return value ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void IdentifySelectedAssets(List<TreeViewItemData<Asset>> selectedAssets)
    {
        foreach(TreeViewItemData<Asset> selectedAsset in selectedAssets)
        {
            if (selectedAsset.data is not Folder selectedFolder)
            {
                continue;
            }

            selectedFolder.Identify();
        }
    }

    private void ProcessSelectedAssets(List<TreeViewItemData<Asset>> selectedAssets)
    {
        foreach (TreeViewItemData<Asset> selectedAsset in selectedAssets)
        {
            selectedAsset.data.ProcessRules();
        }
    }

    private void CompareSelectedAssets(List<TreeViewItemData<Asset>> selectedAssets)
    {
        foreach (TreeViewItemData<Asset> selectedAsset in selectedAssets)
        {
            selectedAsset.data.Compare();
        }
    }
    #endregion

    /*/
foreach (TreeViewItemData<ProcessingState> selectedItem in selectedItems)
{
    selectedState.Clear();

    if (selectedItem.data is FolderCollectionAndProcessingState folderState)
    {
        folderStateUxml.CloneTree(selectedState);

        var collectFolderAssetsButton = selectedState.Q<Button>("collect-folder-button");
        var collectAndProcessFolderAssetsButton = selectedState.Q<Button>("collect-and-process-folder-button");
        var processFolderButton = selectedState.Q<Button>("process-folder-button");

        collectFolderAssetsButton.SetEnabled(folderState.assetStates.Count == 0);
        collectAndProcessFolderAssetsButton.SetEnabled(folderState.assetApplicator.AssetStore.DataByAssetPath.Count == 0);
        processFolderButton.SetEnabled(folderState.assetStates.Count != 0 && folderState.assetApplicator.AssetStore.DataByAssetPath.Count == 0);

        collectFolderAssetsButton.clicked += () =>
        {
            OnCollectFolderAssets(selectedItem);



            collectFolderAssetsButton.SetEnabled(false);
            collectAndProcessFolderAssetsButton.SetEnabled(false);
            processFolderButton.SetEnabled(true);
        };

        collectAndProcessFolderAssetsButton.clicked += () =>
        {
            OnCollectAndProcessFolderAssets(settings, selectedItem);
            OnCompareFolderAssets(settings, selectedItem, entriesByAssetFilepath);

            mainTreeView.SetRootItems(folderItems);
            mainTreeView.ExpandItem(selectedItem.id, true);
            mainTreeView.Rebuild();

            collectFolderAssetsButton.SetEnabled(false);
            collectAndProcessFolderAssetsButton.SetEnabled(false);
            processFolderButton.SetEnabled(false);
        };

        processFolderButton.clicked += () =>
        {
            OnProcessFolderAssets(settings, selectedItem);
            OnCompareFolderAssets(settings, selectedItem, entriesByAssetFilepath);

            mainTreeView.SetRootItems(folderItems);
            mainTreeView.ExpandItem(selectedItem.id, true);
            mainTreeView.Rebuild();

            collectFolderAssetsButton.SetEnabled(false);
            collectAndProcessFolderAssetsButton.SetEnabled(false);
            processFolderButton.SetEnabled(false);
        };
    }
    else if (selectedItem.data is AssetCollectionAndProcessingState assetState)
    {

    }

}
//*/

    /*/
            collectAllAssetsButton.clicked += () =>
            {
                OnCollectAllAssets(folderItems);

                collectAllAssetsButton.SetEnabled(false);
                collectAndProcessAllAssetsButton.SetEnabled(false);
                processAllButton.SetEnabled(true);

                mainTreeView.SetRootItems(folderItems);
                mainTreeView.ExpandAll();
                mainTreeView.Rebuild();
                deduplicateButton.SetEnabled(false);
            };

            collectAndProcessAllAssetsButton.clicked += () =>
            {
                OnCollectAndProcessAllAssets(settings, folderItems);
                OnCompareAssets(settings, folderItems, entriesByAssetFilepath);

                mainTreeView.SetRootItems(folderItems);
                mainTreeView.ExpandAll();
                mainTreeView.Rebuild();

                collectAllAssetsButton.SetEnabled(false);
                collectAndProcessAllAssetsButton.SetEnabled(false);
                processAllButton.SetEnabled(false);
                deduplicateButton.SetEnabled(true);
            };

            processAllButton.clicked += () =>
            {
                OnProcessAllAssets(settings, folderItems);
                OnCompareAssets(settings, folderItems, entriesByAssetFilepath);

                mainTreeView.SetRootItems(folderItems);
                mainTreeView.ExpandAll();
                mainTreeView.Rebuild();

                collectAllAssetsButton.SetEnabled(false);
                collectAndProcessAllAssetsButton.SetEnabled(false);
                processAllButton.SetEnabled(false);
                deduplicateButton.SetEnabled(true);
            };
    //*/
}
