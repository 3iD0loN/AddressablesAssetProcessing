using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using USP.AddressablesAssetProcessing;

public class TreeViewElement
{
    public bool IsExpanded { get; set; }

    public TreeViewElement(bool isExpanded)
    {
        IsExpanded = isExpanded;
    }
}

public class TreeViewElement<T> : TreeViewElement
{
    public T Value { get; set; }

    public TreeViewElement(bool isExpanded, T value) :
        base(isExpanded)
    {
        Value = value;
    }
}

public static class TreeViewExtensions
{
    #region Static Methods
    public static void AddItem<T>(BaseTreeView treeView, int parentId, TreeViewItemData<T> item, int childIndex = -1, bool rebuildTree = true)
    {
        treeView.AddItem(item, parentId, childIndex, rebuildTree);

        AddItems(treeView, item.id, item.children, childIndex, rebuildTree);
    }

    public static void AddItems<T>(BaseTreeView treeView, int parentId, IEnumerable<TreeViewItemData<T>> items, int childIndex = -1, bool rebuildTree = true)
    {
        foreach (var item in items)
        {
            AddItem(treeView, parentId, item, childIndex, rebuildTree);
        }
    }

    public static void AddUniqueItem<T>(BaseTreeView treeView, int parentId, TreeViewItemData<T> item, int childIndex = -1, bool rebuildTree = true)
    {
        if (treeView.viewController.GetIndexForId(item.id) == -1)
        {
            treeView.AddItem(item, parentId, childIndex, rebuildTree);
        }
    }

    public static void AddUniqueItems<T>(BaseTreeView treeView, int parentId, IEnumerable<TreeViewItemData<T>> items, int childIndex = -1, bool rebuildTree = true)
    {
        var childItems = new List<TreeViewItemData<T>>(items);
        foreach (var item in childItems)
        {
            AddUniqueItem(treeView, parentId, item, childIndex, rebuildTree);
        }
    }

    public static int FindRootItemIdByIndex(BaseTreeView treeView, int index)
    {
        int id = treeView.viewController.GetIdForIndex(index);

        return FindRootItemIdById(treeView, id);
    }

    public static int FindRootItemIdById(BaseTreeView treeView, int id)
    {
        int nextId = treeView.viewController.GetParentId(id);

        if (nextId == -1)
        {
            return id;
        }

        return FindRootItemIdById(treeView, nextId);
    }

    public static int FindFirstRootItemIdByIndex<T>(BaseTreeView treeView, int index, Predicate<T> match)
    {
        int id = treeView.viewController.GetIdForIndex(index);

        return FindFirstRootItemIdById(treeView, id, match);
    }

    public static int FindFirstRootItemIdById<T>(BaseTreeView treeView, int id, Predicate<T> match)
    {
        if (id == -1 || match == null)
        {
            return -1;
        }

        T item = treeView.GetItemDataForId<T>(id);

        if (match(item))
        {
            return id;
        }

        int nextId = treeView.viewController.GetParentId(id);

        return FindFirstRootItemIdById(treeView, nextId, match);
    }

    public static void ReplaceItem<T>(BaseTreeView treeView, TreeViewItemData<T> item, int parentId = -1, int childIndex = -1, bool rebuildTree = true)
    {
        ReplaceItem(treeView, item.id, item, parentId, childIndex, rebuildTree);
    }

    public static void ReplaceItem<T>(BaseTreeView treeView, int oldId, TreeViewItemData<T> item, int parentId = -1, int childIndex = -1, bool rebuildTree = true)
    {
        if (childIndex == -1)
        {
            childIndex = treeView.viewController.GetIndexForId(oldId);
        }

        bool removed = treeView.TryRemoveItem(oldId, true);
        if (removed)
        {
            AddUniqueItem(treeView, parentId, item, childIndex - 1, rebuildTree);
        }
    }

    public static void ExpandItem<T>(BaseTreeView treeView, TreeViewItemData<T> item, bool shouldRefresh)
        where T : TreeViewElement
    {
        if (item.data.IsExpanded)
        {
            treeView.ExpandItem(item.id, false, shouldRefresh);
        }
        else
        {
            treeView.CollapseItem(item.id, false, shouldRefresh);
        }

        ExpandItems(treeView, item.children, shouldRefresh);
    }

    public static void ExpandItems<T>(BaseTreeView treeView, IEnumerable<TreeViewItemData<T>> items, bool shouldRefresh)
        where T : TreeViewElement
    {
        foreach (var item in items)
        {
            ExpandItem(treeView, item, shouldRefresh);
        }
    }
    #endregion
}
