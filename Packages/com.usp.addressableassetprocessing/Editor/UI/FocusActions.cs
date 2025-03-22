using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UIElements;

using USP.AddressablesAssetProcessing;
using static AddressablesProcessingWindow;

[UxmlElement("FocusActions")]
public partial class FocusActions : VisualElement
{
    #region Fields
    private readonly VisualTreeAsset focusActionsUxml;

    //private readonly StyleSheet focusActionsUss;
    #endregion

    #region Properties
    public new (AddressableAssetSettings, List<TreeViewItemData<ProcessingState>>) dataSource { get; set; }
    #endregion

    #region Events
    public event System.Action<IEnumerable<TreeViewItemData<ProcessingState>>, int, int> changed;
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

        collectTargetButton.clicked += () =>
        {
            OnCollectAllAssets(dataSource.Item2);

            UpdateVisual(dataSource.Item2, collectTargetButton, processTargetButton, collectAndProcessTargetButton);
        };

        processTargetButton.clicked += () =>
        {
            OnProcessAllAssets(dataSource.Item1, dataSource.Item2);
            //OnCompareAssets(dataSource.Item1, dataSource.Item2, entriesByAssetFilepath);

            UpdateVisual(dataSource.Item2, collectTargetButton, processTargetButton, collectAndProcessTargetButton);
        };

        collectAndProcessTargetButton.clicked += () =>
        {
            OnCollectAndProcessAllAssets(dataSource.Item1, dataSource.Item2);
            //OnCompareAssets(dataSource.Item1, dataSource.Item2, entriesByAssetFilepath);

            UpdateVisual(dataSource.Item2, collectTargetButton, processTargetButton, collectAndProcessTargetButton);            
        };

        UpdateVisual(dataSource.Item2, collectTargetButton, processTargetButton, collectAndProcessTargetButton);
    }

    private void UpdateVisual(IEnumerable<TreeViewItemData<ProcessingState>> dataItems, VisualElement collectTargetButton, VisualElement processTargetButton, VisualElement collectAndProcessTargetButton)
    {
        int collectedCount = 0;
        int processedCount = 0;

        foreach (TreeViewItemData<ProcessingState> dataItem in dataItems)
        {
            if (dataItem.data is FolderCollectionAndProcessingState processingState)
            {
                collectedCount += processingState.assetStates.Count;
            }
            else if (dataItem.data is AssetCollectionAndProcessingState)
            {
                // Count up this one asset.
                collectedCount++;
            }

            processedCount += dataItem.data.assetApplicator.AssetStore.DataByAssetPath.Count;
        }

        collectTargetButton.style.display = Show(collectedCount == 0);
        processTargetButton.style.display = Show(collectedCount != 0 && processedCount == 0);
        collectAndProcessTargetButton.style.display = Show(collectedCount == 0);

        changed?.Invoke(dataSource.Item2, collectedCount, processedCount);
    }

    private DisplayStyle Show(bool value)
    {
        return value ? DisplayStyle.Flex : DisplayStyle.None;
    }

    #region Collection and Processing States for UI
    private void OnCollectAllAssets(List<TreeViewItemData<ProcessingState>> folderItems)
    {
        for (int i = 0; i < folderItems.Count; ++i)
        {
            folderItems[i] = OnCollectFolderAssets(folderItems[i]);
        }
    }

    private TreeViewItemData<ProcessingState> OnCollectFolderAssets(TreeViewItemData<ProcessingState> folderItem)
    {
        if (folderItem.data is not FolderCollectionAndProcessingState folderState)
        {
            return folderItem;
        }

        if (!folderState.enabled)
        {
            return folderItem;
        }

        CollectAssets(folderState);

        var assetItems = folderItem.children as List<TreeViewItemData<ProcessingState>>;
        assetItems.Clear();
        foreach (var assetState in folderState.assetStates)
        {
            var assetItem = new TreeViewItemData<ProcessingState>(assetState.assetGuid.GetHashCode(), assetState, null);
            assetItems.Add(assetItem);
        }

        return folderItem;
    }

    private void OnProcessAllAssets(AddressableAssetSettings settings, List<TreeViewItemData<ProcessingState>> folderItems)
    {
        for (int i = 0; i < folderItems.Count; ++i)
        {
            folderItems[i] = OnProcessFolderAssets(settings, folderItems[i]);
        }
    }

    private TreeViewItemData<ProcessingState> OnProcessFolderAssets(AddressableAssetSettings settings, TreeViewItemData<ProcessingState> folderItem)
    {
        if (folderItem.data is not FolderCollectionAndProcessingState folderState)
        {
            return folderItem;
        }

        if (!folderState.enabled)
        {
            return folderItem;
        }

        ProcessAssets(settings, folderState);

        return folderItem;
    }

    private void OnCollectAndProcessAllAssets(AddressableAssetSettings settings, List<TreeViewItemData<ProcessingState>> folderItems)
    {
        OnCollectAllAssets(folderItems);

        OnProcessAllAssets(settings, folderItems);
    }

    private void OnCollectAndProcessFolderAssets(AddressableAssetSettings settings, TreeViewItemData<ProcessingState> folderItem)
    {
        OnCollectFolderAssets(folderItem);

        OnProcessFolderAssets(settings, folderItem);
    }
    #endregion

    #region Compare States for UI
    private void OnCompareAssets(AddressableAssetSettings settings, List<TreeViewItemData<ProcessingState>> folderItems, Dictionary<string, ComparisonEntry> entriesByAssetpath)
    {
        for (int i = 0; i < folderItems.Count; ++i)
        {
            OnCompareFolderAssets(settings, folderItems[i], entriesByAssetpath);
        }
    }

    private void OnCompareFolderAssets(AddressableAssetSettings settings, TreeViewItemData<ProcessingState> folderItem, Dictionary<string, ComparisonEntry> entriesByAssetpath)
    {
        if (folderItem.data is not FolderCollectionAndProcessingState folderState)
        {
            return;
        }

        CompareAssets(settings, folderState, entriesByAssetpath);
    }
    #endregion

    #region Collection and Processing States
    private static void CollectAssets(FolderCollectionAndProcessingState folderState)
    {
        if (!folderState.enabled)
        {
            return;
        }

        var assetFilePaths = new Dictionary<string, string>();

        // Collect the prefab assets that are under the parent directory path.
        folderState.assetpathCollector.GetFiles(folderState.path, ref assetFilePaths);

        foreach ((string guid, string path) in assetFilePaths)
        {
            var assetState = new AssetCollectionAndProcessingState()
            {
                assetGuid = guid,
                path = path,
                assetpathCollector = folderState.assetpathCollector,
                groupExtractor = folderState.groupExtractor,
                addressExtractor = folderState.addressExtractor,
                labelExtractor = folderState.labelExtractor,
                assetApplicator = folderState.assetApplicator,
            };

            folderState.assetStates.Add(assetState);
        }
    }

    private static void ProcessAssets(AddressableAssetSettings settings, FolderCollectionAndProcessingState folderState)
    {
        if (!folderState.enabled)
        {
            return;
        }

        if (settings == null)
        {
            return;
        }

        foreach (AssetCollectionAndProcessingState assetState in folderState.assetStates)
        {
            ProcessAsset(settings, assetState);
        }
    }

    private static void ProcessAsset(AddressableAssetSettings settings, AssetCollectionAndProcessingState assetState)
    {
        if (settings == null || !assetState.enabled)
        {
            return;
        }

        AddressablesProcessing.ProcessAsset(settings,
            assetState.path,
            assetState.groupExtractor,
            assetState.addressExtractor,
            assetState.labelExtractor,
            assetState.assetApplicator);
    }

    private void CollectAndProcessAssets(AddressableAssetSettings settings, List<ProcessingState> folderStates)
    {
        foreach (FolderCollectionAndProcessingState folderState in folderStates)
        {
            CollectAssets(folderState);
        }

        foreach (FolderCollectionAndProcessingState folderState in folderStates)
        {
            ProcessAssets(settings, folderState);
        }
    }
    #endregion

    #region Compare States
    private void CompareAssets(AddressableAssetSettings settings, List<ProcessingState> folderStates,
        Dictionary<string, ComparisonEntry> entriesByAssetpath)
    {
        foreach (FolderCollectionAndProcessingState folderState in folderStates)
        {
            CompareAssets(settings, folderState, entriesByAssetpath);
        }
    }

    private void CompareAssets(AddressableAssetSettings settings, FolderCollectionAndProcessingState folderState,
        Dictionary<string, ComparisonEntry> entriesByAssetpath)
    {
        if (folderState.assetApplicator is not CombinedAssetApplicator combinedAssetApplicator)
        {
            return;
        }

        foreach (AssetCollectionAndProcessingState assetState in folderState.assetStates)
        {
            CompareAsset(settings, combinedAssetApplicator, assetState.path, entriesByAssetpath);
        }
    }

    private void CompareAsset(AddressableAssetSettings settings, CombinedAssetApplicator combinedAssetApplicator, string assetFilePath,
        Dictionary<string, ComparisonEntry> entriesByAssetpath)
    {
        if (entriesByAssetpath.TryGetValue(assetFilePath, out ComparisonEntry comparisonEntry))
        {
            Debug.LogWarning($"Collision for '{assetFilePath}'");
            return;
        }

        comparisonEntry = ComparisonEntries.CreateEntry(settings, combinedAssetApplicator, assetFilePath);
        entriesByAssetpath.Add(assetFilePath, comparisonEntry);
    }
    #endregion
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
