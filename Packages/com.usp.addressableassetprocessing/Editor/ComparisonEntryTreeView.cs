using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using USP.AddressablesAssetProcessing;

[UxmlElement("ComparisonEntryTreeView")]
public partial class ComparisonEntryTreeView : MultiColumnTreeView
{
    #region Methods
    public ComparisonEntryTreeView()
    {
        var comparisonEntryUss = Helper.LoadRequired<StyleSheet>("StyleSheet\\ComparisonEntry.uss");
        var leftRightcompareEntryControlsUxml = Helper.LoadRequired<VisualTreeAsset>("UXML\\LeftRightCompareEntryControls.uxml");
        var rightcompareEntryControlsUxml = Helper.LoadRequired<VisualTreeAsset>("UXML\\RightCompareEntryControls.uxml");

        if (columns.Count == 0)
        {
            var comparisonEntryTreeViewUxml = Helper.LoadRequired<VisualTreeAsset>("UXML\\ComparisonEntryTreeView.uxml");
            var temp = new VisualElement();
            comparisonEntryTreeViewUxml.CloneTree(temp);
            var tempMultiColumnTreeView = temp.Q<MultiColumnTreeView>();

            columns.Add(tempMultiColumnTreeView.columns["meta-file-data"]);
            columns.Add(tempMultiColumnTreeView.columns["processing-meta-compare"]);
            columns.Add(tempMultiColumnTreeView.columns["file-processing-rules"]);
            columns.Add(tempMultiColumnTreeView.columns["metafile-addressables-compare"]);
            columns.Add(tempMultiColumnTreeView.columns["addressables-data"]);
        }

        Column processingColumn = columns["file-processing-rules"];
        processingColumn.makeCell = () =>
        {
            var result = new TextField();
            result.SetEnabled(false);
            return result;
        };
        processingColumn.bindCell = (VisualElement element, int index) =>
        {
            if (element is not TextField textField)
            {
                return;
            }

            var entry = GetItemDataForIndex<ComparisonEntry>(index);
            string value = entry.fileProcessingAsset != null ? entry.fileProcessingAsset.ToString() : "No Entry";

            textField.label = entry.entryName;
            textField.value = value;
        };

        Column processingMetaColumn = columns["processing-meta-compare"];
        processingMetaColumn.makeCell = () =>
        {
            var result = new VisualElement();
            leftRightcompareEntryControlsUxml.CloneTree(result);
            result.styleSheets.Add(comparisonEntryUss);

            return result;
        };
        processingMetaColumn.bindCell = (VisualElement element, int index) =>
        {
            var sameLabel = element.Q<Label>("same-label");
            var differentButtons = element.Q<VisualElement>("different-buttons");

            var copyLeftButton = element.Q<Button>("copy-left-button");
            var copyRightButton = element.Q<Button>("copy-right-button");

            //copyLeftButton.clicked -=
            //copyLeftButton.clicked +=

            //copyRightButton.clicked -=
            //copyRightButton.clicked +=

            var entry = GetItemDataForIndex<ComparisonEntry>(index);
            if (entry.leftCompare)
            {
                sameLabel.style.display = DisplayStyle.Flex;
                differentButtons.style.display = DisplayStyle.None;
            }
            else
            {
                sameLabel.style.display = DisplayStyle.None;
                differentButtons.style.display = DisplayStyle.Flex;
            }
        };

        Column metaFileDataColumn = columns["meta-file-data"];
        metaFileDataColumn.makeCell = () =>
        {
            var result = new TextField();
            result.SetEnabled(false);
            return result;
        };
        metaFileDataColumn.bindCell = (VisualElement element, int index) =>
        {
            if (element is not TextField textField)
            {
                return;
            }

            var entry = GetItemDataForIndex<ComparisonEntry>(index);
            string value = entry.metaDataAsset != null ? entry.metaDataAsset.ToString() : "No Entry";

            textField.label = entry.entryName;
            textField.value = value;
        };

        Column metafileAddressablesColumn = columns["metafile-addressables-compare"];
        metafileAddressablesColumn.makeCell = () =>
        {
            var result = new VisualElement();
            rightcompareEntryControlsUxml.CloneTree(result);
            result.styleSheets.Add(comparisonEntryUss);

            return result;
        };
        metafileAddressablesColumn.bindCell = (VisualElement element, int index) =>
        {
            var sameLabel = element.Q<Label>("same-label");
            var differentButtons = element.Q<VisualElement>("different-buttons");

            var copyRightButton = element.Q<Button>("copy-right-button");
            //copyRightButton.clicked -=
            //copyRightButton.clicked +=

            var entry = GetItemDataForIndex<ComparisonEntry>(index);
            if (entry.leftCompare)
            {
                sameLabel.style.display = DisplayStyle.Flex;
                differentButtons.style.display = DisplayStyle.None;
            }
            else
            {
                sameLabel.style.display = DisplayStyle.None;
                differentButtons.style.display = DisplayStyle.Flex;
            }
        };

        Column addressablesColumn = columns["addressables-data"];
        addressablesColumn.makeCell = () =>
        {
            var result = new TextField();
            result.SetEnabled(false);
            return result;
        };
        addressablesColumn.bindCell = (VisualElement element, int index) =>
        {
            if (element is not TextField textField)
            {
                return;
            }

            var entry = GetItemDataForIndex<ComparisonEntry>(index);
            string value = entry.addressablesAsset != null ? entry.addressablesAsset.ToString() : "No Entry";

            textField.label = entry.entryName;
            textField.value = value;
        };
    }

    public void SetRootItems(IList<TreeViewItemData<ComparisonEntry>> rootItems)
    {
        base.SetRootItems(rootItems);
    }
    #endregion
}
