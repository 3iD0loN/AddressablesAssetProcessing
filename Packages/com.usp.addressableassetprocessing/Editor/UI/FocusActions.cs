using System;
using System.Collections.Generic;

using UnityEngine.UIElements;

using USP.AddressablesAssetProcessing;
using static AddressablesProcessingWindow;

[UxmlElement("FocusActions")]
public partial class FocusActions : VisualElement
{
    #region Static
    public static List<TreeViewItemData<TreeViewElement<Asset>>> Pack(IEnumerable<Asset> assets)
    {
        var result = new List<TreeViewItemData<TreeViewElement<Asset>>>();
        foreach (Asset asset in assets)
        {
            TreeViewItemData<TreeViewElement<Asset>> item = Pack(asset);
            result.Add(item);
        }

        return result;
    }

    public static TreeViewItemData<TreeViewElement<Asset>> Pack(Asset asset)
    {
        List<TreeViewItemData<TreeViewElement<Asset>>> childItems = null;
        if (asset is Folder folderState)
        {
            childItems = Pack(folderState.Children);
        }

        var item = new TreeViewElement<Asset>(true, asset);
        return new TreeViewItemData<TreeViewElement<Asset>>(asset.Id.GetHashCode(), item, childItems);
    }

    #endregion

    #region Fields
    private readonly VisualTreeAsset focusActionsUxml;

    //private readonly StyleSheet focusActionsUss;
    #endregion

    #region Properties
    public new IList<TreeViewItemData<TreeViewElement<Asset>>> dataSource { get; set; }
    #endregion

    #region Events
    public event Action<bool, int, int> changed;
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

            UpdateVisual(true, collectTargetButton, processTargetButton, collectAndProcessTargetButton);
        };

        processTargetButton.clicked += () =>
        {
            ProcessSelectedAssets(dataSource);

            UpdateVisual(false, collectTargetButton, processTargetButton, collectAndProcessTargetButton);
        };

        collectAndProcessTargetButton.clicked += () =>
        {
            IdentifySelectedAssets(dataSource);
            ProcessSelectedAssets(dataSource);

            UpdateVisual(true, collectTargetButton, processTargetButton, collectAndProcessTargetButton);            
        };

        UpdateVisual(false, collectTargetButton, processTargetButton, collectAndProcessTargetButton);
    }

    private void UpdateVisual(bool elementsChanged, VisualElement collectTargetButton, VisualElement processTargetButton, VisualElement collectAndProcessTargetButton)
    {
        int collectedCount = 0;
        int processedCount = 0;
        
        foreach (TreeViewItemData<TreeViewElement<Asset>> dataItem in dataSource)
        {
            collectedCount += dataItem.data.Value.IdentifiedCount;

            /// TODO: change this so that the rule processor has the result of the operation for those assets.
            processedCount += dataItem.data.Value.ProcessedData.Count;
        }

        collectTargetButton.style.display = Show(collectedCount == 0);
        processTargetButton.style.display = Show(collectedCount != 0 && processedCount == 0);
        collectAndProcessTargetButton.style.display = Show(collectedCount == 0);

        changed?.Invoke(elementsChanged, collectedCount, processedCount);
    }

    private string GetName()
    {
        var length = dataSource.Count;

        if (length == 1)
        {
            return dataSource[0].data.Value.Id;
        }

        string result = string.Empty;
        int lastIndex = length - 1;
        for (int i = 0; i < lastIndex; ++i)
        {
            result += dataSource[i].data.Value.Id + ", ";
        }

        result += "and " + dataSource[lastIndex].data.Value.Id;

        return result;
    }

    private DisplayStyle Show(bool value)
    {
        return value ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void IdentifySelectedAssets(IList<TreeViewItemData<TreeViewElement<Asset>>> selectedAssets)
    {
        for(int i = 0; i < selectedAssets.Count; ++i)
        {
            if (selectedAssets[i].data.Value is not Folder selectedFolder)
            {
                continue;
            }

            selectedFolder.Identify();

            // Overwrite this item with an item that has children.
            selectedAssets[i] = Pack(selectedFolder);
        }
    }

    private void ProcessSelectedAssets(IList<TreeViewItemData<TreeViewElement<Asset>>> selectedAssets)
    {
        foreach (TreeViewItemData<TreeViewElement<Asset>> selectedAsset in selectedAssets)
        {
            selectedAsset.data.Value.ProcessRules();
        }
    }
    #endregion
}
