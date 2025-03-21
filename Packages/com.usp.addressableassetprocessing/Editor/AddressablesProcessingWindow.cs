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

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressablesAssetStore addressablesAssetStore = BuildAddressablesAssetStore(settings);
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

        var deduplicateButton = rootVisualElement.Q<Button>("deduplicate-all-button");

        deduplicateButton.clicked += OnDeduplicateAssets;

        var selectedState = rootVisualElement.Q<VisualElement>("selected-processing-state");
        mainTreeView.itemsChosen += (notUsed_selectedItems) =>
        {
            selectedState.Clear();
            assetStateUxml.CloneTree(selectedState);

            IEnumerable<TreeViewItemData<ProcessingState>> selectedItems = mainTreeView.GetSelectedItems<ProcessingState>();

            FocusActions focusActions = selectedState.Q<FocusActions>("focus-actions");
            focusActions.dataSource = (settings, new List<TreeViewItemData<ProcessingState>>(selectedItems));
            focusActions.changed += (IEnumerable<TreeViewItemData<ProcessingState>> selectedItems, int collectedCount, int processedCount) =>
            {
                mainTreeView.SetRootItems(folderItems);

                foreach (TreeViewItemData<ProcessingState> selectedItem in selectedItems)
                {
                    mainTreeView.ExpandItem(selectedItem.id, true);
                }

                mainTreeView.Rebuild();

                deduplicateButton.SetEnabled(collectedCount != 0 && processedCount != 0);
            };
            focusActions.Rebuild();

            /*/
            ComparisonEntryTreeView comparisonEntryView = selectedState.Q<ComparisonEntryTreeView>("comparison-entry");

            System.Action update = () =>
            {
                var comparisonEntries = new List<ComparisonEntry>();
                foreach (TreeViewItemData<ProcessingState> selectedItem in selectedItems)
                {
                    var combinedAssetApplicator = selectedItem.data.assetApplicator as CombinedAssetApplicator;
                    ComparisonEntry comparisonEntry = ComparisonEntries.CreateEntry(settings, combinedAssetApplicator, assetState.path);
                    comparisonEntries.Add(comparisonEntry);
                }

                var comparisonEntryItems = new List<TreeViewItemData<ComparisonEntry>>();
                Pack(comparisonEntries, comparisonEntryItems);
                comparisonEntryView.SetRootItems(comparisonEntryItems);
                comparisonEntryView.Rebuild();
                comparisonEntryView.ExpandAll();
            };
            comparisonEntryView.changed += update;
            update();
            //*/
        };
    }

    private AddressablesAssetStore BuildAddressablesAssetStore(AddressableAssetSettings settings)
    {
        if (settings == null)
        {
            return null;
        }

        var result = new AddressablesAssetStore();

        result.AddGlobalLabels(settings);

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

    #region BuildStates
    protected abstract List<ProcessingState> BuildStates(AddressablesAssetStore addressablesAssetStore);
    #endregion

    #region Deduplication
    private void OnDeduplicateAssets()
    {
        var groupExtractor = CreateDeduplicateGroupExtractor();

        var addressExtractor = new SimplifiedAddressExtractor();

        var assetApplicator = new MetaAddressablesAssetApplicator();

        AddressablesDeduplicator.ProcessAssets(groupExtractor, addressExtractor, assetApplicator);
    }

    protected abstract IGroupExtractor<HashSet<MetaAddressables.GroupData>> CreateDeduplicateGroupExtractor();
    #endregion
    #endregion
}
