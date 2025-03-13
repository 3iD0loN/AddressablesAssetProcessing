using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

using USP.AddressablesAssetProcessing;
using USP.MetaAddressables;

using Object = UnityEngine.Object;
using UnityEditor.AddressableAssets.Build;
using System.Linq;
using UnityEditorInternal;

public static class Helper
{
    public static T Load<T>(string assetPath) where T : Object
    {
        const string WindowfilePath = "Packages\\com.usp.addressableassetprocessing\\Editor\\";

        return AssetDatabase.LoadAssetAtPath<T>(WindowfilePath + assetPath);
    }

    public static T LoadRequired<T>(string assetPath) where T : Object
    {
        T result = Load<T>(assetPath);

        if (!result)
        {
            Debug.LogError($"Unable to find required resource '{assetPath}'");
        }

        return result;
    }
}

public abstract class AddressablesProcessingWindow : EditorWindow
{
    #region Types
    public class ProcessingState
    {
        public bool enabled = true;

        public string name;

        public IExtractor<string, AddressableAssetGroupTemplate> groupExtractor;

        public IExtractor<string, string> addressExtractor;

        public IExtractor<string, HashSet<string>> labelExtractor;

        public IAssetApplicator assetApplicator;
    }

    public class CollectionAndProcessingState : ProcessingState
    {
        public string path
        {
            get => name;
            set => name = value;
        }

        public AssetPathCollector assetpathCollector;
    }

    public class AssetCollectionAndProcessingState : CollectionAndProcessingState
    {
        public string assetGuid;
    }

    public class FolderCollectionAndProcessingState : CollectionAndProcessingState
    {
        public List<AssetCollectionAndProcessingState> assetStates = new List<AssetCollectionAndProcessingState>();
    }
    #endregion

    #region Static Methods
    #region Addressables Event Subscription
    [InitializeOnLoadMethod]
    private static void SubscribeToEvents()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        settings.OnModification -= OnSettingsModification;
        settings.OnModification += OnSettingsModification;
    }

    private static void OnSettingsModification(AddressableAssetSettings settings,
        AddressableAssetSettings.ModificationEvent modificationEvent, object eventData)
    {
        switch (modificationEvent)
        {
            case AddressableAssetSettings.ModificationEvent.GroupAdded:
            case AddressableAssetSettings.ModificationEvent.GroupRemoved:
            case AddressableAssetSettings.ModificationEvent.EntryAdded:
                var entriesCreated = eventData as List<AddressableAssetEntry>;
                break;
            case AddressableAssetSettings.ModificationEvent.EntryMoved:
                var entriesMoved = eventData as List<AddressableAssetEntry>;
                
                break;
            case AddressableAssetSettings.ModificationEvent.EntryRemoved:
                var removedEntries = eventData as List<AddressableAssetEntry>;
                break;
            case AddressableAssetSettings.ModificationEvent.GroupRenamed:
            case AddressableAssetSettings.ModificationEvent.EntryModified:
            case AddressableAssetSettings.ModificationEvent.BatchModification:
                break;
        }
    }
    #endregion
    #endregion

    #region Methods
    private void CreateGUI()
    {
        var windowUss = Helper.LoadRequired<StyleSheet>("StyleSheet\\Window.uss");
        rootVisualElement.styleSheets.Add(windowUss);

        var windowUxml = Helper.LoadRequired<VisualTreeAsset>("UXML\\Window.uxml");
        windowUxml.CloneTree(rootVisualElement);

        var folderStateUxml = Helper.LoadRequired<VisualTreeAsset>("UXML\\FolderState.uxml");

        var assetStateUxml = Helper.LoadRequired<VisualTreeAsset>("UXML\\AssetState.uxml");

        VisualElement globalState = rootVisualElement.Q<VisualElement>("global-settings");
        CreateGlobalStateGUI(globalState);

        AddressablesAssetStore addressablesAssetStore = BuildAddressablesAssetStore();
        List<ProcessingState> folderStates = BuildStates(addressablesAssetStore);

        var entriesByAssetFilepath = new Dictionary<string, ComparisonEntry>();
        var folderItems = new List<TreeViewItemData<ProcessingState>>();
        Pack(folderStates, folderItems);

        MultiColumnTreeView mainTreeView = rootVisualElement.Q<MultiColumnTreeView>("main-tree-view");
        mainTreeView.SetRootItems(folderItems);

        /*/
        Column enabledColumn = mainTreeView.columns["enabled"];
        enabledColumn.makeCell = () => new Toggle();
        enabledColumn.bindCell = (VisualElement element, int index) =>
        {
            if (element is not Toggle toggle)
            {
                return;
            }

            var state = (TreeViewItemData<ProcessingState>)mainTreeView.itemsSource[index];

            Debug.Log("xxx");
            //toggle.value = state.enabled;
        };
        //*/

        Column pathColumn = mainTreeView.columns["path"];
        pathColumn.makeCell = () => new Label();
        pathColumn.bindCell = (VisualElement element, int index) =>
        {
            if (element is not Label label)
            {
                return;
            }

            var state = mainTreeView.GetItemDataForIndex<ProcessingState>(index);

            label.text = state.name;
        };

        var selectedState = rootVisualElement.Q<VisualElement>("selected-processing-state");
        mainTreeView.itemsChosen += (notUsed_selectedItems) =>
        {
            IEnumerable<TreeViewItemData<ProcessingState>> selectedItems = mainTreeView.GetSelectedItems<ProcessingState>();

            foreach (TreeViewItemData<ProcessingState> selectedItem in selectedItems)
            {
                selectedState.Clear();

                if (selectedItem.data is FolderCollectionAndProcessingState folderState)
                {
                    folderStateUxml.CloneTree(selectedState);

                    var collectFolderAssetsButton = selectedState.Q<Button>("collect-folder-button");
                    var collectAndProcessFolderAssetsButton = selectedState.Q<Button>("collect-and-process-folder-button");
                    var processFolderButton = selectedState.Q<Button>("process-folder-button");

                    collectFolderAssetsButton.SetEnabled(folderState.assetStates.Count != 0);

                    collectFolderAssetsButton.clicked += () =>
                    {
                        OnCollectFolderAssets(selectedItem);

                        mainTreeView.SetRootItems(folderItems);
                        mainTreeView.ExpandItem(selectedItem.id, true);
                        mainTreeView.Rebuild();

                        collectFolderAssetsButton.SetEnabled(false);
                        collectAndProcessFolderAssetsButton.SetEnabled(false);
                        processFolderButton.SetEnabled(true);
                    };

                    collectAndProcessFolderAssetsButton.clicked += () =>
                    {
                        OnCollectAndProcessFolderAssets(selectedItem);
                        OnCompareFolderAssets(selectedItem, entriesByAssetFilepath);

                        mainTreeView.SetRootItems(folderItems);
                        mainTreeView.ExpandItem(selectedItem.id, true);
                        mainTreeView.Rebuild();

                        collectFolderAssetsButton.SetEnabled(false);
                        collectAndProcessFolderAssetsButton.SetEnabled(false);
                        processFolderButton.SetEnabled(false);
                    };
                    
                    processFolderButton.clicked += () =>
                    {
                        OnProcessFolderAssets(selectedItem);
                        OnCompareFolderAssets(selectedItem, entriesByAssetFilepath);

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
                    assetStateUxml.CloneTree(selectedState);

                    var combinedAssetApplicator = assetState.assetApplicator as CombinedAssetApplicator;
                    ComparisonEntry comparisonEntry = ComparisonEntries.CreateEntry(combinedAssetApplicator, assetState.path);
                    var comparisonEntryItems = new List<TreeViewItemData<ComparisonEntry>>();
                    Pack(comparisonEntry, comparisonEntryItems);

                    ComparisonEntryTreeView comparisonEntryView = rootVisualElement.Q<ComparisonEntryTreeView>("comparison-entry");
                    comparisonEntryView.SetRootItems(comparisonEntryItems);
                    comparisonEntryView.ExpandAll();
                }
            }
        };

        var collectAllAssetsButton = rootVisualElement.Q<Button>("collect-all-button");
        var collectAndProcessAllAssetsButton = rootVisualElement.Q<Button>("collect-and-process-all-button");
        var processAllButton = rootVisualElement.Q<Button>("process-all-button");
        var deduplicateButton = rootVisualElement.Q<Button>("deduplicate-all-button");

        collectAllAssetsButton.clicked += () =>
        {
            OnCollectAllAssets(folderItems);

            mainTreeView.SetRootItems(folderItems);
            mainTreeView.ExpandAll();
            mainTreeView.Rebuild();

            collectAllAssetsButton.SetEnabled(false);
            collectAndProcessAllAssetsButton.SetEnabled(false);
            processAllButton.SetEnabled(true);
            deduplicateButton.SetEnabled(false);
        };

        collectAndProcessAllAssetsButton.clicked += () =>
        {
            OnCollectAndProcessAllAssets(folderItems);
            OnCompareAssets(folderItems, entriesByAssetFilepath);

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
            OnProcessAllAssets(folderItems);
            OnCompareAssets(folderItems, entriesByAssetFilepath);

            mainTreeView.SetRootItems(folderItems);
            mainTreeView.ExpandAll();
            mainTreeView.Rebuild();

            collectAllAssetsButton.SetEnabled(false);
            collectAndProcessAllAssetsButton.SetEnabled(false);
            processAllButton.SetEnabled(false);
            deduplicateButton.SetEnabled(true);
        };

        deduplicateButton.clicked += OnDeduplicateAssets;
    }

    private AddressablesAssetStore BuildAddressablesAssetStore()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null)
        {
            return null;
        }

        var result = new AddressablesAssetStore();

        result.CreateGlobalLabels(settings);

        foreach (AddressableAssetGroup group in settings.groups)
        {
            foreach (AddressableAssetEntry entry in group.entries)
            {
                result.AddAsset(entry);
            }
        }

        return result;
    }

    protected abstract void CreateGlobalStateGUI(VisualElement globalState);

    #region Pack for UI
    private void Pack(IEnumerable<ProcessingState> states, List<TreeViewItemData<ProcessingState>> result)
    {
        foreach (ProcessingState state in states)
        {
            Pack(state, result);
        }
    }

    private void Pack(ProcessingState state, List<TreeViewItemData<ProcessingState>> result)
    {
        if (state is AssetCollectionAndProcessingState assetState)
        {
            Pack(assetState, result);
        }
        else if (state is FolderCollectionAndProcessingState folderState)
        {
            Pack(folderState, result);
        }
    }

    private void Pack(AssetCollectionAndProcessingState assetState, List<TreeViewItemData<ProcessingState>> result)
    {
        string guid = assetState.assetGuid;

        result.Add(new TreeViewItemData<ProcessingState>(guid.GetHashCode(), assetState, null));
    }

    private void Pack(FolderCollectionAndProcessingState folderState, List<TreeViewItemData<ProcessingState>> result)
    {
        string guid = AssetDatabase.AssetPathToGUID(folderState.path);

        var childItems = new List<TreeViewItemData<ProcessingState>>();
        Pack(folderState.assetStates, childItems);

        result.Add(new TreeViewItemData<ProcessingState>(guid.GetHashCode(), folderState, childItems));
    }

    private void Pack(IEnumerable<ComparisonEntry> comparisonEntries, List<TreeViewItemData<ComparisonEntry>> result)
    {
        foreach(var comparisonEntry in comparisonEntries)
        {
            Pack(comparisonEntry, result);
        }
    }

    private void Pack(ComparisonEntry comparisonEntry, List<TreeViewItemData<ComparisonEntry>> result)
    {

        var childItems = new List<TreeViewItemData<ComparisonEntry>>();
        if (comparisonEntry.children != null)
        {
            Pack(comparisonEntry.children, childItems);
        }
        
        result.Add(new TreeViewItemData<ComparisonEntry>(comparisonEntry.GetHashCode(), comparisonEntry, childItems));
    }
    #endregion

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

    private void OnProcessAllAssets(List<TreeViewItemData<ProcessingState>> folderItems)
    {
        for (int i = 0; i < folderItems.Count; ++i)
        {
            folderItems[i] = OnProcessFolderAssets(folderItems[i]);
        }
    }

    private TreeViewItemData<ProcessingState> OnProcessFolderAssets(TreeViewItemData<ProcessingState> folderItem)
    {
        if (folderItem.data is not FolderCollectionAndProcessingState folderState)
        {
            return folderItem;
        }

        if (!folderState.enabled)
        {
            return folderItem;
        }

        ProcessAssets(folderState);

        return folderItem;
    }

    private void OnCollectAndProcessAllAssets(List<TreeViewItemData<ProcessingState>> folderItems)
    {
        OnCollectAllAssets(folderItems);

        OnProcessAllAssets(folderItems);
    }

    private void OnCollectAndProcessFolderAssets(TreeViewItemData<ProcessingState> folderItem)
    {
        OnCollectFolderAssets(folderItem);

        OnProcessFolderAssets(folderItem);
    }
    #endregion

    #region Compare States for UI
    private void OnCompareAssets(List<TreeViewItemData<ProcessingState>> folderItems, Dictionary<string, ComparisonEntry> entriesByAssetpath)
    {
        for (int i = 0; i < folderItems.Count; ++i)
        {
            OnCompareFolderAssets(folderItems[i], entriesByAssetpath);
        }
    }

    private void OnCompareFolderAssets(TreeViewItemData<ProcessingState> folderItem, Dictionary<string, ComparisonEntry> entriesByAssetpath)
    {
        if (folderItem.data is not FolderCollectionAndProcessingState folderState)
        {
            return;
        }

        CompareAssets(folderState, entriesByAssetpath);
    }
    #endregion

    #region BuildStates
    protected abstract List<ProcessingState> BuildStates(AddressablesAssetStore addressablesAssetStore);
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

    private static void ProcessAssets(FolderCollectionAndProcessingState folderState)
    {
        if (!folderState.enabled)
        {
            return;
        }

        var settings = AddressableAssetSettingsDefaultObject.Settings;

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

    private void CollectAndProcessAssets(List<ProcessingState> folderStates)
    {
        foreach (FolderCollectionAndProcessingState folderState in folderStates)
        {
            CollectAssets(folderState);
        }

        foreach (FolderCollectionAndProcessingState folderState in folderStates)
        {
            ProcessAssets(folderState);
        }
    }
    #endregion

    #region Compare States
    private void CompareAssets(List<ProcessingState> folderStates, Dictionary<string, ComparisonEntry> entriesByAssetpath)
    {
        foreach (FolderCollectionAndProcessingState folderState in folderStates)
        {
            CompareAssets(folderState, entriesByAssetpath);
        }
    }

    private void CompareAssets(FolderCollectionAndProcessingState folderState, Dictionary<string, ComparisonEntry> entriesByAssetpath)
    {
        if(folderState.assetApplicator is not CombinedAssetApplicator combinedAssetApplicator)
        {
            return;
        }

        foreach (AssetCollectionAndProcessingState assetState in folderState.assetStates)
        {
            CompareAsset(combinedAssetApplicator, assetState.path, entriesByAssetpath);
        }
    }

    private void CompareAsset(CombinedAssetApplicator combinedAssetApplicator, string assetFilePath, Dictionary<string, ComparisonEntry> entriesByAssetpath)
    {
        if (entriesByAssetpath.TryGetValue(assetFilePath, out ComparisonEntry comparisonEntry))
        {
            Debug.LogWarning($"Collision for '{assetFilePath}'");
            return;
        }

        comparisonEntry = ComparisonEntries.CreateEntry(combinedAssetApplicator, assetFilePath);
        entriesByAssetpath.Add(assetFilePath, comparisonEntry);
    }
    #endregion

    private void OnDeduplicateAssets()
    {
        var groupExtractor = CreateDeduplicateGroupExtractor();

        var addressExtractor = new SimplifiedAddressExtractor();

        var assetApplicator = new MetaAddressablesAssetApplicator();

        AddressablesDeduplicator.ProcessAssets(groupExtractor, addressExtractor, assetApplicator);
    }

    protected abstract IGroupExtractor<HashSet<MetaAddressables.GroupData>> CreateDeduplicateGroupExtractor();
    #endregion
}
