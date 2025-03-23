using System.Collections.Generic;

using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

using USP.AddressablesAssetProcessing;
using USP.MetaAddressables;
using System.Linq;

public abstract class AddressablesProcessingWindow : EditorWindow
{
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
        var windowUss = FileHelper.LoadRequired<StyleSheet>("StyleSheet\\Window.uss");
        rootVisualElement.styleSheets.Add(windowUss);

        var windowUxml = FileHelper.LoadRequired<VisualTreeAsset>("UXML\\Window.uxml");
        windowUxml.CloneTree(rootVisualElement);

        VisualElement globalState = rootVisualElement.Q<VisualElement>("global-settings");
        CreateGlobalStateGUI(globalState);

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressablesAssetStore addressablesAssetStore = BuildAddressablesAssetStore(settings);
        List<Folder> folders = BuildFolderStates(addressablesAssetStore);
        
        MultiColumnTreeView mainTreeView = rootVisualElement.Q<MultiColumnTreeView>("main-tree-view");
        List<TreeViewItemData<Asset>> folderItems = FocusActions.Pack(folders);
        mainTreeView.SetRootItems(folderItems);
        mainTreeView.ExpandAll();

        var assetStateUxml = FileHelper.LoadRequired<VisualTreeAsset>("UXML\\AssetState.uxml");

        Column enabledColumn = mainTreeView.columns["enabled"];
        enabledColumn.makeCell = () => new Toggle();
        enabledColumn.bindCell = (VisualElement element, int index) =>
        {
            if (element is not Toggle toggle)
            {
                return;
            }

            var state = mainTreeView.GetItemDataForIndex<Asset>(index);

            toggle.value = state.IsEnabled;
            toggle.RegisterCallback<ChangeEvent<bool>>(@event => state.IsEnabled = @event.newValue);
        };

        Column pathColumn = mainTreeView.columns["path"];
        pathColumn.makeCell = () => new Label();
        pathColumn.bindCell = (VisualElement element, int index) =>
        {
            if (element is not Label label)
            {
                return;
            }

            var state = mainTreeView.GetItemDataForIndex<Asset>(index);

            label.text = state.Id;
        };

        var deduplicateButton = rootVisualElement.Q<Button>("deduplicate-all-button");

        deduplicateButton.clicked += OnDeduplicateAssets;

        var selectedState = rootVisualElement.Q<VisualElement>("selected-processing-state");
        mainTreeView.itemsChosen += (notUsed_selectedItems) =>
        {
            selectedState.Clear();
            assetStateUxml.CloneTree(selectedState);

            IEnumerable<TreeViewItemData<Asset>> selectedItems = mainTreeView.GetSelectedItems<Asset>();

            ComparisonEntryTreeView comparisonEntryTreeView = selectedState.Q<ComparisonEntryTreeView>("comparison-entry");
            System.Action updateComparisonEntryTreeView = () =>
            {
                var comparisonEntries = new HashSet<ComparisonEntry>();
                foreach (TreeViewItemData<Asset> selectedItem in selectedItems)
                {
                    if (selectedItem.data.ComparisonEntries == null)
                    {
                        continue;
                    }

                    comparisonEntries.UnionWith(selectedItem.data.ComparisonEntries);
                }

                List<TreeViewItemData<ComparisonEntry>> comparisonEntryItems = ComparisonEntryTreeView.Pack(comparisonEntries);
                comparisonEntryTreeView.SetRootItems(comparisonEntryItems);
                comparisonEntryTreeView.Rebuild();
                comparisonEntryTreeView.ExpandAll();
                foreach (TreeViewItemData<ComparisonEntry> comparisonEntryItem in comparisonEntryItems)
                {
                    comparisonEntryTreeView.CollapseItem(comparisonEntryItem.id, false);
                }
            };

            comparisonEntryTreeView.changed += updateComparisonEntryTreeView;
            updateComparisonEntryTreeView();

            FocusActions focusActions = selectedState.Q<FocusActions>("focus-actions");
            focusActions.dataSource = new List<TreeViewItemData<Asset>>(selectedItems);
            focusActions.changed += (IEnumerable<TreeViewItemData<Asset>> selectedItems, int collectedCount, int processedCount) =>
            {
                folderItems = FocusActions.Pack(folders);
                mainTreeView.SetRootItems(folderItems);
                mainTreeView.ExpandAll();
                mainTreeView.Rebuild();

                updateComparisonEntryTreeView();

                deduplicateButton.SetEnabled(collectedCount != 0 && processedCount != 0);
            };
            focusActions.Rebuild();
        };
    }

    private AddressablesAssetStore BuildAddressablesAssetStore(AddressableAssetSettings settings)
    {
        if (settings == null)
        {
            return null;
        }

        return new AddressablesAssetStore(settings);
    }

    protected abstract void CreateGlobalStateGUI(VisualElement globalState);
    #region BuildStates
    protected abstract List<Folder> BuildFolderStates(AddressablesAssetStore addressablesAssetStore);
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
