using System;
using UnityEngine.UIElements;

using USP.AddressablesAssetProcessing;

[UxmlElement("CompareOperationField")]
public partial class CompareOperationField : VisualElement
{
    #region Fields
    private readonly StyleSheet compareOperandUss;

    private readonly VisualTreeAsset compareOperandUxml;
    #endregion

    #region
    public new CompareOperation dataSource
    {
        get;
        set;
    }
    #endregion

    #region Events
    public event Action changed;
    #endregion

    #region Methods
    public CompareOperationField()
    {
        compareOperandUss = FileHelper.LoadRequired<StyleSheet>("StyleSheet\\CompareOperand.uss");
        compareOperandUxml = FileHelper.LoadRequired<VisualTreeAsset>("UXML\\CompareOperand.uxml");
    }

    public void Rebuild(Action changed = null)
    {
        this.changed = changed;
        this.Clear();
        compareOperandUxml.CloneTree(this);
        this.styleSheets.Add(compareOperandUss);

        var sameLabel = this.Q<Label>("same-label");
        var differentLabel = this.Q<Label>("different-label");
        var differentButtons = this.Q<VisualElement>("different-buttons");

        if (dataSource.result)
        {
            sameLabel.style.display = DisplayStyle.Flex;
            differentLabel.style.display = DisplayStyle.None;
            differentButtons.style.display = DisplayStyle.None;

            return;
        }

        if (dataSource.leftHand.IsReadonly && dataSource.rightHand.IsReadonly)
        {
            sameLabel.style.display = DisplayStyle.None;
            differentLabel.style.display = DisplayStyle.Flex;
            differentButtons.style.display = DisplayStyle.None;

            return;
        }

        sameLabel.style.display = DisplayStyle.None;
        differentLabel.style.display = DisplayStyle.None;
        differentButtons.style.display = DisplayStyle.Flex;

        var copyLeftButton = this.Q<Button>("copy-left-button");
        if (dataSource.leftHand.IsReadonly)
        {
            copyLeftButton.style.display = DisplayStyle.None;
        }
        else
        {
            copyLeftButton.clicked -= OnCopyLeftClicked;
            copyLeftButton.clicked += OnCopyLeftClicked;
        }

        var copyRightButton = this.Q<Button>("copy-right-button");
        if (dataSource.rightHand.IsReadonly)
        {
            copyRightButton.style.display = DisplayStyle.None;
        }
        else
        {
            copyRightButton.clicked -= OnCopyRightClicked;
            copyRightButton.clicked += OnCopyRightClicked;
        }
    }

    private void OnCopyLeftClicked()
    {
        CompareOperand source = dataSource.rightHand;
        CompareOperand destination = dataSource.leftHand;
        destination.Value = source.Value;

        changed?.Invoke();
    }

    private void OnCopyRightClicked()
    {
        CompareOperand source = dataSource.leftHand;
        CompareOperand destination = dataSource.rightHand;
        destination.Value = source.Value;

        changed?.Invoke();
    }
    #endregion
}
